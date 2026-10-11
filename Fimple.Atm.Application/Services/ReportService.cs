using Fimple.Atm.Domain.Constants;
using Fimple.Atm.Domain.Entities;
using Fimple.Atm.Domain.Enums;
using Fimple.Atm.Domain.Exceptions;
using Fimple.Atm.Domain.Interfaces;
using Fimple.Atm.Domain.Models;
using Fimple.Atm.Infrastructure.PostgreSQL;
using Microsoft.EntityFrameworkCore;

namespace Fimple.Atm.Application.Services
{
    public class ReportService : IReportService
    {
        private readonly AppDbContext _appDbContext;
        private readonly TimeProvider _timeProvider;

        public ReportService(AppDbContext appDbContext, TimeProvider timeProvider)
        {
            _appDbContext = appDbContext;
            _timeProvider = timeProvider;
        }

        public async Task<ReportModel.Operations.Return> GetOperationsReport(ReportModel.Operations.Request request)
        {
            var atms = await LoadAtms(request.AtmId);
            var rates = await _appDbContext.ExchangeRates
                .AsNoTracking()
                .ToDictionaryAsync(exchangeRate => exchangeRate.Currency, exchangeRate => exchangeRate.RateToBaseCurrency);

            var atmReports = atms.Select(atm => BuildAtmReport(atm, rates)).ToList();
            var overallTotals = SumTotals(atmReports.SelectMany(atmReport => atmReport.Totals));

            return new ReportModel.Operations.Return
            {
                GeneratedAt = _timeProvider.GetUtcNow().UtcDateTime,
                BaseCurrency = ReconciliationRules.BaseCurrency,
                Atms = atmReports,
                Totals = overallTotals,
                TotalInBaseCurrency = atmReports.Sum(atmReport => atmReport.TotalInBaseCurrency)
            };
        }

        private async Task<List<AtmMachine>> LoadAtms(Guid? atmId)
        {
            var query = _appDbContext.AtmMachines
                .AsNoTracking()
                .Include(atm => atm.Cassettes)
                .ThenInclude(cassette => cassette.Contents)
                .AsQueryable();

            if (atmId.HasValue)
            {
                query = query.Where(atm => atm.Id == atmId.Value);
            }

            var atms = await query.OrderBy(atm => atm.Name).ToListAsync();
            if (atmId.HasValue && atms.Count == 0)
            {
                throw new NotFoundException(ErrorCodes.AtmNotFound, $"ATM not found: {atmId.Value}");
            }

            return atms;
        }

        private static ReportModel.AtmReportItem BuildAtmReport(AtmMachine atm, Dictionary<Currency, decimal> rates)
        {
            var contents = atm.Cassettes.SelectMany(cassette => cassette.Contents).ToList();
            var totals = Enum.GetValues<Currency>()
                .Select(currency => BuildCurrencyTotal(currency, contents.Where(content => content.Currency == currency).ToList()))
                .ToList();

            return new ReportModel.AtmReportItem
            {
                AtmId = atm.Id,
                Name = atm.Name,
                Status = atm.Status,
                Cassettes = AtmModelMapper.ToCassetteItems(atm),
                Totals = totals,
                TotalInBaseCurrency = ConvertToBaseCurrency(totals, rates)
            };
        }

        private static ReportModel.CurrencyTotalItem BuildCurrencyTotal(Currency currency, List<CassetteContent> contents)
        {
            return new ReportModel.CurrencyTotalItem
            {
                Currency = currency,
                NoteCount = contents.Sum(content => content.NoteCount),
                ReservedCount = contents.Sum(content => content.ReservedCount),
                AvailableCount = contents.Sum(content => content.AvailableCount),
                Amount = contents.Sum(content => (decimal)content.NoteCount * (int)content.Denomination)
            };
        }

        private static List<ReportModel.CurrencyTotalItem> SumTotals(IEnumerable<ReportModel.CurrencyTotalItem> totals)
        {
            return totals
                .GroupBy(total => total.Currency)
                .Select(group => new ReportModel.CurrencyTotalItem
                {
                    Currency = group.Key,
                    NoteCount = group.Sum(total => total.NoteCount),
                    ReservedCount = group.Sum(total => total.ReservedCount),
                    AvailableCount = group.Sum(total => total.AvailableCount),
                    Amount = group.Sum(total => total.Amount)
                })
                .OrderBy(total => total.Currency)
                .ToList();
        }

        private static decimal ConvertToBaseCurrency(IEnumerable<ReportModel.CurrencyTotalItem> totals, Dictionary<Currency, decimal> rates)
        {
            var totalInBaseCurrency = 0m;
            foreach (var total in totals)
            {
                if (!rates.TryGetValue(total.Currency, out var rate))
                {
                    throw new BusinessRuleException(ErrorCodes.ExchangeRateMissing, $"No exchange rate is configured for {total.Currency}.");
                }

                totalInBaseCurrency += total.Amount * rate;
            }

            return totalInBaseCurrency;
        }
    }
}
