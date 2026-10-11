using Fimple.Atm.Domain.Constants;
using Fimple.Atm.Domain.Entities;
using Fimple.Atm.Domain.Models;

namespace Fimple.Atm.Application.Services
{
    internal static class ReconciliationModelMapper
    {
        public static ReconciliationModel.ReconciliationItem ToReconciliationItem(Reconciliation reconciliation, IEnumerable<Cassette> cassettes)
        {
            return new ReconciliationModel.ReconciliationItem
            {
                Id = reconciliation.Id,
                AtmId = reconciliation.AtmId,
                Status = reconciliation.Status,
                TotalDifferenceInBaseCurrency = reconciliation.TotalDifferenceInBaseCurrency,
                RequiresApproval = RequiresApproval(reconciliation),
                CreatedBy = reconciliation.CreatedBy,
                CreatedAt = reconciliation.CreatedAt,
                ApprovedBy = reconciliation.ApprovedBy,
                ApprovedAt = reconciliation.ApprovedAt,
                Lines = ToLineItems(reconciliation, cassettes)
            };
        }

        public static ReconciliationModel.GetDetail.Return ToGetDetailReturn(Reconciliation reconciliation, IEnumerable<Cassette> cassettes)
        {
            return new ReconciliationModel.GetDetail.Return
            {
                Id = reconciliation.Id,
                AtmId = reconciliation.AtmId,
                Status = reconciliation.Status,
                TotalDifferenceInBaseCurrency = reconciliation.TotalDifferenceInBaseCurrency,
                RequiresApproval = RequiresApproval(reconciliation),
                CreatedBy = reconciliation.CreatedBy,
                CreatedAt = reconciliation.CreatedAt,
                ApprovedBy = reconciliation.ApprovedBy,
                ApprovedAt = reconciliation.ApprovedAt,
                Lines = ToLineItems(reconciliation, cassettes)
            };
        }

        public static ReconciliationModel.Add.Return ToAddReturn(Reconciliation reconciliation, IEnumerable<Cassette> cassettes)
        {
            return new ReconciliationModel.Add.Return
            {
                Id = reconciliation.Id,
                AtmId = reconciliation.AtmId,
                Status = reconciliation.Status,
                TotalDifferenceInBaseCurrency = reconciliation.TotalDifferenceInBaseCurrency,
                RequiresApproval = RequiresApproval(reconciliation),
                CreatedBy = reconciliation.CreatedBy,
                CreatedAt = reconciliation.CreatedAt,
                ApprovedBy = reconciliation.ApprovedBy,
                ApprovedAt = reconciliation.ApprovedAt,
                Lines = ToLineItems(reconciliation, cassettes)
            };
        }

        public static ReconciliationModel.Approve.Return ToApproveReturn(Reconciliation reconciliation, IEnumerable<Cassette> cassettes)
        {
            return new ReconciliationModel.Approve.Return
            {
                Id = reconciliation.Id,
                AtmId = reconciliation.AtmId,
                Status = reconciliation.Status,
                TotalDifferenceInBaseCurrency = reconciliation.TotalDifferenceInBaseCurrency,
                RequiresApproval = RequiresApproval(reconciliation),
                CreatedBy = reconciliation.CreatedBy,
                CreatedAt = reconciliation.CreatedAt,
                ApprovedBy = reconciliation.ApprovedBy,
                ApprovedAt = reconciliation.ApprovedAt,
                Lines = ToLineItems(reconciliation, cassettes)
            };
        }

        private static bool RequiresApproval(Reconciliation reconciliation)
        {
            return reconciliation.TotalDifferenceInBaseCurrency > ReconciliationRules.ApprovalThresholdInBaseCurrency;
        }

        private static List<ReconciliationModel.LineItem> ToLineItems(Reconciliation reconciliation, IEnumerable<Cassette> cassettes)
        {
            var positions = cassettes.ToDictionary(cassette => cassette.Id, cassette => cassette.Position);
            return reconciliation.Lines
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
        }
    }
}
