namespace Fimple.Atm.Infrastructure.PostgreSQL
{
    public static class TableNames
    {
        public const string ATMS = "t_Atms";
        public const string CASSETTES = "t_Cassettes";
        public const string CASSETTE_CONTENTS = "t_CassetteContents";
        public const string CASSETTE_MOVEMENTS = "t_CassetteMovements";
        public const string WITHDRAWAL_QUOTES = "t_WithdrawalQuotes";
        public const string WITHDRAWAL_QUOTE_OPTIONS = "t_WithdrawalQuoteOptions";
        public const string WITHDRAWAL_QUOTE_OPTION_ITEMS = "t_WithdrawalQuoteOptionItems";
        public const string DEPOSIT_TRANSACTIONS = "t_DepositTransactions";
        public const string DEPOSIT_NOTES = "t_DepositNotes";
        public const string RECONCILIATIONS = "t_Reconciliations";
        public const string RECONCILIATION_LINES = "t_ReconciliationLines";
        public const string EXCHANGE_RATES = "t_ExchangeRates";
        public const string OUTBOX_EVENTS = "t_OutboxEvents";
    }
}
