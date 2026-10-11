using Fimple.Atm.Domain.Models;

namespace Fimple.Atm.Domain.Interfaces
{
    public interface IConsistencyService
    {
        public Task<ConsistencyModel.Check.Return> CheckConsistency(ConsistencyModel.Check.Request request);
    }
}
