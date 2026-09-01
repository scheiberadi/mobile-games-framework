using UnityEngine;
using MobileGamesFramework.Persistence;

namespace Game01_2048
{
    public class Game2048SaveService
    {
        private const string SaveKey = "game2048.save";

        private readonly IKeyValueStore _store;

        public Game2048SaveService(IKeyValueStore store)
        {
            _store = store;
        }

        public bool HasSave() => !string.IsNullOrEmpty(_store.GetString(SaveKey, ""));

        public void Save(Game2048Game game)
        {
            var dto = Game2048SaveDto.From(game);
            _store.SetString(SaveKey, JsonUtility.ToJson(dto));
        }

        public bool TryLoad(ITileSpawnStrategy spawner, out Game2048Game game)
        {
            var raw = _store.GetString(SaveKey, "");
            if (string.IsNullOrEmpty(raw))
            {
                game = null;
                return false;
            }

            var dto = JsonUtility.FromJson<Game2048SaveDto>(raw);
            game = dto.ToGame(spawner);
            return true;
        }

        public void ClearSave() => _store.SetString(SaveKey, "");
    }
}
