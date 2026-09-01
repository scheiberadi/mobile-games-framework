using NUnit.Framework;
using MobileGamesFramework.Undo;

namespace MobileGamesFramework.Tests
{
    public class UndoStackTests
    {
        [Test]
        public void CanUndo_InitiallyFalse()
        {
            var stack = new UndoStack<int>();

            Assert.IsFalse(stack.CanUndo);
        }

        [Test]
        public void Push_ThenCanUndo_True()
        {
            var stack = new UndoStack<int>();

            stack.Push(1);

            Assert.IsTrue(stack.CanUndo);
        }

        [Test]
        public void TryPop_ReturnsPushedState()
        {
            var stack = new UndoStack<int>();
            stack.Push(42);

            var popped = stack.TryPop(out var state);

            Assert.IsTrue(popped);
            Assert.AreEqual(42, state);
        }

        [Test]
        public void TryPop_WhenEmpty_ReturnsFalse()
        {
            var stack = new UndoStack<int>();

            var popped = stack.TryPop(out _);

            Assert.IsFalse(popped);
        }

        [Test]
        public void MultipleStates_PopInLifoOrder()
        {
            var stack = new UndoStack<int>();
            stack.Push(1);
            stack.Push(2);
            stack.Push(3);

            stack.TryPop(out var first);
            stack.TryPop(out var second);

            Assert.AreEqual(3, first);
            Assert.AreEqual(2, second);
        }

        [Test]
        public void Clear_RemovesAllPushedStates()
        {
            var stack = new UndoStack<int>();
            stack.Push(1);
            stack.Push(2);

            stack.Clear();

            Assert.IsFalse(stack.CanUndo);
        }
    }
}
