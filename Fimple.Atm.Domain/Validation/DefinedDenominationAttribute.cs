using System.ComponentModel.DataAnnotations;
using Fimple.Atm.Domain.Enums;

namespace Fimple.Atm.Domain.Validation
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DefinedDenominationAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value == null) return true;
            return value is int denominationValue && Enum.IsDefined(typeof(Denomination), denominationValue);
        }

        public override string FormatErrorMessage(string name)
        {
            var supportedValues = string.Join(", ", Enum.GetValues<Denomination>().Select(denomination => (int)denomination));
            return $"{name} must be one of: {supportedValues}.";
        }
    }
}
