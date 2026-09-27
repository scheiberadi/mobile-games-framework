using System.Collections.Generic;

namespace MobileGamesFramework.Undo
{
    public class UndoStack<TState>
    {
        private readonly Stack<TState> _states = new Stack<TState>();

        public bool CanUndo => _states.Count > 0;

        public void Push(TState state) => _states.Push(state);

        public bool TryPop(out TState state)
        {
            if (_states.Count == 0)
            {
                state = default;
                return false;
            }

            state = _states.Pop();
            return true;
        }

        public void Clear() => _states.Clear();
    }
}
