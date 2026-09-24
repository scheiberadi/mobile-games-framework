using System;
using System.Collections.Generic;
using EvasLearningWorld.Rules;
using MobileGamesFramework.Persistence;
using UnityEngine;

namespace EvasLearningWorld.App
{
    // Reads and writes PlayerProgress as JSON under one key. A missing or damaged save loads as a fresh start.
    public sealed class SaveStore
    {
        public const string Key = "eva.save.v1";

        private readonly IKeyValueStore _store;

        public SaveStore(IKeyValueStore store) => _store = store;

        public PlayerProgress Load()
        {
            var json = _store.GetString(Key, "");
            if (string.IsNullOrEmpty(json)) return new PlayerProgress();
            try
            {
                var progress = JsonUtility.FromJson<PlayerProgress>(json);
                if (progress == null) return new PlayerProgress();
                if (progress.Look == null) progress.Look = new CharacterLook();
                if (progress.Owned == null) progress.Owned = new List<string>();
                if (progress.House == null) progress.House = new HouseLayout();
                if (progress.House.Placements == null) progress.House.Placements = new List<Placement>();
                var kept = new List<Placement>();
                foreach (var placement in progress.House.Placements)
                {
                    if (placement.SlotId != null && placement.SlotId.StartsWith("bedroom_"))
                        placement.SlotId = "kids_" + placement.SlotId.Substring("bedroom_".Length);
                    if (HouseSlots.Find(placement.SlotId) != null) kept.Add(placement);
                }
                progress.House.Placements = kept;
                progress.DifficultyLevel = Math.Max(DifficultyLadder.MinLevel, Math.Min(DifficultyLadder.MaxLevel, progress.DifficultyLevel));
                return progress;
            }
            catch (Exception)
            {
                return new PlayerProgress();
            }
        }

        public void Save(PlayerProgress progress) => _store.SetString(Key, JsonUtility.ToJson(progress));
    }
}
