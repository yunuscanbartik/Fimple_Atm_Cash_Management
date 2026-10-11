using Fimple.Atm.Domain.Entities;

namespace Fimple.Atm.Domain.Interfaces
{
    public interface IOutboxRepository
    {
        public Task<List<OutboxEvent>> LockPendingEvents(int batchSize);
        public void Add(OutboxEvent outboxEvent);
    }
}
