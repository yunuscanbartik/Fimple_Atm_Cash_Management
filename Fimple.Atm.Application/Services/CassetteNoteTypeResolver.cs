using Fimple.Atm.Domain.Constants;
using Fimple.Atm.Domain.Entities;
using Fimple.Atm.Domain.Enums;
using Fimple.Atm.Domain.Exceptions;

namespace Fimple.Atm.Application.Services
{
    internal static class CassetteNoteTypeResolver
    {
        public static (Currency Currency, Denomination Denomination) Resolve(Cassette cassette, Currency? requestedCurrency, Denomination? requestedDenomination)
        {
            if (cassette.Kind == CassetteKind.Deposit)
            {
                if (requestedCurrency == null || requestedDenomination == null)
                {
                    throw new BusinessRuleException(ErrorCodes.NoteTypeRequired,
                        $"Cassette {cassette.Position} is the deposit cassette; currency and denomination are required.");
                }

                return (requestedCurrency.Value, requestedDenomination.Value);
            }

            var currencyMismatch = requestedCurrency != null && requestedCurrency != cassette.Currency;
            var denominationMismatch = requestedDenomination != null && requestedDenomination != cassette.Denomination;
            if (currencyMismatch || denominationMismatch)
            {
                throw new BusinessRuleException(ErrorCodes.CassetteNoteMismatch,
                    $"Cassette {cassette.Position} only holds {cassette.Currency} {(int)cassette.Denomination.Value} notes.");
            }

            return (cassette.Currency.Value, cassette.Denomination.Value);
        }

        public static Denomination? ToDenomination(int? denominationValue)
        {
            return denominationValue.HasValue ? (Denomination)denominationValue.Value : null;
        }
    }
}
