using Fimple.Atm.Domain.Constants;
using Fimple.Atm.Domain.Entities;
using Fimple.Atm.Domain.Models;

namespace Fimple.Atm.Application.Services
{
    internal static class ReconciliationModelMapper
    {
        public static ReconciliationModel.ReconciliationItem ToReconciliationItem(Reconciliation reconciliation, IEnumerable<Cassette> cassettes)
        {
            var requiresApproval = RequiresApproval(reconciliation);
            var lines = ToLineItems(reconciliation, cassettes);

            return new ReconciliationModel.ReconciliationItem
            {
                Id = reconciliation.Id,
                AtmId = reconciliation.AtmId,
                Status = reconciliation.Status,
                TotalDifferenceInBaseCurrency = reconciliation.TotalDifferenceInBaseCurrency,
                RequiresApproval = requiresApproval,
                CreatedBy = reconciliation.CreatedBy,
                CreatedAt = reconciliation.CreatedAt,
                ApprovedBy = reconciliation.ApprovedBy,
                ApprovedAt = reconciliation.ApprovedAt,
                Lines = lines
            };
        }

        public static ReconciliationModel.GetDetail.Return ToGetDetailReturn(Reconciliation reconciliation, IEnumerable<Cassette> cassettes)
        {
            var requiresApproval = RequiresApproval(reconciliation);
            var lines = ToLineItems(reconciliation, cassettes);

            return new ReconciliationModel.GetDetail.Return
            {
                Id = reconciliation.Id,
                AtmId = reconciliation.AtmId,
                Status = reconciliation.Status,
                TotalDifferenceInBaseCurrency = reconciliation.TotalDifferenceInBaseCurrency,
                RequiresApproval = requiresApproval,
                CreatedBy = reconciliation.CreatedBy,
                CreatedAt = reconciliation.CreatedAt,
                ApprovedBy = reconciliation.ApprovedBy,
                ApprovedAt = reconciliation.ApprovedAt,
                Lines = lines
            };
        }

        public static ReconciliationModel.Add.Return ToAddReturn(Reconciliation reconciliation, IEnumerable<Cassette> cassettes)
        {
            var requiresApproval = RequiresApproval(reconciliation);
            var lines = ToLineItems(reconciliation, cassettes);

            return new ReconciliationModel.Add.Return
            {
                Id = reconciliation.Id,
                AtmId = reconciliation.AtmId,
                Status = reconciliation.Status,
                TotalDifferenceInBaseCurrency = reconciliation.TotalDifferenceInBaseCurrency,
                RequiresApproval = requiresApproval,
                CreatedBy = reconciliation.CreatedBy,
                CreatedAt = reconciliation.CreatedAt,
                ApprovedBy = reconciliation.ApprovedBy,
                ApprovedAt = reconciliation.ApprovedAt,
                Lines = lines
            };
        }

        public static ReconciliationModel.Approve.Return ToApproveReturn(Reconciliation reconciliation, IEnumerable<Cassette> cassettes)
        {
            var requiresApproval = RequiresApproval(reconciliation);
            var lines = ToLineItems(reconciliation, cassettes);

            return new ReconciliationModel.Approve.Return
            {
                Id = reconciliation.Id,
                AtmId = reconciliation.AtmId,
                Status = reconciliation.Status,
                TotalDifferenceInBaseCurrency = reconciliation.TotalDifferenceInBaseCurrency,
                RequiresApproval = requiresApproval,
                CreatedBy = reconciliation.CreatedBy,
                CreatedAt = reconciliation.CreatedAt,
                ApprovedBy = reconciliation.ApprovedBy,
                ApprovedAt = reconciliation.ApprovedAt,
                Lines = lines
            };
        }

        private static bool RequiresApproval(Reconciliation reconciliation)
        {
            return reconciliation.TotalDifferenceInBaseCurrency > ReconciliationRules.APPROVAL_THRESHOLD_IN_BASE_CURRENCY;
        }

        private static List<ReconciliationModel.LineItem> ToLineItems(Reconciliation reconciliation, IEnumerable<Cassette> cassettes)
        {
            var positions = CassettePositions.Build(cassettes);
            return reconciliation.Lines
                .Select(line => ToLineItem(line, positions))
                .OrderBy(line => line.CassettePosition)
                .ThenBy(line => line.Currency)
                .ThenBy(line => line.Denomination)
                .ToList();
        }

        private static ReconciliationModel.LineItem ToLineItem(ReconciliationLine line, Dictionary<Guid, int> positions)
        {
            var cassettePosition = CassettePositions.Get(positions, line.CassetteId);
            var denomination = (int)line.Denomination;

            return new ReconciliationModel.LineItem
            {
                CassetteId = line.CassetteId,
                CassettePosition = cassettePosition,
                Currency = line.Currency,
                Denomination = denomination,
                SystemCount = line.SystemCount,
                CountedCount = line.CountedCount,
                Difference = line.Difference
            };
        }
    }
}
