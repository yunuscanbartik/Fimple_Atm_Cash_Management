using Fimple.Atm.Domain.Models;

namespace Fimple.Atm.Domain.Interfaces
{
    public interface IWithdrawalService
    {
        public Task<WithdrawalModel.CreateQuote.Return> CreateQuote(WithdrawalModel.CreateQuote.Request request);
        public Task<WithdrawalModel.GetQuote.Return> GetQuote(WithdrawalModel.GetQuote.Request request);
        public Task<WithdrawalModel.ConfirmQuote.Return> ConfirmQuote(WithdrawalModel.ConfirmQuote.Request request);
        public Task<WithdrawalModel.CancelQuote.Return> CancelQuote(WithdrawalModel.CancelQuote.Request request);
    }
}
