using Fimple.Atm.Domain.Constants;
using Fimple.Atm.Domain.Enums;
using Fimple.Atm.Domain.Exceptions;
using Fimple.Atm.Domain.Models;

namespace Fimple.Atm.Application.Services
{
    public static class CassetteConfigurationValidator
    {
        public static void Validate(IReadOnlyCollection<AtmModel.CassetteConfigItem> cassetteConfigurations)
        {
            if (cassetteConfigurations.Count != AtmRules.CassetteCount)
            {
                throw Invalid($"An ATM must have exactly {AtmRules.CassetteCount} cassettes.");
            }

            var positions = cassetteConfigurations.Select(configuration => configuration.Position).Distinct().ToList();
            var hasAllPositions = positions.Count == AtmRules.CassetteCount
                && positions.All(position => position >= 1 && position <= AtmRules.CassetteCount);
            if (!hasAllPositions)
            {
                throw Invalid($"Cassette positions must be unique and between 1 and {AtmRules.CassetteCount}.");
            }

            var depositCassettes = cassetteConfigurations.Where(configuration => configuration.Kind == CassetteKind.Deposit).ToList();
            if (depositCassettes.Count != 1)
            {
                throw Invalid("Exactly one cassette must be a deposit cassette.");
            }

            var depositHasNoteType = depositCassettes[0].Currency != null || depositCassettes[0].Denomination != null;
            if (depositHasNoteType)
            {
                throw Invalid("The deposit cassette accepts any currency and denomination, so none may be configured for it.");
            }

            var recyclingCassettes = cassetteConfigurations.Where(configuration => configuration.Kind == CassetteKind.Recycling).ToList();
            var recyclingMissingNoteType = recyclingCassettes.Any(configuration =>
                configuration.Currency == null || configuration.Denomination == null);
            if (recyclingMissingNoteType)
            {
                throw Invalid("Every recycling cassette needs a currency and a denomination.");
            }

            var foreignCurrencyCount = recyclingCassettes.Count(configuration => configuration.Currency != ReconciliationRules.BaseCurrency);
            var isForeignCountAllowed = foreignCurrencyCount >= AtmRules.MinForeignCurrencyRecyclingCassetteCount
                && foreignCurrencyCount <= AtmRules.MaxForeignCurrencyRecyclingCassetteCount;
            if (!isForeignCountAllowed)
            {
                throw Invalid(
                    $"Between {AtmRules.MinForeignCurrencyRecyclingCassetteCount} and {AtmRules.MaxForeignCurrencyRecyclingCassetteCount} " +
                    "recycling cassettes must be configured for a foreign currency (USD or EUR).");
            }
        }

        private static BusinessRuleException Invalid(string message)
        {
            return new BusinessRuleException(ErrorCodes.InvalidCassetteConfiguration, message);
        }
    }
}
