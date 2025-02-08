public class AgendaConsumerService : IHostedService
{
    private readonly AgendaConsumer _consumer;
    private readonly ILogger<AgendaConsumerService> _logger;

    public AgendaConsumerService(
        AgendaConsumer consumer,
        ILogger<AgendaConsumerService> logger)
    {
        _consumer = consumer;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Iniciando o consumo de mensagens...");
            _consumer.StartConsuming();
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao iniciar o consumo de mensagens");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Parando o consumo de mensagens...");
            _consumer.Dispose();
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao parar o consumo de mensagens");
            throw;
        }
    }
}