using Fimple.Atm.Domain.Interfaces;

namespace Fimple.Atm.Worker.Workers
{
    public class OutboxWorker : PeriodicWorker
    {
        private const string INTERVAL_SECONDS_KEY = "Outbox:IntervalSeconds";

        private readonly ILogger<OutboxWorker> _logger;

        public OutboxWorker(IServiceScopeFactory serviceScopeFactory, IConfiguration configuration, ILogger<OutboxWorker> logger)
            : base(serviceScopeFactory, configuration, logger, INTERVAL_SECONDS_KEY)
        {
            _logger = logger;
        }

        protected override string WorkerName => nameof(OutboxWorker);

        protected override async Task RunCycle(IServiceProvider serviceProvider)
        {
            var outboxProcessor = serviceProvider.GetRequiredService<IOutboxProcessor>();
            var processedCount = await outboxProcessor.ProcessPendingEvents();
            if (processedCount > 0)
            {
                _logger.LogInformation("Processed {ProcessedCount} outbox events.", processedCount);
            }
        }
    }
}
