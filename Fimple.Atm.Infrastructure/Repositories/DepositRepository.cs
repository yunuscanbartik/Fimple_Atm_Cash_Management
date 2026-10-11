using Fimple.Atm.Domain.Entities;
using Fimple.Atm.Domain.Interfaces;
using Fimple.Atm.Infrastructure.PostgreSQL;

namespace Fimple.Atm.Infrastructure.Repositories
{
    public class DepositRepository : IDepositRepository
    {
        private readonly AppDbContext _appDbContext;

        public DepositRepository(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public void Add(DepositTransaction deposit)
        {
            _appDbContext.DepositTransactions.Add(deposit);
        }
    }
}
