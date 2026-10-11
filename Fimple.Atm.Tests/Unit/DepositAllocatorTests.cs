using Fimple.Atm.Application.Services;
using Fimple.Atm.Domain.Enums;

namespace Fimple.Atm.Tests.Unit
{
    public class DepositAllocatorTests
    {
        private const int CAPACITY = 100;

        private static readonly Guid DepositCassetteId = Guid.NewGuid();
        private static readonly Guid TryHundredCassetteId = Guid.NewGuid();
        private static readonly Guid TryFiftyCassetteId = Guid.NewGuid();
        private static readonly Guid UsdHundredCassetteId = Guid.NewGuid();

        [Fact]
        public void NoteWithMatchingRecyclingCassetteGoesToThatCassette()
        {
            var spaces = Spaces(depositNotes: 0, tryHundredNotes: 10);

            var allocation = DepositAllocator.Allocate(spaces, new[] { new DepositLine(Currency.Try, Denomination.Hundred, 5) });

            var placement = Assert.Single(allocation.Placements);
            Assert.Equal(TryHundredCassetteId, placement.CassetteId);
            Assert.Equal(5, placement.NoteCount);
            Assert.Empty(allocation.Returns);
        }

        [Fact]
        public void NoteWithoutMatchingRecyclingCassetteGoesToTheDepositCassette()
        {
            var spaces = Spaces(depositNotes: 0, tryHundredNotes: 10);

            var allocation = DepositAllocator.Allocate(spaces, new[] { new DepositLine(Currency.Eur, Denomination.Fifty, 7) });

            var placement = Assert.Single(allocation.Placements);
            Assert.Equal(DepositCassetteId, placement.CassetteId);
            Assert.Equal(7, placement.NoteCount);
        }

        [Fact]
        public void RecyclingOverflowSpillsToTheDepositCassette()
        {
            var spaces = Spaces(depositNotes: 0, tryHundredNotes: 95);

            var allocation = DepositAllocator.Allocate(spaces, new[] { new DepositLine(Currency.Try, Denomination.Hundred, 12) });

            Assert.Equal(2, allocation.Placements.Count);
            Assert.Equal(5, allocation.Placements.Single(placement => placement.CassetteId == TryHundredCassetteId).NoteCount);
            Assert.Equal(7, allocation.Placements.Single(placement => placement.CassetteId == DepositCassetteId).NoteCount);
            Assert.Empty(allocation.Returns);
        }

        [Fact]
        public void NotesThatFitNowhereAreReturned()
        {
            var spaces = Spaces(depositNotes: 96, tryHundredNotes: 98);

            var allocation = DepositAllocator.Allocate(spaces, new[] { new DepositLine(Currency.Try, Denomination.Hundred, 10) });

            Assert.Equal(2, allocation.Placements.Single(placement => placement.CassetteId == TryHundredCassetteId).NoteCount);
            Assert.Equal(4, allocation.Placements.Single(placement => placement.CassetteId == DepositCassetteId).NoteCount);
            var returned = Assert.Single(allocation.Returns);
            Assert.Equal(4, returned.NoteCount);
            Assert.Equal(Denomination.Hundred, returned.Denomination);
        }

        [Fact]
        public void FullDepositCassetteReturnsEverythingWithoutAMatchingCassette()
        {
            var spaces = Spaces(depositNotes: CAPACITY, tryHundredNotes: 0);

            var allocation = DepositAllocator.Allocate(spaces, new[] { new DepositLine(Currency.Usd, Denomination.Twenty, 3) });

            Assert.Empty(allocation.Placements);
            Assert.Equal(3, Assert.Single(allocation.Returns).NoteCount);
        }

        [Fact]
        public void TwoMatchingCassettesFillTheEmptierOneFirst()
        {
            var fullerCassette = new CassetteSpace(TryHundredCassetteId, 2, CassetteKind.Recycling, Currency.Try, Denomination.Hundred, CAPACITY, 60);
            var emptierCassette = new CassetteSpace(TryFiftyCassetteId, 3, CassetteKind.Recycling, Currency.Try, Denomination.Hundred, CAPACITY, 20);
            var depositCassette = new CassetteSpace(DepositCassetteId, 1, CassetteKind.Deposit, null, null, CAPACITY, 0);

            var allocation = DepositAllocator.Allocate(new[] { depositCassette, fullerCassette, emptierCassette }, new[] { new DepositLine(Currency.Try, Denomination.Hundred, 10) });

            var placement = Assert.Single(allocation.Placements);
            Assert.Equal(TryFiftyCassetteId, placement.CassetteId);
        }

        [Fact]
        public void DepositCassetteSpaceIsSharedAcrossLinesInRequestOrder()
        {
            var spaces = Spaces(depositNotes: 90, tryHundredNotes: 0);
            var lines = new[]
            {
                new DepositLine(Currency.Eur, Denomination.Fifty, 6),
                new DepositLine(Currency.Eur, Denomination.Twenty, 6)
            };

            var allocation = DepositAllocator.Allocate(spaces, lines);

            Assert.Equal(6, allocation.Placements.Single(placement => placement.Denomination == Denomination.Fifty).NoteCount);
            Assert.Equal(4, allocation.Placements.Single(placement => placement.Denomination == Denomination.Twenty).NoteCount);
            var returned = Assert.Single(allocation.Returns);
            Assert.Equal(Denomination.Twenty, returned.Denomination);
            Assert.Equal(2, returned.NoteCount);
        }

        private static List<CassetteSpace> Spaces(int depositNotes, int tryHundredNotes)
        {
            return new List<CassetteSpace>
            {
                new(DepositCassetteId, 1, CassetteKind.Deposit, null, null, CAPACITY, depositNotes),
                new(TryHundredCassetteId, 2, CassetteKind.Recycling, Currency.Try, Denomination.Hundred, CAPACITY, tryHundredNotes),
                new(TryFiftyCassetteId, 3, CassetteKind.Recycling, Currency.Try, Denomination.Fifty, CAPACITY, 0),
                new(UsdHundredCassetteId, 4, CassetteKind.Recycling, Currency.Usd, Denomination.Hundred, CAPACITY, 0)
            };
        }
    }
}
