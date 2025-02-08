using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

var rabbitMqConfig = new
{
    HostName = Environment.GetEnvironmentVariable("RabbitMQ__HostName") ?? "rabbitmq-service",
    Port = Environment.GetEnvironmentVariable("RabbitMQ__Port") ?? "5672",
    UserName = Environment.GetEnvironmentVariable("RabbitMQ__UserName") ?? "guest",
    Password = Environment.GetEnvironmentVariable("RabbitMQ__Password") ?? "guest"
};
builder.Services.AddSingleton<UserRegistrationConsumer>(sp =>
{
    var dbContext = sp.GetRequiredService<ApplicationDbContext>();
    var logger = sp.GetRequiredService<ILogger<UserRegistrationConsumer>>();

    return new UserRegistrationConsumer(rabbitMqConfig, dbContext, logger);
});

// Configuração do SQL Server
var dbConnection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(dbConnection));

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
