using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TelegramBotWeatherNaSharpah.Models
{
    public class UserModel
    {
        [Key]
        public long ChatId { get; set; }  // Ідентифікатор чату користувача в Telegram
        public string Username { get; set; }  // Ім'я користувача
    }

}
