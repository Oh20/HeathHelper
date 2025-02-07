var builder = WebApplication.CreateBuilder(args);

// Configurações do RabbitMQ para conexão
var rabbitMQHostName = Environment.GetEnvironmentVariable("RabbitMQ__HostName") ?? "localhost";
var rabbitMQPort = Environment.GetEnvironmentVariable("RabbitMQ__Port") ?? "5672";
var rabbitMQUser = Environment.GetEnvironmentVariable("RabbitMQ__UserName") ?? "guest";
var rabbitMQPassword = Environment.GetEnvironmentVariable("RabbitMQ__Password") ?? "guest";

var rabbitMQConnectionString = $"amqp://{rabbitMQUser}:{rabbitMQPassword}@{rabbitMQHostName}:{rabbitMQPort}";

// Configuração do Consumer Service
var consumerServiceUrl = Environment.GetEnvironmentVariable("ConsumerServiceUrl")
   ?? builder.Configuration["ConsumerServiceUrl"];

// Registro de Serviços
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configuração do AgendaProducer
builder.Services.AddSingleton(sp =>
{
    var logger = sp.GetRequiredService<ILogger<AgendaProducer>>();
    return new AgendaProducer(rabbitMQConnectionString, logger);
});

// Configuração do HttpClient
builder.Services.AddHttpClient("ConsumerService", client =>
{
    client.BaseAddress = new Uri(consumerServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
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

// Health Check endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy" }));

// Mapear controllers
app.MapControllers();

try
{
    app.Logger.LogInformation("Iniciando aplicação Producer");
    app.Run();
}
catch (Exception ex)
{
    app.Logger.LogCritical(ex, "Aplicação Producer falhou ao iniciar");
    throw;
}