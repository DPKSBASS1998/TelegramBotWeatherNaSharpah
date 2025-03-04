﻿using System.ComponentModel.DataAnnotations.Schema;

namespace TelegramBotWeatherNaSharpah.Models
{
    public class WeatherRequestToDB // Клас для зберігання запитів погоди
    {
        public int Id { get; set; }  // Ідентифікатор запиту
        [ForeignKey("ChatId")]
        public long ChatId { get; set; }  // Ідентифікатор чату користувача
        public DateTime RequestDate { get; set; }  // Дата запиту
        public string? CityName { get; set; }  // Назва міста
        public string? WeatherCondition { get; set; }  // Умови погоди
        public double? Temperature { get; set; }  // Температура
        public double? FeelsLike { get; set; }  // Температура, що відчувається
        public int? Humidity { get; set; }  // Вологість
        public double? WindSpeed { get; set; }  // Швидкість вітру
        public string? ErrorMessage { get; set; }  // Повідомлення про помилку
    }
}
