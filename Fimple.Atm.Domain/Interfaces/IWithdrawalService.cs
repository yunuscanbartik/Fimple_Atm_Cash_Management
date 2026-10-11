using Fimple.Atm.Domain.Models;

namespace Fimple.Atm.Domain.Interfaces
{
    public interface IWithdrawalService
    {
        public Task<WithdrawalModel.QuoteItem> CreateQuote(WithdrawalModel.CreateQuote.Request request);
        public Task<WithdrawalModel.QuoteItem> GetQuote(WithdrawalModel.GetQuote.Request request);
        public Task<WithdrawalModel.ConfirmItem> ConfirmQuote(WithdrawalModel.ConfirmQuote.Request request);
        public Task<WithdrawalModel.QuoteItem> CancelQuote(WithdrawalModel.CancelQuote.Request request);
    }
}
