var builder = WebApplication.CreateBuilder(args);

// Adicionar serviços necessários
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Registrar o AgendaProducer
builder.Services.AddSingleton(sp =>
{
    var logger = sp.GetRequiredService<ILogger<AgendaProducer>>();
    var connectionString = builder.Configuration.GetConnectionString("RabbitMQ");
    return new AgendaProducer(connectionString, logger);
});

builder.Services.AddHttpClient("ConsumerService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ConsumerServiceUrl"]);
});

var app = builder.Build();

// Configurar o pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

// Mapear controllers
app.MapControllers();

app.Run();