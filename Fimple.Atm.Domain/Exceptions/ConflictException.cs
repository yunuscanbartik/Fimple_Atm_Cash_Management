namespace Fimple.Atm.Domain.Exceptions
{
    public class ConflictException : AtmException
    {
        public ConflictException(string errorCode, string message)
            : base(errorCode, message)
        {

        }
    }
}
