using Fimple.Atm.Domain.Interfaces;

namespace Fimple.Atm.Worker.Workers
{
    public class QuoteExpiryWorker : BackgroundService
    {
        private const string INTERVAL_SECONDS_KEY = "QuoteExpiry:IntervalSeconds";
        private const int DEFAULT_INTERVAL_SECONDS = 5;

        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<QuoteExpiryWorker> _logger;
        private readonly TimeSpan _interval;

        public QuoteExpiryWorker(IServiceScopeFactory serviceScopeFactory, IConfiguration configuration, ILogger<QuoteExpiryWorker> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _interval = TimeSpan.FromSeconds(configuration.GetValue(INTERVAL_SECONDS_KEY, DEFAULT_INTERVAL_SECONDS));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(_interval);

            try
            {
                do
                {
                    await ReleaseExpiredQuotes();
                }
                while (await timer.WaitForNextTickAsync(stoppingToken));
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Quote expiry worker is stopping.");
            }
        }

        private async Task ReleaseExpiredQuotes()
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var quoteExpiryService = scope.ServiceProvider.GetRequiredService<IQuoteExpiryService>();
                var releasedCount = await quoteExpiryService.ReleaseExpiredQuotes();
                if (releasedCount > 0)
                {
                    _logger.LogInformation("Released the reservations of {ReleasedCount} expired quotes.", releasedCount);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Releasing expired quotes failed; it will be retried on the next cycle.");
            }
        }
    }
}
