using Fimple.Atm.Domain.Constants;
using Fimple.Atm.Domain.Entities;
using Fimple.Atm.Domain.Enums;
using Fimple.Atm.Domain.Exceptions;
using Fimple.Atm.Domain.Interfaces;
using Fimple.Atm.Domain.Models;

namespace Fimple.Atm.Application.Services
{
    public class ReportService : IReportService
    {
        private readonly IAtmRepository _atmRepository;
        private readonly IExchangeRateRepository _exchangeRateRepository;
        private readonly TimeProvider _timeProvider;

        public ReportService(IAtmRepository atmRepository, IExchangeRateRepository exchangeRateRepository, TimeProvider timeProvider)
        {
            _atmRepository = atmRepository;
            _exchangeRateRepository = exchangeRateRepository;
            _timeProvider = timeProvider;
        }

        public async Task<ReportModel.Operations.Return> GetOperationsReport(ReportModel.Operations.Request request)
        {
            var atms = await LoadAtms(request.AtmId);
            var rates = await _exchangeRateRepository.GetRatesToBaseCurrency();

            var atmReports = atms.Select(atm => BuildAtmReport(atm, rates)).ToList();
            var overallTotals = SumTotals(atmReports.SelectMany(atmReport => atmReport.Totals));
            var generatedAt = _timeProvider.GetUtcNow().UtcDateTime;
            var totalInBaseCurrency = atmReports.Sum(atmReport => atmReport.TotalInBaseCurrency);

            return new ReportModel.Operations.Return
            {
                GeneratedAt = generatedAt,
                BaseCurrency = ReconciliationRules.BASE_CURRENCY,
                Atms = atmReports,
                Totals = overallTotals,
                TotalInBaseCurrency = totalInBaseCurrency
            };
        }

        private async Task<List<AtmMachine>> LoadAtms(Guid? atmId)
        {
            var atms = await _atmRepository.GetAllWithCassettesReadOnly(atmId);
            if (atmId.HasValue && atms.Count == 0)
            {
                throw new NotFoundException(ErrorCodes.ATM_NOT_FOUND, $"ATM not found: {atmId.Value}");
            }

            return atms;
        }

        private static ReportModel.AtmReportItem BuildAtmReport(AtmMachine atm, Dictionary<Currency, decimal> rates)
        {
            var contents = atm.Cassettes.SelectMany(cassette => cassette.Contents).ToList();
            var totals = Enum.GetValues<Currency>()
                .Select(currency => BuildCurrencyTotal(currency, contents.Where(content => content.Currency == currency).ToList()))
                .ToList();
            var cassetteItems = AtmModelMapper.ToCassetteItems(atm);
            var totalInBaseCurrency = ConvertToBaseCurrency(totals, rates);

            return new ReportModel.AtmReportItem
            {
                AtmId = atm.Id,
                Name = atm.Name,
                Status = atm.Status,
                Cassettes = cassetteItems,
                Totals = totals,
                TotalInBaseCurrency = totalInBaseCurrency
            };
        }

        private static ReportModel.CurrencyTotalItem BuildCurrencyTotal(Currency currency, List<CassetteContent> contents)
        {
            var noteCount = contents.Sum(content => content.NoteCount);
            var reservedCount = contents.Sum(content => content.ReservedCount);
            var availableCount = contents.Sum(content => content.AvailableCount);
            var amount = contents.Sum(content => (decimal)content.NoteCount * (int)content.Denomination);

            return new ReportModel.CurrencyTotalItem
            {
                Currency = currency,
                NoteCount = noteCount,
                ReservedCount = reservedCount,
                AvailableCount = availableCount,
                Amount = amount
            };
        }

        private static List<ReportModel.CurrencyTotalItem> SumTotals(IEnumerable<ReportModel.CurrencyTotalItem> totals)
        {
            return totals
                .GroupBy(total => total.Currency)
                .Select(SumCurrencyTotals)
                .OrderBy(total => total.Currency)
                .ToList();
        }

        private static ReportModel.CurrencyTotalItem SumCurrencyTotals(IGrouping<Currency, ReportModel.CurrencyTotalItem> group)
        {
            var noteCount = group.Sum(total => total.NoteCount);
            var reservedCount = group.Sum(total => total.ReservedCount);
            var availableCount = group.Sum(total => total.AvailableCount);
            var amount = group.Sum(total => total.Amount);

            return new ReportModel.CurrencyTotalItem
            {
                Currency = group.Key,
                NoteCount = noteCount,
                ReservedCount = reservedCount,
                AvailableCount = availableCount,
                Amount = amount
            };
        }

        private static decimal ConvertToBaseCurrency(IEnumerable<ReportModel.CurrencyTotalItem> totals, Dictionary<Currency, decimal> rates)
        {
            var totalInBaseCurrency = 0m;
            foreach (var total in totals)
            {
                if (!rates.TryGetValue(total.Currency, out var rate))
                {
                    throw new BusinessRuleException(ErrorCodes.EXCHANGE_RATE_MISSING, $"No exchange rate is configured for {total.Currency}.");
                }

                totalInBaseCurrency += total.Amount * rate;
            }

            return totalInBaseCurrency;
        }
    }
}
