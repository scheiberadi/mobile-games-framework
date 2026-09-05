using System.Collections.Generic;
using NUnit.Framework;
using MobileGamesFramework.Persistence;
using Game02_Sudoku;

namespace Game02_Sudoku.Tests
{
    public class SudokuLeaderboardStoreTests
    {
        private class FakeKeyValueStore : IKeyValueStore
        {
            private readonly Dictionary<string, string> _values = new Dictionary<string, string>();

            public string GetString(string key, string defaultValue) =>
                _values.TryGetValue(key, out var value) ? value : defaultValue;

            public void SetString(string key, string value) => _values[key] = value;
        }

        [Test]
        public void GetTimes_NoneYet_ReturnsEmpty()
        {
            var store = new SudokuLeaderboardStore(new FakeKeyValueStore());

            Assert.IsEmpty(store.GetTimes(Difficulty.Easy));
        }

        [Test]
        public void GetCompletedCount_NoneYet_ReturnsZero()
        {
            var store = new SudokuLeaderboardStore(new FakeKeyValueStore());

            Assert.AreEqual(0, store.GetCompletedCount(Difficulty.Easy));
        }

        [Test]
        public void ReportCompletion_AddsTimeToList()
        {
            var store = new SudokuLeaderboardStore(new FakeKeyValueStore());

            store.ReportCompletion(Difficulty.Easy, 120f);

            CollectionAssert.AreEqual(new[] { 120f }, store.GetTimes(Difficulty.Easy));
        }

        [Test]
        public void ReportCompletion_IncrementsCompletedCount()
        {
            var store = new SudokuLeaderboardStore(new FakeKeyValueStore());

            store.ReportCompletion(Difficulty.Easy, 120f);
            store.ReportCompletion(Difficulty.Easy, 90f);

            Assert.AreEqual(2, store.GetCompletedCount(Difficulty.Easy));
        }

        [Test]
        public void GetTimes_ReturnsSortedFastestFirst()
        {
            var store = new SudokuLeaderboardStore(new FakeKeyValueStore());

            store.ReportCompletion(Difficulty.Easy, 200f);
            store.ReportCompletion(Difficulty.Easy, 90f);
            store.ReportCompletion(Difficulty.Easy, 150f);

            CollectionAssert.AreEqual(new[] { 90f, 150f, 200f }, store.GetTimes(Difficulty.Easy));
        }

        [Test]
        public void ReportCompletion_KeepsOnlyTopTwentyFastest()
        {
            var store = new SudokuLeaderboardStore(new FakeKeyValueStore());
            for (var i = 0; i < 25; i++) store.ReportCompletion(Difficulty.Easy, 1000f - i);

            var times = store.GetTimes(Difficulty.Easy);

            Assert.AreEqual(20, times.Count);
            Assert.AreEqual(976f, times[0]);
        }

        [Test]
        public void ReportCompletion_KeepsFullCompletedCountEvenPastTopTwenty()
        {
            var store = new SudokuLeaderboardStore(new FakeKeyValueStore());
            for (var i = 0; i < 25; i++) store.ReportCompletion(Difficulty.Easy, 1000f - i);

            Assert.AreEqual(25, store.GetCompletedCount(Difficulty.Easy));
        }

        [Test]
        public void ReportCompletion_DifferentDifficulties_AreIndependent()
        {
            var store = new SudokuLeaderboardStore(new FakeKeyValueStore());

            store.ReportCompletion(Difficulty.Easy, 120f);
            store.ReportCompletion(Difficulty.Hard, 300f);

            CollectionAssert.AreEqual(new[] { 120f }, store.GetTimes(Difficulty.Easy));
            CollectionAssert.AreEqual(new[] { 300f }, store.GetTimes(Difficulty.Hard));
            Assert.AreEqual(1, store.GetCompletedCount(Difficulty.Easy));
            Assert.AreEqual(1, store.GetCompletedCount(Difficulty.Hard));
        }

        [Test]
        public void ClearTimes_RemovesTimesAndResetsCompletedCount()
        {
            var store = new SudokuLeaderboardStore(new FakeKeyValueStore());
            store.ReportCompletion(Difficulty.Easy, 120f);
            store.ReportCompletion(Difficulty.Easy, 90f);

            store.ClearTimes(Difficulty.Easy);

            Assert.IsEmpty(store.GetTimes(Difficulty.Easy));
            Assert.AreEqual(0, store.GetCompletedCount(Difficulty.Easy));
        }

        [Test]
        public void ClearTimes_DoesNotAffectOtherDifficulties()
        {
            var store = new SudokuLeaderboardStore(new FakeKeyValueStore());
            store.ReportCompletion(Difficulty.Easy, 120f);
            store.ReportCompletion(Difficulty.Hard, 300f);

            store.ClearTimes(Difficulty.Easy);

            Assert.IsEmpty(store.GetTimes(Difficulty.Easy));
            CollectionAssert.AreEqual(new[] { 300f }, store.GetTimes(Difficulty.Hard));
        }

        [Test]
        public void GetEntries_NoneYet_ReturnsEmpty()
        {
            var store = new SudokuLeaderboardStore(new FakeKeyValueStore());

            Assert.IsEmpty(store.GetEntries(Difficulty.Easy));
        }

        [Test]
        public void ReportCompletion_RecordsCompletionDate()
        {
            var store = new SudokuLeaderboardStore(new FakeKeyValueStore());
            var completedAt = new System.DateTime(2026, 9, 4, 20, 30, 0);

            store.ReportCompletion(Difficulty.Easy, 120f, completedAt);

            var entries = store.GetEntries(Difficulty.Easy);
            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual(120f, entries[0].Seconds);
            Assert.AreEqual(completedAt, entries[0].CompletedAt);
        }

        [Test]
        public void GetEntries_ReturnsSortedFastestFirstWithMatchingDates()
        {
            var store = new SudokuLeaderboardStore(new FakeKeyValueStore());
            var first = new System.DateTime(2026, 9, 1, 10, 0, 0);
            var second = new System.DateTime(2026, 9, 2, 10, 0, 0);

            store.ReportCompletion(Difficulty.Easy, 200f, first);
            store.ReportCompletion(Difficulty.Easy, 90f, second);

            var entries = store.GetEntries(Difficulty.Easy);
            Assert.AreEqual(90f, entries[0].Seconds);
            Assert.AreEqual(second, entries[0].CompletedAt);
            Assert.AreEqual(200f, entries[1].Seconds);
            Assert.AreEqual(first, entries[1].CompletedAt);
        }

        [Test]
        public void GetEntries_TimeSavedBeforeDatesExisted_FallsBackToMinValueInsteadOfDroppingIt()
        {
            var kv = new FakeKeyValueStore();
            kv.SetString("sudoku.leaderboard.Easy", "120");
            var store = new SudokuLeaderboardStore(kv);

            var entries = store.GetEntries(Difficulty.Easy);

            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual(120f, entries[0].Seconds);
            Assert.AreEqual(System.DateTime.MinValue, entries[0].CompletedAt);
        }

        [Test]
        public void ReportCompletion_NoDateSupplied_DefaultsToNow()
        {
            var store = new SudokuLeaderboardStore(new FakeKeyValueStore());
            var before = System.DateTime.Now;

            store.ReportCompletion(Difficulty.Easy, 120f);

            var after = System.DateTime.Now;
            var recorded = store.GetEntries(Difficulty.Easy)[0].CompletedAt;
            Assert.GreaterOrEqual(recorded, before.AddSeconds(-1));
            Assert.LessOrEqual(recorded, after.AddSeconds(1));
        }
    }
}
