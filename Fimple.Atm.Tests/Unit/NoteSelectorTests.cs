using Fimple.Atm.Application.Services;
using Fimple.Atm.Domain.Enums;

namespace Fimple.Atm.Tests.Unit
{
    public class NoteSelectorTests
    {
        private const int CASSETTE_CAPACITY = 100;

        [Fact]
        public void Case1_TryExactAmountUsesFewestNotes()
        {
            var stocks = Stocks((Denomination.Hundred, 0), (Denomination.Fifty, 3), (Denomination.Twenty, 10));

            var result = NoteSelector.Select(stocks, 160);

            AssertBreakdown(result.Exact, 160, (Denomination.Fifty, 2), (Denomination.Twenty, 3));
            Assert.Null(result.Lower);
            Assert.Null(result.Upper);
        }

        [Fact]
        public void Case2_NoExactMatchReturnsLowerAndUpperAlternatives()
        {
            var stocks = Stocks((Denomination.Fifty, 1), (Denomination.Twenty, 2));

            var result = NoteSelector.Select(stocks, 80);

            Assert.Null(result.Exact);
            AssertBreakdown(result.Lower, 70, (Denomination.Fifty, 1), (Denomination.Twenty, 1));
            AssertBreakdown(result.Upper, 90, (Denomination.Fifty, 1), (Denomination.Twenty, 2));
        }

        [Fact]
        public void Case3_NoExactMatchWithPlentyOfNotes()
        {
            var stocks = Stocks((Denomination.Hundred, 5), (Denomination.Twenty, 10));

            var result = NoteSelector.Select(stocks, 130);

            Assert.Null(result.Exact);
            AssertBreakdown(result.Lower, 120, (Denomination.Hundred, 1), (Denomination.Twenty, 1));
            AssertBreakdown(result.Upper, 140, (Denomination.Hundred, 1), (Denomination.Twenty, 2));
        }

        [Fact]
        public void Case4_UpperAlternativeIsAbsentWhenStockRunsOut()
        {
            var stocks = Stocks((Denomination.Hundred, 2));

            var result = NoteSelector.Select(stocks, 250);

            Assert.Null(result.Exact);
            AssertBreakdown(result.Lower, 200, (Denomination.Hundred, 2));
            Assert.Null(result.Upper);
        }

        [Fact]
        public void Case5_ExactAmountSkipsDenominationWithoutStock()
        {
            var stocks = Stocks((Denomination.FiveHundred, 1), (Denomination.Hundred, 0), (Denomination.Twenty, 5));

            var result = NoteSelector.Select(stocks, 600);

            AssertBreakdown(result.Exact, 600, (Denomination.FiveHundred, 1), (Denomination.Twenty, 5));
        }

        [Fact]
        public void Case6_PrefersFewestNotesOverGreedyOrAllSmallNotes()
        {
            var stocks = Stocks((Denomination.Hundred, 1), (Denomination.Fifty, 4), (Denomination.Twenty, 5));

            var result = NoteSelector.Select(stocks, 200);

            AssertBreakdown(result.Exact, 200, (Denomination.Hundred, 1), (Denomination.Fifty, 2));
        }

        [Fact]
        public void Case7_ExactAmountThatNeedsEveryDenomination()
        {
            var stocks = Stocks((Denomination.FiveHundred, 1), (Denomination.Hundred, 3), (Denomination.Twenty, 20));

            var result = NoteSelector.Select(stocks, 1000);

            AssertBreakdown(result.Exact, 1000, (Denomination.FiveHundred, 1), (Denomination.Hundred, 3), (Denomination.Twenty, 10));
        }

        [Fact]
        public void Case8_NoExactMatchWhereGreedyGivesTheWrongAnswer()
        {
            var stocks = Stocks((Denomination.Hundred, 3), (Denomination.Fifty, 1), (Denomination.Twenty, 1));

            var result = NoteSelector.Select(stocks, 110);

            Assert.Null(result.Exact);
            AssertBreakdown(result.Lower, 100, (Denomination.Hundred, 1));
            AssertBreakdown(result.Upper, 120, (Denomination.Hundred, 1), (Denomination.Twenty, 1));
        }

        [Fact]
        public void RequestAboveTotalStockOffersEverythingAsLowerAlternative()
        {
            var stocks = Stocks((Denomination.Fifty, 2), (Denomination.Twenty, 1));

            var result = NoteSelector.Select(stocks, 500);

            Assert.Null(result.Exact);
            AssertBreakdown(result.Lower, 120, (Denomination.Fifty, 2), (Denomination.Twenty, 1));
            Assert.Null(result.Upper);
        }

        [Fact]
        public void EmptyStockOffersNothing()
        {
            var stocks = Stocks((Denomination.Hundred, 0), (Denomination.Fifty, 0));

            var result = NoteSelector.Select(stocks, 100);

            Assert.Null(result.Exact);
            Assert.Null(result.Lower);
            Assert.Null(result.Upper);
        }

        [Fact]
        public void AmountBelowSmallestNoteHasOnlyAnUpperAlternative()
        {
            var stocks = Stocks((Denomination.Twenty, 5));

            var result = NoteSelector.Select(stocks, 10);

            Assert.Null(result.Exact);
            Assert.Null(result.Lower);
            AssertBreakdown(result.Upper, 20, (Denomination.Twenty, 1));
        }

        [Fact]
        public void SameDenominationInTwoCassettesDrainsTheFullerCassetteFirst()
        {
            var emptierCassette = new NoteStock(Guid.NewGuid(), 2, Denomination.Hundred, 30, CASSETTE_CAPACITY);
            var fullerCassette = new NoteStock(Guid.NewGuid(), 3, Denomination.Hundred, 80, CASSETTE_CAPACITY);

            var result = NoteSelector.Select(new[] { emptierCassette, fullerCassette }, 500);

            Assert.NotNull(result.Exact);
            var allocation = Assert.Single(result.Exact!.Allocations);
            Assert.Equal(fullerCassette.CassetteId, allocation.CassetteId);
            Assert.Equal(5, allocation.NoteCount);
        }

        [Fact]
        public void SameDenominationSpillsOverToTheSecondCassetteWhenTheFirstRunsOut()
        {
            var firstCassette = new NoteStock(Guid.NewGuid(), 2, Denomination.Hundred, 3, CASSETTE_CAPACITY);
            var secondCassette = new NoteStock(Guid.NewGuid(), 3, Denomination.Hundred, 2, CASSETTE_CAPACITY);

            var result = NoteSelector.Select(new[] { firstCassette, secondCassette }, 500);

            Assert.NotNull(result.Exact);
            Assert.Equal(5, result.Exact!.NoteCount);
            Assert.Equal(2, result.Exact.Allocations.Count);
        }

        [Fact]
        public void NonPositiveAmountIsRejected()
        {
            var stocks = Stocks((Denomination.Hundred, 1));

            Assert.Throws<ArgumentOutOfRangeException>(() => NoteSelector.Select(stocks, 0));
        }

        private static List<NoteStock> Stocks(params (Denomination Denomination, int AvailableCount)[] stocks)
        {
            return stocks
                .Select((stock, index) => new NoteStock(Guid.NewGuid(), index + 2, stock.Denomination, stock.AvailableCount, CASSETTE_CAPACITY))
                .ToList();
        }

        private static void AssertBreakdown(NoteBreakdown? breakdown, int expectedAmount, params (Denomination Denomination, int NoteCount)[] expectedNotes)
        {
            Assert.NotNull(breakdown);
            Assert.Equal(expectedAmount, breakdown!.Amount);

            var actualNotes = breakdown.Allocations
                .GroupBy(allocation => allocation.Denomination)
                .ToDictionary(group => group.Key, group => group.Sum(allocation => allocation.NoteCount));
            var expectedNoteMap = expectedNotes.ToDictionary(note => note.Denomination, note => note.NoteCount);
            Assert.Equal(expectedNoteMap.OrderBy(pair => pair.Key), actualNotes.OrderBy(pair => pair.Key));
        }
    }
}
