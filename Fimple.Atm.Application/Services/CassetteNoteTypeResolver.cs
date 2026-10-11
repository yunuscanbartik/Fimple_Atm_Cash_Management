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

            var cassetteCurrency = cassette.Currency
                ?? throw new InvalidOperationException($"Recycling cassette {cassette.Position} has no currency configured.");
            var cassetteDenomination = cassette.Denomination
                ?? throw new InvalidOperationException($"Recycling cassette {cassette.Position} has no denomination configured.");

            var currencyMismatch = requestedCurrency != null && requestedCurrency != cassetteCurrency;
            var denominationMismatch = requestedDenomination != null && requestedDenomination != cassetteDenomination;
            if (currencyMismatch || denominationMismatch)
            {
                throw new BusinessRuleException(ErrorCodes.CassetteNoteMismatch,
                    $"Cassette {cassette.Position} only holds {cassetteCurrency} {(int)cassetteDenomination} notes.");
            }

            return (cassetteCurrency, cassetteDenomination);
        }

        public static Denomination? ToDenomination(int? denominationValue)
        {
            return denominationValue.HasValue ? (Denomination)denominationValue.Value : null;
        }
    }
}
