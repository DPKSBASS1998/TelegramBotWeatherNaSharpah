using System.Data;
using TelegramBotWeatherNaSharpah.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;

namespace TelegramBotWeatherNaSharpah.Services
{
    public class UserService
    {
        private readonly string _connectionString;
        public UserService(IConfiguration configuration) {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        public async Task AddOrUpdateUser(WeatherRequestToDB weatherInfo, string username)
        {
            using (var dbConnection = new SqlConnection(_connectionString))
            {
                string insertQueryforWeather = @"
                INSERT INTO WeatherRequests (ChatId, RequestDate, CityName, WeatherCondition, Temperature, FeelsLike, Humidity, WindSpeed, ErrorMessage)
                VALUES (@ChatId, @RequestDate, @CityName, @WeatherCondition, @Temperature, @FeelsLike, @Humidity, @WindSpeed, @ErrorMessage)"
            ;
                await dbConnection.OpenAsync();

                var user = await dbConnection.QueryFirstOrDefaultAsync<UserModel>(
                    "SELECT * FROM Users WHERE ChatId = @ChatId", new { ChatId = weatherInfo.ChatId });
                long ChatId = weatherInfo.ChatId;

                if (user == null)
                {
                    var insertUserQuery = "INSERT INTO Users (ChatId, Username) VALUES (@ChatId, @Username)";
                    await dbConnection.ExecuteAsync(insertUserQuery, new { ChatId = ChatId, Username = username });

                    await dbConnection.ExecuteAsync(insertQueryforWeather, weatherInfo);

                }
                else
                {
                    await dbConnection.ExecuteAsync(insertQueryforWeather, weatherInfo);
                }
            }
        }
        public async Task<List<UserModel>> GetUsersFromDb()
        {
            using (var dbConnection = new SqlConnection(_connectionString))
            {
                await dbConnection.OpenAsync();
                return (await dbConnection.QueryAsync<UserModel>("SELECT * FROM Users")).ToList();
            }
        }
      

        // Отримання запитів погоди за ID користувача
        public async Task<List<WeatherRequestToDB>> GetWeatherRequestsByUserId(long userId)
        {
            using (var dbConnection = new SqlConnection(_connectionString))
            {
                await dbConnection.OpenAsync();
                return (await dbConnection.QueryAsync<WeatherRequestToDB>(
                    "SELECT * FROM WeatherRequests WHERE ChatId = @ChatId", new { ChatId = userId }
                )).ToList();
            }
        }

    }
}
