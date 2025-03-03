using System.Net.Http;
using System.Text.Json;
using Azure;
using Microsoft.Extensions.Options;
using TelegramBotWeatherNaSharpah.Models;

namespace TelegramBotWeatherNaSharpah.Services
{
    public class WeatherService
    {
        private readonly WeatherSettings _weatherSettings;
        private readonly HttpClient _httpClient;

        public WeatherService(IOptions<WeatherSettings> weatherSettings, HttpClient httpClient)
        {
            _weatherSettings = weatherSettings.Value;
            _httpClient = httpClient;
        }

        // Загальний метод, що поєднує всі кроки
        public async Task<WeatherResponse?> GetWeatherAsync(string city)
        {
            try
            {
                // 1. Отримуємо сиру відповідь
                var responseBody = await GetWeatherRawAsync(city);

                // 2. Десеріалізуємо відповідь в об'єкт
                return await DeserializeWeatherResponse(responseBody);
            }
            catch (Exception ex)
            {
                // Логування помилки
                Console.WriteLine($"Помилка при запиті до API: {ex.Message}");
                return null; // Повертаємо null у разі помилки
            }
        }


        // 1. Метод для отримання сирої відповіді
        public async Task<HttpResponseMessage> GetWeatherRawAsync(string city)
        {
            var url = $"{_weatherSettings.BaseUrl}?q={city}&appid={_weatherSettings.ApiKey}&units=metric&lang=ua";
            var response = await _httpClient.GetAsync(url); // Отримаємо всю відповідь (включаючи статус-код)
            return response;
        }


        // 2. Метод для десеріалізації JSON в об'єкт
        public async Task<WeatherResponse> DeserializeWeatherResponse(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode) // Перевірка на успішний статус-код (200-299)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var weatherData = JsonSerializer.Deserialize<JsonElement>(responseBody);

                var weatherCondition = weatherData.GetProperty("weather")[0].GetProperty("description").GetString();
                var temperature = weatherData.GetProperty("main").GetProperty("temp").GetDouble();
                var feelsLike = weatherData.GetProperty("main").GetProperty("feels_like").GetDouble();
                var humidity = weatherData.GetProperty("main").GetProperty("humidity").GetInt32();
                var windSpeed = weatherData.GetProperty("wind").GetProperty("speed").GetDouble();
                var cityName = weatherData.GetProperty("name").GetString();

                return new WeatherResponse
                {
                    CityName = cityName,
                    WeatherCondition = weatherCondition,
                    Temperature = temperature,
                    FeelsLike = feelsLike,
                    Humidity = humidity,
                    WindSpeed = windSpeed,
                    ErrorMessage = null
                };
            }
            else
            {
                // Отримуємо повідомлення про помилку, якщо статус-код не успішний
                var responseBody = await response.Content.ReadAsStringAsync();
                var weatherData = JsonSerializer.Deserialize<JsonElement>(responseBody);
                var errorMessage = weatherData.TryGetProperty("message", out var message) ? message.GetString() : "Невідома помилка";

                return new WeatherResponse
                {
                    CityName = null,
                    WeatherCondition = null,
                    Temperature = null,
                    FeelsLike = null,
                    Humidity = null,
                    WindSpeed = null,
                    ErrorMessage = $"Помилка: {errorMessage}, Статус код: {response.StatusCode}"
                };
            }
        }


        // 3. Метод для форматування відповіді
        public string FormatWeatherInfo(WeatherResponse weather)
        {
            return $@"
            Погода у {weather.CityName}
            Умови: {weather.WeatherCondition}
            Температура: {weather.Temperature}°C
            Відчувається як: {weather.FeelsLike}°C
            Вологість: {weather.Humidity}%
            Швидкість вітру: {weather.WindSpeed} м/с
        ";
        }
        public WeatherRequestToDB MapToWeatherResponse(WeatherResponse weatherResponce, long userId) // Метод для маппінгу об'єкта WeatherResponse в WeatherRequestToDB
        {
            return new WeatherRequestToDB
            {
                ChatId = userId,
                RequestDate = DateTime.Now,

                CityName = weatherResponce.CityName,
                WeatherCondition = weatherResponce.WeatherCondition,
                Temperature = weatherResponce.Temperature,
                FeelsLike = weatherResponce.FeelsLike,
                Humidity = weatherResponce.Humidity,
                WindSpeed = weatherResponce.WindSpeed,
                ErrorMessage = weatherResponce.ErrorMessage
            };
        }


    }

}
