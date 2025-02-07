using RabbitMQ.Client;
using System.Text.Json;
using System.Text;

public class RabbitMQPublisher : IDisposable
{
    private readonly IModel _channel;
    private readonly IConnection _connection;

    private const string QUEUE_NAME_DOCTOR = "doctor-registration";
    private const string QUEUE_NAME_PATIENT = "patient-registration";

    public RabbitMQPublisher(string connectionString)
    {
        var factory = new ConnectionFactory { Uri = new Uri(connectionString) };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Declarar filas para médicos e pacientes
        _channel.QueueDeclare(QUEUE_NAME_DOCTOR, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare(QUEUE_NAME_PATIENT, durable: true, exclusive: false, autoDelete: false);
    }

    public void PublishDoctorRegistration(DoctorRegistrationDto doctor)
    {
        var message = JsonSerializer.Serialize(doctor);
        var body = Encoding.UTF8.GetBytes(message);

        _channel.BasicPublish(
            exchange: "",
            routingKey: QUEUE_NAME_DOCTOR,
            basicProperties: null,
            body: body
        );
    }

    public void PublishPatientRegistration(PatientRegistrationDto patient)
    {
        var message = JsonSerializer.Serialize(patient);
        var body = Encoding.UTF8.GetBytes(message);

        _channel.BasicPublish(
            exchange: "",
            routingKey: QUEUE_NAME_PATIENT,
            basicProperties: null,
            body: body
        );
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}