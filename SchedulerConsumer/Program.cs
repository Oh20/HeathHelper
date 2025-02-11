using Microsoft.EntityFrameworkCore;
using static AgendaConsumer;

var builder = WebApplication.CreateBuilder(args);

// Configuração do RabbitMQ
var rabbitConfig = new RabbitMQConfig
{
    HostName = Environment.GetEnvironmentVariable("RabbitMQ__HostName") ?? "localhost",
    Port = int.Parse(Environment.GetEnvironmentVariable("RabbitMQ__Port") ?? "5672"),
    UserName = Environment.GetEnvironmentVariable("RabbitMQ__UserName") ?? "guest",
    Password = Environment.GetEnvironmentVariable("RabbitMQ__Password") ?? "guest"
};

// Configuração de serviços externos
var userServiceUrl = Environment.GetEnvironmentVariable("UserServiceUrl")
    ?? builder.Configuration["UserServiceUrl"]
    ?? "http://localhost:5001";  // Porta do UserRegisterConsumer

// Configuração do HttpClient
builder.Services.AddHttpClient("UserService", client =>
{
    client.BaseAddress = new Uri(userServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});


// Configuração do banco de dados
var dbConnection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddSingleton(rabbitConfig);
builder.Services.AddSingleton<AgendaConsumer>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<AgendaConsumer>>();
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var serviceProvider = sp;

    return new AgendaConsumer(
        rabbitConfig,
        serviceProvider,
        logger,
        httpClientFactory,
        userServiceUrl
    );
});

// Configuração do DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(dbConnection));

// Configuração de serviços
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<AgendaConsumerService>();

// Configuração do HttpClient
builder.Services.AddHttpClient("UserService", client =>
{
    client.BaseAddress = new Uri(userServiceUrl);
});

// Configuração de Logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Information);
});

var app = builder.Build();

// Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Health Check endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy" }));

try
{
    app.Logger.LogInformation("Iniciando aplicação Consumer");
    app.Run();
}
catch (Exception ex)
{
    app.Logger.LogCritical(ex, "Aplicação Consumer falhou ao iniciar");
    throw;
}
