using System.Data;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotWeatherNaSharpah.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data.Common;

namespace TelegramBotWeatherNaSharpah.Services
{
    public class TelegramBotService
    {
        private readonly TelegramBotClient _botClient; // Об'єкт  Telegram API
        private readonly string _weatherApiKey;// ApiKey до OpenWeather
        private readonly IDbConnection _dbConnection; // Підключення до бази даних
        private readonly string _connectionString; // Рядок підключення до БД

        // Конструктор
        public TelegramBotService(string token, IConfiguration configuration)
        {
            _botClient = new TelegramBotClient(token); // Створення нового екземпляра TelegramBotClient з токеном
            _connectionString = configuration.GetConnectionString("DefaultConnection"); // Отримуємо рядок підключення
            _dbConnection = new SqlConnection(_connectionString); // Створюємо з'єднання з БД
            _weatherApiKey = configuration["WeatherSettings:ApiKey"]; //апіключ
        }

        // Додатковий метод для спрощення
        public async Task SendTextMessageAsync(long userId, string message)
        {
            await _botClient.SendTextMessageAsync(userId, message, parseMode: ParseMode.Html);
        }

        public async Task AddOrUpdateUser(long userId, string username, string weatherInfo, string city)
        {
            using (IDbConnection dbConnection = new SqlConnection(_connectionString))
            {
                dbConnection.Open();

                // Перевіряємо, чи існує користувач у базі даних
                var user = await dbConnection.QueryFirstOrDefaultAsync<UserModel>(
                    "SELECT * FROM Users WHERE ChatId = @ChatId", new { ChatId = userId });

                if (user == null)
                {
                    // Якщо користувач відсутній, додаємо нового користувача
                    var insertUserQuery = "INSERT INTO Users (ChatId, Username) VALUES (@ChatId, @Username)";
                    await dbConnection.ExecuteAsync(insertUserQuery, new { ChatId = userId, Username = username });

                    // Створюємо новий запит погоди для користувача
                    var insertWeatherRequestQuery = @"
                    INSERT INTO WeatherRequests (ChatId, City, WeatherResponse, RequestDate)
                    VALUES (@ChatId, @City, @WeatherResponse, @RequestDate)";
                    await dbConnection.ExecuteAsync(insertWeatherRequestQuery,
                        new { ChatId = userId, City = city, WeatherResponse = weatherInfo, RequestDate = DateTime.Now });
                }
                else
                {
                    // Якщо користувач існує, просто додаємо новий запит погоди
                    var insertWeatherRequestQuery = @"
                    INSERT INTO WeatherRequests (ChatId, City, WeatherResponse, RequestDate)
                    VALUES (@ChatId, @City, @WeatherResponse, @RequestDate)";
                    await dbConnection.ExecuteAsync(insertWeatherRequestQuery,
                        new { ChatId = userId, City = city, WeatherResponse = weatherInfo, RequestDate = DateTime.Now });
                }
            }
        }

        // Метод для обробки оновлень (повідомлень та або кнопок) від користувача
        public async Task HandleUpdateAsync(Update update)
        {
            if (update.Type == UpdateType.Message && update.Message!.Text != null) // перевірямо чи повідомлення є текстовим а не файл і тд
            {
                var message = update.Message.Text;
                var chatId = update.Message.Chat.Id;
                var username = update.Message.Chat.Username ?? "Без імені"; // Якщо немає username, ставимо заглушку

                if (message == "/start" || message == "Старт") // Обробка кнопки "Старт"
                {
                    await _botClient.SendTextMessageAsync(
                        chatId,
                        "Щоб отримати прогноз погоди, напишіть /weather 'Назва_міста_англійською' або скористайтесь готовими варіантами.",
                        replyMarkup: GetWeatherButtons() // Відкриваємо меню з містами
                    );
                }
                else if (message.StartsWith("/weather")) // Перевіряємо, чи починається рядок з "/weather"
                {
                    string city = message.Replace("/weather", "").Trim(); // Видаляємо "/weather" і обрізаємо всі пробіли

                    if (!string.IsNullOrWhiteSpace(city)) // Перевіряємо, чи залишився якийсь текст
                    {
                        try
                        {
                            // Отримуємо дані про погоду
                            string weatherInfo = await GetWeatherAsync(city);

                            // Виводимо відповідь
                            await SendTextMessageAsync(chatId, weatherInfo);

                            // Записуємо запит в БД
                            await AddOrUpdateUser(chatId, username, weatherInfo, city);

                            Console.WriteLine($"Успішний запит: {city}");
                        }
                        catch (Exception ex)
                        {
                            // Якщо виникла помилка, виводимо повідомлення в консоль
                            Console.WriteLine($"Помилка при запиті погоди для міста {city}: {ex.Message}");

                            // Записуємо помилку в базу даних
                            string errorMessage = $"Помилка при запиті для міста {city}: {ex.Message}";
                            await AddOrUpdateUser(chatId, username, errorMessage, city); // Зберігаємо помилку замість погоди

                            // Виводимо повідомлення користувачу
                            await SendTextMessageAsync(chatId, $"Не вдалося отримати погоду для {city}. Помилка: {ex.Message}");

                            // Логування помилки в консолі
                            Console.WriteLine($"Запит для міста {city} не вдався: {ex.Message}");
                        }
                    }

                    else
                    {
                        await _botClient.SendTextMessageAsync(chatId, "Вкажіть назву міста після команди /weather.");
                    }
                }
                else
                {
                    await _botClient.SendTextMessageAsync(chatId, "Для отримання інструкції натисніть кнопку /start");
                }
            }

            if (update.Type == UpdateType.CallbackQuery) // Обробляємо тільки кнопки
            {
                var callbackQuery = update.CallbackQuery; // Отримуємо дані про натискання кнопки
                var chatId = callbackQuery.Message.Chat.Id; // Отримуємо ідентифікатор чату
                var username = callbackQuery.Message.Chat.Username ?? "Без імені"; // Отримуємо юзернейм, якщо немає то ставимо без імені

                string city = callbackQuery.Data; // Отримуємо дані, які були передані при натисканні кнопки

                string weatherInfo = await GetWeatherAsync(city); // Записуємо результати надісланого запиту

                await AddOrUpdateUser(chatId, username, weatherInfo, city); // додаємо запит в історію запитів юзера 
                Console.WriteLine(city);
                await SendTextMessageAsync(chatId, weatherInfo); // виводимо відповідь з форматуванням в хтмл
            }
        }

        // Метод для отримання погоди через везер апі
        public async Task<string> GetWeatherAsync(string city)
        {
            using var httpClient = new HttpClient(); // Створюємо екземпляр HttpClient для виконання HTTP запитів
            string url = $"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={_weatherApiKey}&units=metric&lang=ua"; // Формуємо URL запиту до API

            try
            {
                var response = await httpClient.GetAsync(url); // Виконуємо запит

                if (response.IsSuccessStatusCode) // Перевіряємо, чи успішний статус-код (200-299)
                {
                    var responseBody = await response.Content.ReadAsStringAsync(); // Отримуємо відповідь від API

                    // Десеріалізуємо JSON файл
                    var weatherData = JsonSerializer.Deserialize<JsonElement>(responseBody);

                    // Отримуємо основні дані з JSON відповіді
                    var weatherCondition = weatherData.GetProperty("weather")[0].GetProperty("description").GetString(); // Опис погодних умов
                    var temperature = weatherData.GetProperty("main").GetProperty("temp").GetDouble(); // Температура в градусах Цельсія
                    var feelsLike = weatherData.GetProperty("main").GetProperty("feels_like").GetDouble(); // Відчувається як 
                    var humidity = weatherData.GetProperty("main").GetProperty("humidity").GetInt32(); // Вологість
                    var windSpeed = weatherData.GetProperty("wind").GetProperty("speed").GetDouble(); // Швидкість вітру
                    var cityName = weatherData.GetProperty("name").GetString(); // Назва міста

                    // Формуємо повідомлення у форматі HTML
                    string weatherInfo = $@"
                        <b>Погода у {cityName}</b>
                        <i>Умови:</i> {weatherCondition}
                        <i>Температура:</i> {temperature}°C
                        <i>Відчувається як:</i> {feelsLike}°C
                        <i>Вологість:</i> {humidity}%
                        <i>Швидкість вітру:</i> {windSpeed} м/с
                        ";

                    return weatherInfo; // Повертаємо повідомлення
                }
                else
                {
                    //записуємо відповідь сервера
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    string errorMessage = $"Помилка при запиті для міста {city}: {response.StatusCode} - {errorResponse}";

                    return errorMessage; // Повертаємо помилку
                }
            }
            catch (Exception ex)
            {
                // Інші проблеми, мережа і тд
                Console.WriteLine($"Помилка при запиті до API: {ex.Message}");
                return $"Не вдалося отримати погоду для міста {city}. Помилка: {ex.Message}";
            }
        }

        // Створення кнопок для вибору міста
        private InlineKeyboardMarkup GetWeatherButtons()
        {
            return new InlineKeyboardMarkup( // Клавіатура
                new[]
                {
                    new InlineKeyboardButton // Непосредственно кнопка
                    {
                        Text = "Київ", // Текст на кнопці
                        CallbackData = "Kyiv" // Дані, які будуть передані при натисканні кнопки
                    },
                    new InlineKeyboardButton
                    {
                        Text = "Берлін",
                        CallbackData = "Berlin"
                    },
                    new InlineKeyboardButton
                    {
                        Text = "Нью-Йорк",
                        CallbackData = "New York"
                    },
                    new InlineKeyboardButton
                    {
                        Text = "Токіо",
                        CallbackData = "Tokyo"
                    }
                }
            );
        }

        // Метод для запуску бота
        public async Task StartAsync()
        {
            var me = await _botClient.GetMeAsync(); // Отримуємо інформацію про бота
            Console.WriteLine($"Бот {me.Username} запущено!"); // Виводимо повідомлення про запуск бота в консолі

            _botClient.StartReceiving( // Починаємо отримувати оновлення від користувачів
                async (client, update, token) => await HandleUpdateAsync(update), // Обробляємо оновлення
                async (client, exception, token) => Console.WriteLine($"Помилка: {exception.Message}") // Логування помилок
            );
        }

        // Отримання всіх користувачів з БД
        public async Task<List<UserModel>> GetUsersFromDb()
        {
            using (IDbConnection dbConnection = new SqlConnection(_connectionString))
            {
                dbConnection.Open();
                return (await dbConnection.QueryAsync<UserModel>("SELECT * FROM Users")).ToList();
            }
        }

        // Отримання запитів погоди за ID користувача
        public async Task<List<WeatherRequest>> GetWeatherRequestsByUserId(long userId)
        {
            using (IDbConnection dbConnection = new SqlConnection(_connectionString))
            {
                dbConnection.Open();
                return (await dbConnection.QueryAsync<WeatherRequest>(
                    "SELECT * FROM WeatherRequests WHERE ChatId = @ChatId", new { ChatId = userId }
                )).ToList();
            }
        }
    }
}
