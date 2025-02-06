using RabbitMQ.Client;
using System.Text.Json;
using System.Text;

public class AgendaProducer : IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<AgendaProducer> _logger;

    // Filas para diferentes operações
    private const string QUEUE_NAME_SLOTS = "agenda-slots";
    private const string QUEUE_NAME_UPDATE = "agenda-update";
    private const string QUEUE_NAME_CONSULTA = "consulta-nova";
    private const string QUEUE_NAME_CONSULTA_UPDATE = "consulta-update";
    private const string QUEUE_NAME_CONSULTA_QUERY = "consulta-query";
    private const string QUEUE_NAME_SLOTS_QUERY = "slots-query";

    public AgendaProducer(string connectionString, ILogger<AgendaProducer> logger)
    {
        _logger = logger;
        var factory = new ConnectionFactory { Uri = new Uri(connectionString) };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Declarar todas as filas
        _channel.QueueDeclare(QUEUE_NAME_SLOTS, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare(QUEUE_NAME_UPDATE, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare(QUEUE_NAME_CONSULTA, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare(QUEUE_NAME_CONSULTA_UPDATE, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare(QUEUE_NAME_CONSULTA_QUERY, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare(QUEUE_NAME_SLOTS_QUERY, durable: true, exclusive: false, autoDelete: false);
    }

    // Método para publicar novos slots de agenda
    public void PublishAgenda(AgendaDto agenda)
    {
        try
        {
            var message = JsonSerializer.Serialize(agenda);
            PublishMessage(QUEUE_NAME_SLOTS, message);
            _logger.LogInformation($"Slot de agenda publicado para médico {agenda.MedicoId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao publicar slot de agenda");
            throw;
        }
    }

    // Método para atualizar slots existentes
    public void PublishAgendaUpdate(AgendaInputDto agenda, int medicoId, int agendaId)
    {
        try
        {
            var updateMessage = new AgendaUpdateDto
            {
                AgendaId = agendaId,
                MedicoId = medicoId,
                DataHoraInicio = agenda.DataHoraInicio,
                DataHoraFim = agenda.DataHoraFim,
                TipoAgenda = agenda.TipoAgenda,
                Observacao = agenda.Observacao,
                Especialidade = agenda.Especialidade
            };

            var message = JsonSerializer.Serialize(updateMessage);
            PublishMessage(QUEUE_NAME_UPDATE, message);
            _logger.LogInformation($"Atualização de agenda publicada. AgendaId: {agendaId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao publicar atualização de agenda");
            throw;
        }
    }

    // Método para publicar nova consulta
    public void PublishNovaConsulta(NovaConsultaDto consultaDto)
    {
        try
        {
            var message = JsonSerializer.Serialize(consultaDto);
            var body = Encoding.UTF8.GetBytes(message);

            _channel.BasicPublish(
                exchange: "",
                routingKey: QUEUE_NAME_CONSULTA,
                basicProperties: null,
                body: body
            );

            _logger.LogInformation($"Nova consulta publicada para processamento: AgendaId {consultaDto.AgendaId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao publicar nova consulta");
            throw;
        }
    }

    // Método para atualizar status de consulta
    public void PublishConsultaUpdate(int medicoId, int consultaId, ConsultaUpdateDto updateDto)
    {
        try
        {
            var consultaMessage = new
            {
                ConsultaId = consultaId,
                MedicoId = medicoId,
                NovoStatus = updateDto.NovoStatus,
                Observacoes = updateDto.Observacoes,
                MotivoCancelamento = updateDto.MotivoCancelamento
            };

            var message = JsonSerializer.Serialize(consultaMessage);
            var body = Encoding.UTF8.GetBytes(message);

            PublishMessage(QUEUE_NAME_CONSULTA_UPDATE, message);

            _logger.LogInformation($"Atualização de consulta publicada. ConsultaId: {consultaId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao publicar atualização de consulta");
            throw;
        }
    }

    // Método para consultar slots disponíveis
    public void PublishSlotsQuery(string especialidade, DateTime data)
    {
        try
        {
            var queryMessage = new
            {
                Especialidade = especialidade,
                Data = data
            };

            var message = JsonSerializer.Serialize(queryMessage);
            PublishMessage(QUEUE_NAME_SLOTS_QUERY, message);
            _logger.LogInformation($"Consulta de slots publicada para {especialidade}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao publicar consulta de slots");
            throw;
        }
    }

    // Método para consultar consultas do médico
    public void PublishConsultaQuery(int medicoId, DateTime? data, StatusConsulta? status)
    {
        try
        {
            var queryMessage = new
            {
                MedicoId = medicoId,
                Data = data,
                Status = status
            };

            var message = JsonSerializer.Serialize(queryMessage);
            PublishMessage(QUEUE_NAME_CONSULTA_QUERY, message);
            _logger.LogInformation($"Consulta de agendamentos publicada para médico {medicoId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao publicar consulta de agendamentos");
            throw;
        }
    }

    // Método para consultar consultas do paciente
    public void PublishConsultasPacienteQuery(int pacienteId)
    {
        try
        {
            var queryMessage = new { PacienteId = pacienteId };
            var message = JsonSerializer.Serialize(queryMessage);
            PublishMessage(QUEUE_NAME_CONSULTA_QUERY, message);
            _logger.LogInformation($"Consulta de agendamentos publicada para paciente {pacienteId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao publicar consulta de agendamentos do paciente");
            throw;
        }
    }

    // Método auxiliar para publicar mensagens
    private void PublishMessage(string queueName, string message)
    {
        var body = Encoding.UTF8.GetBytes(message);
        _channel.BasicPublish(
            exchange: "",
            routingKey: queueName,
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