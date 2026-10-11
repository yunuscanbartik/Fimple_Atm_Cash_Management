namespace Fimple.Atm.Domain.Interfaces
{
    public interface IOutboxProcessor
    {
        public Task<int> ProcessPendingEvents();
    }
}
