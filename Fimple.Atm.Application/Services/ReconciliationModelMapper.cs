using Fimple.Atm.Domain.Constants;
using Fimple.Atm.Domain.Entities;
using Fimple.Atm.Domain.Models;

namespace Fimple.Atm.Application.Services
{
    internal static class ReconciliationModelMapper
    {
        public static ReconciliationModel.ReconciliationItem ToReconciliationItem(Reconciliation reconciliation, IEnumerable<Cassette> cassettes)
        {
            var positions = cassettes.ToDictionary(cassette => cassette.Id, cassette => cassette.Position);
            var lines = reconciliation.Lines
                .Select(line => new ReconciliationModel.LineItem
                {
                    CassetteId = line.CassetteId,
                    CassettePosition = positions.GetValueOrDefault(line.CassetteId),
                    Currency = line.Currency,
                    Denomination = (int)line.Denomination,
                    SystemCount = line.SystemCount,
                    CountedCount = line.CountedCount,
                    Difference = line.Difference
                })
                .OrderBy(line => line.CassettePosition)
                .ThenBy(line => line.Currency)
                .ThenBy(line => line.Denomination)
                .ToList();

            return new ReconciliationModel.ReconciliationItem
            {
                Id = reconciliation.Id,
                AtmId = reconciliation.AtmId,
                Status = reconciliation.Status,
                TotalDifferenceInBaseCurrency = reconciliation.TotalDifferenceInBaseCurrency,
                RequiresApproval = reconciliation.TotalDifferenceInBaseCurrency > ReconciliationRules.ApprovalThresholdInBaseCurrency,
                CreatedBy = reconciliation.CreatedBy,
                CreatedAt = reconciliation.CreatedAt,
                ApprovedBy = reconciliation.ApprovedBy,
                ApprovedAt = reconciliation.ApprovedAt,
                Lines = lines
            };
        }
    }
}
