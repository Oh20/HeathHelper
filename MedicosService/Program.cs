using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Adiciona suporte a Controllers e Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configuração do RabbitMQ
builder.Services.AddSingleton(sp =>
    new RabbitMQPublisher(builder.Configuration.GetConnectionString("RabbitMQ")));
builder.Services.AddScoped<DoctorService>();

var app = builder.Build();

// Habilita o Swagger
app.UseSwagger();
app.UseSwaggerUI();

// Endpoints
app.MapPost("/register-doctor", async (
    [FromBody] DoctorRegistrationDto doctorDto,
    RabbitMQPublisher publisher) =>
{
    publisher.PublishDoctorRegistration(doctorDto);
    return Results.Ok(new UserRegistrationResponse
    {
        Success = true,
        Message = "Solicitação de registro enviada com sucesso"
    });
});

app.MapPost("/register-patient", async (
    [FromBody] PatientRegistrationDto patientDto,
    RabbitMQPublisher publisher) =>
{
    publisher.PublishPatientRegistration(patientDto);
    return Results.Ok(new UserRegistrationResponse
    {
        Success = true,
        Message = "Solicitação de registro enviada com sucesso"
    });
});

app.Run();