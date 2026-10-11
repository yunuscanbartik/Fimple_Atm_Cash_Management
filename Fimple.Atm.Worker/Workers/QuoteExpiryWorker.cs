using Fimple.Atm.Domain.Interfaces;

namespace Fimple.Atm.Worker.Workers
{
    public class QuoteExpiryWorker : PeriodicWorker
    {
        private const string INTERVAL_SECONDS_KEY = "QuoteExpiry:IntervalSeconds";

        private readonly ILogger<QuoteExpiryWorker> _logger;

        public QuoteExpiryWorker(IServiceScopeFactory serviceScopeFactory, IConfiguration configuration, ILogger<QuoteExpiryWorker> logger)
            : base(serviceScopeFactory, configuration, logger, INTERVAL_SECONDS_KEY)
        {
            _logger = logger;
        }

        protected override string WorkerName => nameof(QuoteExpiryWorker);

        protected override async Task RunCycle(IServiceProvider serviceProvider)
        {
            var quoteExpiryService = serviceProvider.GetRequiredService<IQuoteExpiryService>();
            var releasedCount = await quoteExpiryService.ReleaseExpiredQuotes();
            if (releasedCount > 0)
            {
                _logger.LogInformation("Released the reservations of {ReleasedCount} expired quotes.", releasedCount);
            }
        }
    }
}
