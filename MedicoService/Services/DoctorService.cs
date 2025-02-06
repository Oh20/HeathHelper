public class DoctorService
{
    private readonly RabbitMQPublisher _rabbitMQPublisher;

    public DoctorService(RabbitMQPublisher rabbitMQPublisher)
    {
        _rabbitMQPublisher = rabbitMQPublisher;
    }

    public async Task<bool> RegisterDoctor(DoctorRegistrationDto doctorDto)
    {
        // Validações básicas
        if (string.IsNullOrEmpty(doctorDto.CPF) ||
            string.IsNullOrEmpty(doctorDto.CRM) ||
            string.IsNullOrEmpty(doctorDto.Email))
        {
            return false;
        }

        // Publica a mensagem na fila do RabbitMQ
        _rabbitMQPublisher.PublishDoctorRegistration(doctorDto);
        return true;
    }
}