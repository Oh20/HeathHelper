using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configura��o do RabbitMQ
var rabbitConfig = new RabbitMQConfig
{
    HostName = Environment.GetEnvironmentVariable("RabbitMQ__HostName") ?? "localhost",
    Port = int.Parse(Environment.GetEnvironmentVariable("RabbitMQ__Port") ?? "5672"),
    UserName = Environment.GetEnvironmentVariable("RabbitMQ__UserName") ?? "guest",
    Password = Environment.GetEnvironmentVariable("RabbitMQ__Password") ?? "guest"
};

// Configura��o do SQL Server
var dbConnection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

// Registro dos servi�os
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configura��o do DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        dbConnection,
        sqlServerOptionsAction: sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null
            );
        }
    ),
    ServiceLifetime.Scoped // Especificando explicitamente o lifetime
);

// Registro dos servi�os do Consumer
builder.Services.AddSingleton(rabbitConfig);
builder.Services.AddSingleton<UserRegistrationConsumer>();
builder.Services.AddHostedService<ConsumerHostedService>();

var app = builder.Build();

// Configure o pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseHttpsRedirection();
app.UseAuthorization();

// Endpoints da API
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