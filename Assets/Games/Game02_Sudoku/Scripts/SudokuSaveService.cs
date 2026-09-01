using UnityEngine;
using MobileGamesFramework.Persistence;

namespace Game02_Sudoku
{
    public class SudokuSaveService
    {
        private const string SaveKey = "sudoku.save";

        private readonly IKeyValueStore _store;

        public SudokuSaveService(IKeyValueStore store)
        {
            _store = store;
        }

        public bool HasSave() => !string.IsNullOrEmpty(_store.GetString(SaveKey, ""));

        public void Save(SudokuGame game, Difficulty difficulty, float elapsedSeconds)
        {
            var dto = SudokuSaveDto.From(game, difficulty, elapsedSeconds);
            _store.SetString(SaveKey, JsonUtility.ToJson(dto));
        }

        public bool TryLoad(out SudokuGame game, out Difficulty difficulty, out float elapsedSeconds)
        {
            var raw = _store.GetString(SaveKey, "");
            if (string.IsNullOrEmpty(raw))
            {
                game = null;
                difficulty = Difficulty.Medium;
                elapsedSeconds = 0f;
                return false;
            }

            var dto = JsonUtility.FromJson<SudokuSaveDto>(raw);
            game = dto.ToGame(out difficulty, out elapsedSeconds);
            return true;
        }

        public void ClearSave() => _store.SetString(SaveKey, "");
    }
}
