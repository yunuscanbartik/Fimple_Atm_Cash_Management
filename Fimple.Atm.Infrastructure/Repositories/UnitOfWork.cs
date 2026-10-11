using System.Data;
using Fimple.Atm.Domain.Interfaces;
using Fimple.Atm.Infrastructure.PostgreSQL;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Fimple.Atm.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _appDbContext;

        public UnitOfWork(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<IDatabaseTransaction> BeginTransaction()
        {
            var transaction = await _appDbContext.Database.BeginTransactionAsync();
            return new DatabaseTransaction(transaction);
        }

        public async Task<IDatabaseTransaction> BeginRepeatableReadTransaction()
        {
            var transaction = await _appDbContext.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);
            return new DatabaseTransaction(transaction);
        }

        public async Task SaveChanges()
        {
            await _appDbContext.SaveChangesAsync();
        }

        public void ClearTracking()
        {
            _appDbContext.ChangeTracker.Clear();
        }

        private sealed class DatabaseTransaction : IDatabaseTransaction
        {
            private readonly IDbContextTransaction _transaction;

            public DatabaseTransaction(IDbContextTransaction transaction)
            {
                _transaction = transaction;
            }

            public async Task Commit()
            {
                await _transaction.CommitAsync();
            }

            public async ValueTask DisposeAsync()
            {
                await _transaction.DisposeAsync();
            }
        }
    }
}
