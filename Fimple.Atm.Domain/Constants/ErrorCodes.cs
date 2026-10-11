namespace Fimple.Atm.Domain.Constants
{
    public static class ErrorCodes
    {
        public const string ValidationFailed = "VALIDATION_FAILED";
        public const string InternalError = "INTERNAL_ERROR";
        public const string UniqueViolation = "UNIQUE_VIOLATION";
        public const string ConstraintViolation = "CONSTRAINT_VIOLATION";
        public const string AtmNotFound = "ATM_NOT_FOUND";
        public const string AtmNameAlreadyExists = "ATM_NAME_ALREADY_EXISTS";
        public const string InvalidAtmState = "INVALID_ATM_STATE";
        public const string InvalidCassetteConfiguration = "INVALID_CASSETTE_CONFIGURATION";
        public const string CassetteNotFound = "CASSETTE_NOT_FOUND";
        public const string CassetteNotEmpty = "CASSETTE_NOT_EMPTY";
        public const string CassetteCapacityExceeded = "CASSETTE_CAPACITY_EXCEEDED";
        public const string CassetteNoteMismatch = "CASSETTE_NOTE_MISMATCH";
        public const string InsufficientNotes = "INSUFFICIENT_NOTES";
        public const string NoteTypeRequired = "NOTE_TYPE_REQUIRED";
        public const string EmptyDeposit = "EMPTY_DEPOSIT";
        public const string ReconciliationAwaitingApproval = "RECONCILIATION_AWAITING_APPROVAL";
        public const string QuoteNotFound = "QUOTE_NOT_FOUND";
        public const string QuoteExpired = "QUOTE_EXPIRED";
        public const string QuoteNotConfirmable = "QUOTE_NOT_CONFIRMABLE";
        public const string QuoteOptionRequired = "QUOTE_OPTION_REQUIRED";
        public const string QuoteOptionNotOffered = "QUOTE_OPTION_NOT_OFFERED";
        public const string DailyLimitExceeded = "DAILY_LIMIT_EXCEEDED";
        public const string InsufficientCash = "INSUFFICIENT_CASH";
        public const string IdempotencyKeyRequired = "IDEMPOTENCY_KEY_REQUIRED";
        public const string IdempotencyKeyReused = "IDEMPOTENCY_KEY_REUSED";
    }
}
