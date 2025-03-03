namespace TelegramBotWeatherNaSharpah.Models
{
    public class WeatherResponse // Клас для зберігання відповіді про погоду
    {
        public string? CityName { get; set; }  // Назва міста
        public string? WeatherCondition { get; set; }  // Умови погоди
        public double? Temperature { get; set; }  // Температура
        public double? FeelsLike { get; set; }  // Температура, що відчувається
        public int? Humidity { get; set; }  // Вологість
        public double? WindSpeed { get; set; }  // Швидкість вітру
        public string? ErrorMessage { get; set; }  // Повідомлення про помилку

    }

}
