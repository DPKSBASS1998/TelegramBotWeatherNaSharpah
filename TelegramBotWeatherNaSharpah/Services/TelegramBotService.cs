using System.Data;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotWeatherNaSharpah.Models;


namespace TelegramBotWeatherNaSharpah.Services
{
    public class TelegramBotService
    {
        private readonly TelegramBotClient _botClient; // Об'єкт  Telegram API
        private readonly WeatherService _weatherService;
        private readonly UserService _userService;

        // 
        public TelegramBotService(string token, IConfiguration configuration, WeatherService weatherService, UserService userService)
        {
            _botClient = new TelegramBotClient(token); // Створення нового екземпляра TelegramBotClient з токеном
            _weatherService = weatherService; // Інжектуємо WeatherService
            _userService = userService;
        }

        // Додатковий метод який можна використовувати в інших класах не інжеціюючи TelegramBotClient
        public async Task SendMessage(long userId, string message)
        {
            await _botClient.SendMessage(userId, message);
        }

        // Метод для обробки оновлень (повідомлень та або кнопок) від користувача
        public async Task HandleUpdateAsync(Update update)
        {
            if (update.Type == UpdateType.Message && update.Message!.Text != null) // перевірямо чи повідомлення є текстовим а не файл і тд
            {

                var message = update.Message.Text;
                var chatId = update.Message.Chat.Id;
                var username = update.Message.Chat.Username ?? "Без імені"; // Якщо немає username, ставимо заглушку

                if (message == "/start") // Обробка кнопки "Старт"
                {
                    string msg = "Привіт! Я бот, який може показати тобі погоду в різних містах світу. \n" +
                                 "Щоб отримати прогноз погоди, напишіть /weather 'Назва_міста";
                    await _botClient.SendMessage(chatId, msg, replyMarkup: GetWeatherButtons());
                }
                else
                {
                    if (!message.StartsWith("/weather"))
                    {
                        string msg = "Запит повинен починатись з /weather. \n" +
                            "Щоб отримати повну інструкцію, напиши /start";
                        await _botClient.SendMessage(chatId, msg);
                        return;
                    }

                    string city = message.Replace("/weather", "").Trim();

                    if (string.IsNullOrWhiteSpace(city))
                    {
                        string msg = "Після /weather необхідно вказати назву міста англійською, наприклад: \n/weather Dnipro. \n" +
                            "Щоб отримати повну інструкцію, напиши /start";
                        await _botClient.SendMessage(chatId, msg);
                        return;
                    }

                    await ProcessWeatherRequestAsync(chatId, username, city);

                }
            }

            if (update.Type == UpdateType.CallbackQuery) // Обробляємо тільки кнопки
            {
                var callbackQuery = update.CallbackQuery; // Отримуємо дані про натискання кнопки
                var chatId = callbackQuery.Message.Chat.Id; // Отримуємо ідентифікатор чату
                var username = callbackQuery.Message.Chat.Username ?? "Без імені"; // Отримуємо юзернейм, якщо немає то ставимо без імені
                string city = callbackQuery.Data; // Отримуємо дані, які були передані при натисканні кнопки

                await ProcessWeatherRequestAsync(chatId, username, city);
            }
        }

        private async Task ProcessWeatherRequestAsync(long chatId, string username, string city)
        {
            // Якщо всі перевірки пройдено, отримуємо погоду
            WeatherResponse weatherInfo = await _weatherService.GetWeatherAsync(city); //отримання погоди

            if (weatherInfo.ErrorMessage != null)
            {
                string msg = $" Помилка при отриманні погоди для міста {city}. {weatherInfo.ErrorMessage}";
                await _botClient.SendMessage(chatId, msg);
            }
            else
            {
                string formatedResponce = _weatherService.FormatWeatherInfo(weatherInfo); //форматування погоди
                await _botClient.SendMessage(chatId, formatedResponce); //надсилання погоди
            }

            // Збір інформації для бази даних
            var weatherinfoDB = _weatherService.MapToWeatherResponse(weatherInfo, chatId);

            try
            {
                // Додавання інформації в базу даних
                await _userService.AddOrUpdateUser(weatherinfoDB, username);
            }
            catch (Exception ex)
            {
                // Логування помилки в разі неуспішного запису в базу даних
                // Тут можна логувати помилку або надіслати повідомлення адміністратору, якщо потрібно
                Console.WriteLine($"Помилка при запису в базу даних: {ex.Message}");
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
            var me = await _botClient.GetMe(); // Отримуємо інформацію про бота
            Console.WriteLine($"Бот {me.Username} запущено!"); // Виводимо повідомлення про запуск бота в консолі

            _botClient.StartReceiving(
                async (client, update, token) => await HandleUpdateAsync(update),
                (client, exception, token) => Console.WriteLine($"Помилка: {exception.Message}")
            );

        }

        // Метод для відправки повідомлення користувачу
        public async Task SendMessageToUser(long chatId, string messageText = null, string imageUrl = null, string stickerFileId = null, string audioUrl = null)
        {
            if (messageText != null)
            {
                await _botClient.SendMessage(chatId, messageText);
            }
            else if (imageUrl != null)
            {
                await _botClient.SendPhoto(chatId, imageUrl);
            }
            else if (stickerFileId != null)
            {
                await _botClient.SendSticker(chatId, stickerFileId);
            }
            else if (audioUrl != null)
            {
                await _botClient.SendVoice(chatId, audioUrl);
            }
            else
            {
                // Якщо жоден з параметрів не заповнений
                await _botClient.SendMessage(chatId, "Невідомий тип повідомлення");
            }
        }

    }
}
