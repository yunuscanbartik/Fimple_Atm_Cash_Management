using Fimple.Atm.Domain.Entities;

namespace Fimple.Atm.Domain.Interfaces
{
    public interface IDepositRepository
    {
        public void Add(DepositTransaction deposit);
    }
}
