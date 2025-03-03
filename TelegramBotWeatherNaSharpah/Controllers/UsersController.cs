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
        private readonly WeatherService _weatherService;
        private readonly UserService _userService;

        public UserController(TelegramBotService telegramBotService, UserService userService)
        {
            _telegramBotService = telegramBotService;
            _userService = userService;
        }

        [HttpGet("users")]// Endpoint для отримання всіх користувачів
        public async Task<IActionResult> GetAllUsers()
        {
            List<UserModel> users = await _userService.GetUsersFromDb();// отримання всіх користувачів
            return Ok(users);// виведення всіх користувачів
        }

        // Endpoint для отримання всіх запитів користувача за ID
        [HttpGet("users/{userId}/weatherrequests")]
        public async Task<IActionResult> GetWeatherRequestsByUserId(long userId)
        {
            var weatherRequests = await _userService.GetWeatherRequestsByUserId(userId); // отримання всіх запитів погоди користувача за ID

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
            List<UserModel> users = await _userService.GetUsersFromDb();//отримання всіх користувачів
            WeatherResponse weatherInfo = await _weatherService.GetWeatherAsync(city);//отримання погоди
            string formatedResponce = _weatherService.FormatWeatherInfo(weatherInfo);
            foreach (var user in users)
            {
                await _telegramBotService.SendMessage(user.ChatId, formatedResponce);// надсилання погоди кожному користувачу

            }
            return Ok();
        }

        [HttpPost("sendFunById")]
        public async Task<IActionResult> SendFunById(long chatId, string messageText = null, string imageUrl = null, string stickerFileId = null, string audioUrl = null)
        {
            await _telegramBotService.SendMessageToUser(chatId,messageText,imageUrl,stickerFileId,audioUrl);
            return Ok();
        }

    }
}
