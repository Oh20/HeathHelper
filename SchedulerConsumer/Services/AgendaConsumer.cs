using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http;
using System.Net.Http;

public class AgendaConsumer : IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AgendaConsumer> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _userServiceUrl;

    private const string QUEUE_NAME_SLOTS = "agenda-slots";
    private const string QUEUE_NAME_UPDATE = "agenda-update";
    private const string QUEUE_NAME_CONSULTA = "consulta-nova";
    private const string QUEUE_NAME_CONSULTA_UPDATE = "consulta-update";

    public class RabbitMQConfig
    {
        public string HostName { get; set; }
        public int Port { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
    public AgendaConsumer(
        RabbitMQConfig rabbitConfig,
        IServiceProvider serviceProvider,
        ILogger<AgendaConsumer> logger,
        IHttpClientFactory httpClientFactory,
        string userServiceUrl)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _userServiceUrl = userServiceUrl;

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

            _logger.LogInformation($"Tentando conectar ao RabbitMQ em {factory.HostName}:{factory.Port}");

            var retryCount = 5;
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    _connection = factory.CreateConnection();
                    _channel = _connection.CreateModel();

                    ConfigureQueues();
                    _logger.LogInformation("Conexão com RabbitMQ estabelecida com sucesso");
                    break;
                }
                catch (Exception ex) when (i < retryCount - 1)
                {
                    _logger.LogWarning($"Tentativa {i + 1} de {retryCount} falhou. Erro: {ex.Message}");
                    Thread.Sleep(TimeSpan.FromSeconds(5 * (i + 1))); // Backoff exponencial
                }
            }

            if (_connection == null || _channel == null)
            {
                throw new Exception("Não foi possível estabelecer conexão com o RabbitMQ após várias tentativas");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao conectar com RabbitMQ");
            throw;
        }
    }

    private void ConfigureQueues()
    {
        _channel.QueueDeclare(QUEUE_NAME_SLOTS, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare(QUEUE_NAME_UPDATE, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare(QUEUE_NAME_CONSULTA, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare(QUEUE_NAME_CONSULTA_UPDATE, durable: true, exclusive: false, autoDelete: false);
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
    }

    private async Task<MedicoDto> GetMedicoById(int medicoId)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient("UserService");
            var response = await client.GetAsync($"/api/medicos/{medicoId}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<MedicoDto>();
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar médico");
            return null;
        }
    }

    public void StartConsuming()
    {
        var slotsConsumer = new EventingBasicConsumer(_channel);
        slotsConsumer.Received += async (model, ea) =>
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var agendaDto = JsonSerializer.Deserialize<AgendaDto>(message);

                var medico = await GetMedicoById(agendaDto.MedicoId);
                if (medico == null)
                {
                    _logger.LogWarning($"Médico não encontrado: {agendaDto.MedicoId}");
                    _channel.BasicNack(ea.DeliveryTag, false, false);
                    return;
                }

                var agenda = new Agenda
                {
                    MedicoId = agendaDto.MedicoId,
                    DataHoraInicio = agendaDto.DataHoraInicio,
                    DataHoraFim = agendaDto.DataHoraFim,
                    Disponivel = true,
                    TipoAgenda = TipoAgenda.Disponivel,
                    Especialidade = agendaDto.Especialidade ?? medico.Especialidade  // Fallback para especialidade do médico
                };

                await dbContext.Agendas.AddAsync(agenda);
                await dbContext.SaveChangesAsync();

                _channel.BasicAck(ea.DeliveryTag, false);
                _logger.LogInformation($"Novo slot de agenda criado para médico: {medico.Nome}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar novo slot de agenda");
                _channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        var updateConsumer = new EventingBasicConsumer(_channel);
        updateConsumer.Received += async (model, ea) =>
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var updateDto = JsonSerializer.Deserialize<AgendaUpdateDto>(message);

                var agenda = await dbContext.Agendas
                    .FirstOrDefaultAsync(a =>
                        a.Id == updateDto.AgendaId &&
                        a.MedicoId == updateDto.MedicoId);

                if (agenda == null)
                {
                    _logger.LogWarning($"Agenda não encontrada: {updateDto.AgendaId}");
                    _channel.BasicNack(ea.DeliveryTag, false, false);
                    return;
                }

                agenda.DataHoraInicio = updateDto.DataHoraInicio;
                agenda.DataHoraFim = updateDto.DataHoraFim;
                agenda.Disponivel = updateDto.TipoAgenda == TipoAgenda.Disponivel;
                agenda.TipoAgenda = updateDto.TipoAgenda;
                agenda.Observacao = updateDto.Observacao;
                agenda.Especialidade = updateDto.Especialidade;

                await dbContext.SaveChangesAsync();

                _channel.BasicAck(ea.DeliveryTag, false);
                _logger.LogInformation($"Agenda atualizada: {agenda.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar atualização de agenda");
                _channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        var consultaConsumer = new EventingBasicConsumer(_channel);
        consultaConsumer.Received += async (model, ea) =>
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var consultaDto = JsonSerializer.Deserialize<NovaConsultaDto>(message);

                // Verifica se o slot está disponível
                var agenda = await dbContext.Agendas
                    .FirstOrDefaultAsync(a =>
                        a.Id == consultaDto.AgendaId &&
                        a.Disponivel);

                if (agenda == null)
                {
                    _logger.LogWarning($"Slot de agenda {consultaDto.AgendaId} não encontrado ou indisponível");
                    _channel.BasicNack(ea.DeliveryTag, false, false);
                    return;
                }

                // Cria a consulta
                var consulta = new Consulta
                {
                    AgendaId = consultaDto.AgendaId,
                    PacienteId = consultaDto.PacienteId,
                    MedicoId = agenda.MedicoId,
                    DataConsulta = agenda.DataHoraInicio,
                    DataSolicitacao = DateTime.Now,
                    Status = StatusConsulta.Solicitada,
                    Observacoes = consultaDto.Observacoes,
                    Especialidade = agenda.Especialidade
                };

                // Marca o slot como indisponível
                agenda.Disponivel = false;

                // Persiste as mudanças
                await dbContext.Consultas.AddAsync(consulta);
                await dbContext.SaveChangesAsync();

                _channel.BasicAck(ea.DeliveryTag, false);
                _logger.LogInformation($"Nova consulta {consulta.Id} criada com sucesso para o médico {agenda.MedicoId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar nova consulta");
                _channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        var consultaUpdateConsumer = new EventingBasicConsumer(_channel);
        consultaUpdateConsumer.Received += async (model, ea) =>
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var updateDto = JsonSerializer.Deserialize<ConsultaUpdateDto>(message);

                var consulta = await dbContext.Consultas
                    .FirstOrDefaultAsync(c =>
                        c.Id == updateDto.ConsultaId &&
                        c.MedicoId == updateDto.MedicoId);

                if (consulta == null)
                {
                    _logger.LogWarning($"Consulta {updateDto.ConsultaId} não encontrada para o médico {updateDto.MedicoId}");
                    _channel.BasicNack(ea.DeliveryTag, false, false);
                    return;
                }

                consulta.Status = updateDto.NovoStatus;
                consulta.Observacoes = updateDto.Observacoes;

                switch (updateDto.NovoStatus)
                {
                    case StatusConsulta.Confirmada:
                        consulta.DataConfirmacao = DateTime.Now;
                        break;

                    case StatusConsulta.Recusada:
                    case StatusConsulta.Cancelada:
                        consulta.DataCancelamento = DateTime.Now;
                        consulta.MotivoCancelamento = updateDto.MotivoCancelamento;

                        // Liberar o slot da agenda
                        var agenda = await dbContext.Agendas.FindAsync(consulta.AgendaId);
                        if (agenda != null)
                        {
                            agenda.Disponivel = true;
                        }
                        break;
                }

                await dbContext.SaveChangesAsync();

                _channel.BasicAck(ea.DeliveryTag, false);
                _logger.LogInformation($"Status da consulta {consulta.Id} atualizado para {consulta.Status}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar atualização de status da consulta");
                _channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        _channel.BasicConsume(
            queue: QUEUE_NAME_SLOTS,
            autoAck: false,
            consumer: slotsConsumer);

        _channel.BasicConsume(
            queue: QUEUE_NAME_UPDATE,
            autoAck: false,
            consumer: updateConsumer);

        _channel.BasicConsume(
            queue: QUEUE_NAME_CONSULTA,
            autoAck: false,
            consumer: consultaConsumer);

        _channel.BasicConsume(
            queue: QUEUE_NAME_CONSULTA_UPDATE,
            autoAck: false,
            consumer: consultaUpdateConsumer);

    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}