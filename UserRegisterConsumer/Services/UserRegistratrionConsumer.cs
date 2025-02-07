﻿
using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Identity;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

public class UserRegistrationConsumer : IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<UserRegistrationConsumer> _logger;
    private const string QUEUE_NAME_DOCTOR = "doctor-registration";
    private const string QUEUE_NAME_PATIENT = "patient-registration";

    public UserRegistrationConsumer(
        string connectionString,
        ApplicationDbContext dbContext,
        ILogger<UserRegistrationConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;

        _logger.LogInformation($"Tentando conectar ao RabbitMQ com: {connectionString}");

        try
        {
            var factory = new ConnectionFactory { Uri = new Uri(connectionString) };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _logger.LogInformation("Conexão com RabbitMQ estabelecida com sucesso");

            // Configuração das filas
            _channel.QueueDeclare(QUEUE_NAME_DOCTOR, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueDeclare(QUEUE_NAME_PATIENT, durable: true, exclusive: false, autoDelete: false);
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao conectar com RabbitMQ");
            throw;
        }
    }
    public void StartConsuming()
    {
        // Consumidor para médicos
        var doctorConsumer = new EventingBasicConsumer(_channel);
        doctorConsumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var doctorDto = JsonSerializer.Deserialize<DoctorRegistrationDto>(message);

                var passwordHasher = new PasswordHasher<Medico>();

                var medico = new Medico
                {

                    CPF = doctorDto.CPF,
                    Nome = doctorDto.Nome,
                    Email = doctorDto.Email,
                    CRM = doctorDto.CRM,
                    Numero = doctorDto.Numero,
                    Especialidade = doctorDto.Especialidade,
                    Ativo = true
                };

                medico.Senha = passwordHasher.HashPassword(medico, doctorDto.Senha);


                await _dbContext.Medicos.AddAsync(medico);
                await _dbContext.SaveChangesAsync();

                _channel.BasicAck(ea.DeliveryTag, multiple: false);
                _logger.LogInformation($"Médico registrado com sucesso: {medico.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar registro de médico");
                _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        // Consumidor para pacientes
        var patientConsumer = new EventingBasicConsumer(_channel);
        patientConsumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var patientDto = JsonSerializer.Deserialize<PatientRegistrationDto>(message);

                var passwordHasher = new PasswordHasher<Paciente>();

                var paciente = new Paciente
                {
                    CPF = patientDto.CPF,
                    Nome = patientDto.Nome,
                    Email = patientDto.Email,
                    Telefone = patientDto.Telefone,
                    DataNascimento = patientDto.DataNascimento,
                    Convenio = patientDto.Convenio,
                    NumeroConvenio = patientDto.NumeroConvenio,
                    Ativo = true
                };
                paciente.Senha = passwordHasher.HashPassword(paciente, patientDto.Senha);


                await _dbContext.Pacientes.AddAsync(paciente);
                await _dbContext.SaveChangesAsync();

                _channel.BasicAck(ea.DeliveryTag, multiple: false);
                _logger.LogInformation($"Paciente registrado com sucesso: {paciente.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar registro de paciente");
                _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.BasicConsume(queue: QUEUE_NAME_DOCTOR, autoAck: false, consumer: doctorConsumer);
        _channel.BasicConsume(queue: QUEUE_NAME_PATIENT, autoAck: false, consumer: patientConsumer);
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}