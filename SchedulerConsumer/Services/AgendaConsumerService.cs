public class AgendaConsumerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private AgendaConsumer _consumer;

    public AgendaConsumerService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var logger = _serviceProvider.GetRequiredService<ILogger<AgendaConsumer>>();
            var config = _serviceProvider.GetRequiredService<IConfiguration>();
            var httpClientFactory = _serviceProvider.GetRequiredService<IHttpClientFactory>();

            _consumer = new AgendaConsumer(
                config.GetConnectionString("RabbitMQ"),
                _serviceProvider,
                logger,
                httpClientFactory,
                config["UserServiceUrl"]
            );

            _consumer.StartConsuming();

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            var logger = _serviceProvider.GetRequiredService<ILogger<AgendaConsumerService>>();
            logger.LogError(ex, "Erro no AgendaConsumerService");
            throw;
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _consumer?.Dispose();
        return base.StopAsync(cancellationToken);
    }
}