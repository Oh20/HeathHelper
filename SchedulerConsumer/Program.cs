using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configura��es do RabbitMQ
var rabbitMQHostName = Environment.GetEnvironmentVariable("RabbitMQ__HostName") ?? "localhost";
var rabbitMQPort = Environment.GetEnvironmentVariable("RabbitMQ__Port") ?? "5672";
var rabbitMQUser = Environment.GetEnvironmentVariable("RabbitMQ__UserName") ?? "guest";
var rabbitMQPassword = Environment.GetEnvironmentVariable("RabbitMQ__Password") ?? "guest";

var rabbitMQConnectionString = $"amqp://{rabbitMQUser}:{rabbitMQPassword}@{rabbitMQHostName}:{rabbitMQPort}";

// Configura��o do SQL Server
var dbConnection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

// Configura��o do User Service
var userServiceUrl = Environment.GetEnvironmentVariable("UserServiceUrl")
    ?? builder.Configuration["UserServiceUrl"];

// Registro de Servi�os
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configura��o do HttpClient
builder.Services.AddHttpClient("UserService", client =>
{
    client.BaseAddress = new Uri(userServiceUrl);
});

// Configura��o do DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        dbConnection,
        sqlServerOptionsAction: sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        }
    ),
    ServiceLifetime.Transient
);

// Configura��o do Consumer Service
builder.Services.AddSingleton(sp => new Dictionary<string, string>
{
    { "RabbitMQ:ConnectionString", rabbitMQConnectionString },
    { "UserServiceUrl", userServiceUrl }
});

builder.Services.AddHostedService<AgendaConsumerService>();

// Configura��o de Logging
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
    app.Logger.LogInformation("Iniciando aplica��o Consumer");
    app.Run();
}
catch (Exception ex)
{
    app.Logger.LogCritical(ex, "Aplica��o Consumer falhou ao iniciar");
    throw;
}