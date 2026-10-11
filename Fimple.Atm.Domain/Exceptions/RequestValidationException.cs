namespace Fimple.Atm.Domain.Exceptions
{
    public class RequestValidationException : AtmException
    {
        public RequestValidationException(string errorCode, string message)
            : base(errorCode, message)
        {

        }
    }
}
