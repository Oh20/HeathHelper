// UserRegistrationConsumer.cs
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Identity;

public class UserRegistrationConsumer : IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UserRegistrationConsumer> _logger;
    private const string QUEUE_NAME_DOCTOR = "doctor-registration";
    private const string QUEUE_NAME_PATIENT = "patient-registration";
    private bool _disposed;

    public UserRegistrationConsumer(
        RabbitMQConfig rabbitConfig,
        IServiceProvider serviceProvider,
        ILogger<UserRegistrationConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = rabbitConfig.HostName,
                Port = rabbitConfig.Port,
                UserName = rabbitConfig.UserName,
                Password = rabbitConfig.Password,
                RequestedHeartbeat = TimeSpan.FromSeconds(60),
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

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
        var doctorConsumer = new EventingBasicConsumer(_channel);
        doctorConsumer.Received += (model, ea) =>
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var doctorDto = JsonSerializer.Deserialize<DoctorRegistrationDto>(message);

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

                var passwordHasher = new PasswordHasher<Medico>();
                medico.Senha = passwordHasher.HashPassword(medico, doctorDto.Senha);

                dbContext.Medicos.Add(medico);
                dbContext.SaveChanges();

                _channel.BasicAck(ea.DeliveryTag, multiple: false);
                _logger.LogInformation($"Médico registrado com sucesso: {medico.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar registro de médico");
                _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        var patientConsumer = new EventingBasicConsumer(_channel);
        patientConsumer.Received += (model, ea) =>
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var patientDto = JsonSerializer.Deserialize<PatientRegistrationDto>(message);

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

                var passwordHasher = new PasswordHasher<Paciente>();
                paciente.Senha = passwordHasher.HashPassword(paciente, patientDto.Senha);

                dbContext.Pacientes.Add(paciente);
                dbContext.SaveChanges();

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
        if (_disposed) return;

        _disposed = true;
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
    }
}