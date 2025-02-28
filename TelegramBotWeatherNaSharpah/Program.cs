using TelegramBotWeatherNaSharpah.Services;

var builder = WebApplication.CreateBuilder(args);

// �������� ����� � ����� �����
var botToken = builder.Configuration["TelegramSettings:BotToken"];
builder.Services.AddSingleton<TelegramBotService>(sp =>
    new TelegramBotService(botToken, sp.GetRequiredService<IConfiguration>()));


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
