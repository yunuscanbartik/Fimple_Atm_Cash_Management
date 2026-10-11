namespace Fimple.Atm.Domain.Constants
{
    public static class ErrorCodes
    {
        public const string VALIDATION_FAILED = "VALIDATION_FAILED";
        public const string INTERNAL_ERROR = "INTERNAL_ERROR";
        public const string UNIQUE_VIOLATION = "UNIQUE_VIOLATION";
        public const string CONSTRAINT_VIOLATION = "CONSTRAINT_VIOLATION";
        public const string ATM_NOT_FOUND = "ATM_NOT_FOUND";
        public const string ATM_NAME_ALREADY_EXISTS = "ATM_NAME_ALREADY_EXISTS";
        public const string INVALID_ATM_STATE = "INVALID_ATM_STATE";
        public const string INVALID_CASSETTE_CONFIGURATION = "INVALID_CASSETTE_CONFIGURATION";
        public const string CASSETTE_NOT_FOUND = "CASSETTE_NOT_FOUND";
        public const string CASSETTE_NOT_EMPTY = "CASSETTE_NOT_EMPTY";
        public const string CASSETTE_CAPACITY_EXCEEDED = "CASSETTE_CAPACITY_EXCEEDED";
        public const string CASSETTE_NOTE_MISMATCH = "CASSETTE_NOTE_MISMATCH";
        public const string INSUFFICIENT_NOTES = "INSUFFICIENT_NOTES";
        public const string NOTE_TYPE_REQUIRED = "NOTE_TYPE_REQUIRED";
        public const string EMPTY_DEPOSIT = "EMPTY_DEPOSIT";
        public const string USER_HEADER_REQUIRED = "USER_HEADER_REQUIRED";
        public const string RECONCILIATION_NOT_FOUND = "RECONCILIATION_NOT_FOUND";
        public const string RECONCILIATION_NOT_PENDING = "RECONCILIATION_NOT_PENDING";
        public const string FOUR_EYES_VIOLATION = "FOUR_EYES_VIOLATION";
        public const string INVALID_RECONCILIATION_COUNTS = "INVALID_RECONCILIATION_COUNTS";
        public const string EXCHANGE_RATE_MISSING = "EXCHANGE_RATE_MISSING";
        public const string RECONCILIATION_AWAITING_APPROVAL = "RECONCILIATION_AWAITING_APPROVAL";
        public const string QUOTE_NOT_FOUND = "QUOTE_NOT_FOUND";
        public const string QUOTE_EXPIRED = "QUOTE_EXPIRED";
        public const string QUOTE_NOT_CONFIRMABLE = "QUOTE_NOT_CONFIRMABLE";
        public const string QUOTE_OPTION_REQUIRED = "QUOTE_OPTION_REQUIRED";
        public const string QUOTE_OPTION_NOT_OFFERED = "QUOTE_OPTION_NOT_OFFERED";
        public const string DAILY_LIMIT_EXCEEDED = "DAILY_LIMIT_EXCEEDED";
        public const string INSUFFICIENT_CASH = "INSUFFICIENT_CASH";
        public const string IDEMPOTENCY_KEY_REQUIRED = "IDEMPOTENCY_KEY_REQUIRED";
        public const string IDEMPOTENCY_KEY_REUSED = "IDEMPOTENCY_KEY_REUSED";
    }
}
