namespace Fimple.Atm.Domain.Exceptions
{
    public class ForbiddenException : AtmException
    {
        public ForbiddenException(string errorCode, string message)
            : base(errorCode, message)
        {

        }
    }
}
