using Fimple.Atm.Domain.Models;

namespace Fimple.Atm.Domain.Interfaces
{
    public interface IDepositService
    {
        public Task<DepositModel.Add.Return> AddDeposit(DepositModel.Add.Request request);
    }
}
