namespace Fimple.Atm.Domain.Exceptions
{
    public class NotFoundException : AtmException
    {
        public NotFoundException(string errorCode, string message)
            : base(errorCode, message)
        {

        }
    }
}
