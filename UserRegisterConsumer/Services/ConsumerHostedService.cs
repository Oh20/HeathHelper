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
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<UserRegistrationConsumer>>();
                var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

                _consumer = new UserRegistrationConsumer(
                    config.GetConnectionString("RabbitMQ"),
                    dbContext,
                    logger
                );

                _consumer.StartConsuming();

                // Mantém o consumidor ativo
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
        }
    }
}