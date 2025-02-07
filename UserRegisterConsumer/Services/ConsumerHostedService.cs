public class ConsumerHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private UserRegistrationConsumer _consumer;

    public ConsumerHostedService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var consumer = scope.ServiceProvider.GetRequiredService<UserRegistrationConsumer>();

        try
        {
            consumer.StartConsuming();

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ConsumerHostedService>>();
            logger.LogError(ex, "Erro ao executar o consumer");
            throw;
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _consumer?.Dispose();
        return base.StopAsync(cancellationToken);
    }
}