using TelegramBotWeatherNaSharpah.Services;
using TelegramBotWeatherNaSharpah.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Отримуємо токен з джсон файлу
var botToken = builder.Configuration["TelegramSettings:BotToken"];
// Регіструємо сервіси для TelegramBotService і WeatherService в DI контейнері
builder.Services.AddSingleton<TelegramBotService>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>(); // Отримуємо IConfiguration з DI контейнера
    var weatherService = sp.GetRequiredService<WeatherService>(); // Отримуємо WeatherService з DI контейнера
    var userService = sp.GetRequiredService<UserService>(); // Отримуємо UserService з DI контейнера
    return new TelegramBotService(botToken, configuration, weatherService, userService); // Передаємо залежності в TelegramBotService
});

builder.Services.Configure<WeatherSettings>(builder.Configuration.GetSection("WeatherSettings"));
builder.Services.AddHttpClient<WeatherService>();
builder.Services.AddSingleton<UserService>(sp => {
    var configuration = sp.GetRequiredService<IConfiguration>();
    return new UserService(configuration);
});


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();


var botService = app.Services.GetRequiredService<TelegramBotService>();

try
{
    await botService.StartAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"Помилка підключення до Telegram: {ex.Message}");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
