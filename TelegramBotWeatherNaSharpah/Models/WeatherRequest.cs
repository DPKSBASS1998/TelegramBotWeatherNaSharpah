using System.ComponentModel.DataAnnotations.Schema;

namespace TelegramBotWeatherNaSharpah.Models
{
    public class WeatherRequest
    {
        public int Id { get; set; }  // Ідентифікатор запиту
        [ForeignKey("ChatId")]
        public long ChatId { get; set; }  // Ідентифікатор чату користувача
        public string City { get; set; }  // Назва міста
        public string WeatherResponse { get; set; }  // Відповідь на запит погоди
        public DateTime RequestDate { get; set; }  // Дата запиту
    }
}
