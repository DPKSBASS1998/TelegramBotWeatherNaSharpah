using TelegramBotWeatherNaSharpah.Services;
using TelegramBotWeatherNaSharpah.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Configuration;

var builder = WebApplication.CreateBuilder(args);

// �������� ����� � ����� �����
var botToken = builder.Configuration["TelegramSettings:BotToken"];
// ��������� ������ ��� TelegramBotService � WeatherService � DI ���������
builder.Services.AddSingleton<TelegramBotService>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>(); // �������� IConfiguration � DI ����������
    var weatherService = sp.GetRequiredService<WeatherService>(); // �������� WeatherService � DI ����������
    var userService = sp.GetRequiredService<UserService>(); // �������� UserService � DI ����������
    return new TelegramBotService(botToken, configuration, weatherService, userService); // �������� ��������� � TelegramBotService
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
    Console.WriteLine($"������� ���������� �� Telegram: {ex.Message}");
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
