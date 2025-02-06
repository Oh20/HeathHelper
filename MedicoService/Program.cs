using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Configura��o do RabbitMQ
builder.Services.AddSingleton(sp =>
    new RabbitMQPublisher(builder.Configuration.GetConnectionString("RabbitMQ")));

builder.Services.AddScoped<DoctorService>();

var app = builder.Build();


// Endpoint de registro do M�dico

app.MapPost("/register-doctor", async (
    [FromBody] DoctorRegistrationDto doctorDto,
    RabbitMQPublisher publisher) =>
{
    publisher.PublishDoctorRegistration(doctorDto);
    return Results.Ok(new UserRegistrationResponse
    {
        Success = true,
        Message = "Solicita��o de registro enviada com sucesso"
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
        Message = "Solicita��o de registro enviada com sucesso"
    });
});

app.Run();