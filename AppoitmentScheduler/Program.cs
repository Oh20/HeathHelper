using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Configuração de Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Configuração de URLs e RabbitMQ
builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(5065));
var rabbitConfig = builder.Configuration.GetSection("RabbitMQ").Get<RabbitMQConfig>() ?? new RabbitMQConfig();
var consumerServiceUrl = builder.Configuration["ConsumerServiceUrl"] ?? "http://localhost:5065";

// Configuração de HttpClient
builder.Services.AddHttpClient("ConsumerService", client => client.BaseAddress = new Uri(consumerServiceUrl));

// Registro de Serviços
builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.WriteIndented = true);
builder.Services.AddSingleton(rabbitConfig);
builder.Services.AddSingleton<AgendaProducer>();

var app = builder.Build();

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.Run();