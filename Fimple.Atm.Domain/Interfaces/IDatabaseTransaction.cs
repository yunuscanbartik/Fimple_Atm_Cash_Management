namespace Fimple.Atm.Domain.Interfaces
{
    public interface IDatabaseTransaction : IAsyncDisposable
    {
        public Task Commit();
    }
}
