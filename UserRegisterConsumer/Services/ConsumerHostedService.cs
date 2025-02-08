public class ConsumerHostedService : IHostedService
{
    private readonly UserRegistrationConsumer _consumer;
    private readonly ILogger<ConsumerHostedService> _logger;

    public ConsumerHostedService(
        UserRegistrationConsumer consumer,
        ILogger<ConsumerHostedService> logger)
    {
        _consumer = consumer;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Iniciando Consumer Service");
            _consumer.StartConsuming();
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao iniciar o Consumer Service");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Parando Consumer Service");
        _consumer.Dispose();
        return Task.CompletedTask;
    }
}