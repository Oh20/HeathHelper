using Microsoft.EntityFrameworkCore;

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

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHostedService<ConsumerHostedService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.MapGet("/api/medicos/{id}", async (int id, ApplicationDbContext db) =>
{
    var medico = await db.Medicos
        .Where(m => m.Id == id && m.Ativo)
        .FirstOrDefaultAsync();

    if (medico == null)
        return Results.NotFound();

    return Results.Ok(new MedicoDto
    {
        Id = medico.Id,
        Nome = medico.Nome,
        CRM = medico.CRM,
        Especialidade = medico.Especialidade,
        Ativo = medico.Ativo
    });
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
