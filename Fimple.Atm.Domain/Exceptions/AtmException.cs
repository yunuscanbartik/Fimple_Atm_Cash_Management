namespace Fimple.Atm.Domain.Exceptions
{
    public abstract class AtmException : Exception
    {
        public string ErrorCode { get; }

        protected AtmException(string errorCode, string message)
            : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}
