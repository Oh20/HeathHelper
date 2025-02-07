using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Configuração do RabbitMQ
builder.Services.AddSingleton(sp =>
    new RabbitMQPublisher(builder.Configuration.GetConnectionString("RabbitMQ")));

builder.Services.AddScoped<DoctorService>();

var app = builder.Build();


// Endpoint de registro do Médico

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

// Endpoint de registro do Paciente

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