using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBotWeatherNaSharpah.Models;
using TelegramBotWeatherNaSharpah.Services;
using System.Threading.Tasks;
using System.Linq;
using Telegram.Bot;
using System.Data;

namespace TelegramBotWeatherNaSharpah
{
    [ApiController]
    [Route("api")]
    public class UserController : ControllerBase
    {
        private readonly TelegramBotService _telegramBotService;

        public UserController(TelegramBotService telegramBotService)
        {
            _telegramBotService = telegramBotService;
        }

        [HttpGet("users")]// Endpoint для отримання всіх користувачів
        public async Task<IActionResult> GetAllUsers()
        {
            List<UserModel> users = await _telegramBotService.GetUsersFromDb();// отримання всіх користувачів
            return Ok(users);// виведення всіх користувачів
        }

        // Endpoint для отримання всіх запитів користувача за ID
        [HttpGet("users/{userId}/weatherrequests")]
        public async Task<IActionResult> GetWeatherRequestsByUserId(long userId)
        {
            var weatherRequests = await _telegramBotService.GetWeatherRequestsByUserId(userId); // отримання всіх запитів погоди користувача за ID

            if (weatherRequests == null || !weatherRequests.Any())
            {
                return NotFound(new { message = "Немає запитів погоди для цього користувача." });
            }

            return Ok(weatherRequests);// виведення запитів погоди користувача
        }

        // Endpoint для надсилання погоди всім користувачам
        [HttpPost("sendWeatherToAll")]
        public async Task<IActionResult> SendWeatherToAll([FromBody] string city)
        {
            List<UserModel> users = await _telegramBotService.GetUsersFromDb();//отримання всіх користувачів
            string weatherInfo = await _telegramBotService.GetWeatherAsync(city);//отримання погоди
            foreach (var user in users)
            {
                await _telegramBotService.SendTextMessageAsync(user.ChatId, weatherInfo);// надсилання погоди кожному користувачу
            }
            return Ok();
        }
    }
}
