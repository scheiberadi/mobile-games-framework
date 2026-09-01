using System.Collections.Generic;
using NUnit.Framework;
using MobileGamesFramework.Persistence;
using Game02_Sudoku;

namespace Game02_Sudoku.Tests
{
    public class SudokuStatsStoreTests
    {
        private class FakeKeyValueStore : IKeyValueStore
        {
            private readonly Dictionary<string, string> _values = new Dictionary<string, string>();

            public string GetString(string key, string defaultValue) =>
                _values.TryGetValue(key, out var value) ? value : defaultValue;

            public void SetString(string key, string value) => _values[key] = value;
        }

        [Test]
        public void GetBestTimeSeconds_NoneYet_ReturnsNull()
        {
            var store = new SudokuStatsStore(new FakeKeyValueStore());

            Assert.IsNull(store.GetBestTimeSeconds(Difficulty.Easy));
        }

        [Test]
        public void GetTotalCompleted_NoneYet_ReturnsZero()
        {
            var store = new SudokuStatsStore(new FakeKeyValueStore());

            Assert.AreEqual(0, store.GetTotalCompleted());
        }

        [Test]
        public void ReportCompletion_FirstTime_BecomesBestAndIncrementsTotal()
        {
            var store = new SudokuStatsStore(new FakeKeyValueStore());

            store.ReportCompletion(Difficulty.Easy, 120f);

            Assert.AreEqual(120f, store.GetBestTimeSeconds(Difficulty.Easy));
            Assert.AreEqual(1, store.GetTotalCompleted());
        }

        [Test]
        public void ReportCompletion_FasterThanBest_UpdatesBest()
        {
            var store = new SudokuStatsStore(new FakeKeyValueStore());
            store.ReportCompletion(Difficulty.Easy, 120f);

            store.ReportCompletion(Difficulty.Easy, 90f);

            Assert.AreEqual(90f, store.GetBestTimeSeconds(Difficulty.Easy));
        }

        [Test]
        public void ReportCompletion_SlowerThanBest_DoesNotOverwriteBest()
        {
            var store = new SudokuStatsStore(new FakeKeyValueStore());
            store.ReportCompletion(Difficulty.Easy, 90f);

            store.ReportCompletion(Difficulty.Easy, 120f);

            Assert.AreEqual(90f, store.GetBestTimeSeconds(Difficulty.Easy));
        }

        [Test]
        public void ReportCompletion_TotalIncrementsRegardlessOfTime()
        {
            var store = new SudokuStatsStore(new FakeKeyValueStore());

            store.ReportCompletion(Difficulty.Easy, 120f);
            store.ReportCompletion(Difficulty.Hard, 300f);

            Assert.AreEqual(2, store.GetTotalCompleted());
        }

        [Test]
        public void ReportCompletion_DifferentDifficulties_BestTimesAreIndependent()
        {
            var store = new SudokuStatsStore(new FakeKeyValueStore());

            store.ReportCompletion(Difficulty.Easy, 120f);
            store.ReportCompletion(Difficulty.Hard, 300f);

            Assert.AreEqual(120f, store.GetBestTimeSeconds(Difficulty.Easy));
            Assert.AreEqual(300f, store.GetBestTimeSeconds(Difficulty.Hard));
        }
    }
}
