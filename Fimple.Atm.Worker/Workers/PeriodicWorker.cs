namespace Fimple.Atm.Worker.Workers
{
    public abstract class PeriodicWorker : BackgroundService
    {
        private const int DEFAULT_INTERVAL_SECONDS = 5;

        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger _logger;
        private readonly TimeSpan _interval;

        protected PeriodicWorker(IServiceScopeFactory serviceScopeFactory, IConfiguration configuration, ILogger logger, string intervalSecondsKey)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _interval = TimeSpan.FromSeconds(configuration.GetValue(intervalSecondsKey, DEFAULT_INTERVAL_SECONDS));
        }

        protected abstract string WorkerName { get; }

        protected abstract Task RunCycle(IServiceProvider serviceProvider);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(_interval);

            try
            {
                do
                {
                    await RunCycleSafely();
                }
                while (await timer.WaitForNextTickAsync(stoppingToken));
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("{WorkerName} is stopping.", WorkerName);
            }
        }

        private async Task RunCycleSafely()
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                await RunCycle(scope.ServiceProvider);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "{WorkerName} cycle failed; it will be retried on the next cycle.", WorkerName);
            }
        }
    }
}
