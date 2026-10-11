namespace Fimple.Atm.Domain.Exceptions
{
    public class BusinessRuleException : AtmException
    {
        public BusinessRuleException(string errorCode, string message)
            : base(errorCode, message)
        {

        }
    }
}
