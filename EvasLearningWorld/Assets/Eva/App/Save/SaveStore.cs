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
                // M5's character model (Gender/Face/wardrobe-item-ids) replaced the old 3-field Head/Skin/Shirt
                // tint model (docs/superpowers/specs/2026-09-27-character-system-design.md's "Save
                // compatibility"). There is no faithful mapping from a shirt TINT to a specific illustrated
                // wardrobe ITEM, so - a deliberate Task 1 decision, not a missed case - an old save's Look is
                // reset rather than faked into a migrated-but-wrong-looking character; HasCharacter goes back
                // to false so the child re-runs Creator with the new picker instead of silently keeping an
                // empty/default-looking character. Everything else in the save (coins, house, every
                // difficulty ladder) is untouched. Pre-launch development saves only, per that same section.
                if (progress.Version < 2)
                {
                    progress.Look = new CharacterLook();
                    progress.HasCharacter = false;
                    progress.Version = 2;
                }
                progress.Look.Normalize();
                if (progress.Owned == null) progress.Owned = new List<string>();
                if (progress.House == null) progress.House = new HouseLayout();
                if (progress.House.Placements == null) progress.House.Placements = new List<Placement>();
                // The lamp was replaced by the toy chest (no lighting items): same kind, same price.
                for (var i = 0; i < progress.Owned.Count; i++)
                    if (progress.Owned[i] == "lamp") progress.Owned[i] = "chest";
                var kept = new List<Placement>();
                foreach (var placement in progress.House.Placements)
                {
                    if (placement.ItemId == "lamp") placement.ItemId = "chest";
                    if (placement.SlotId != null && placement.SlotId.StartsWith("bedroom_"))
                        placement.SlotId = "kids_" + placement.SlotId.Substring("bedroom_".Length);
                    if (HouseSlots.Find(placement.SlotId) != null) kept.Add(placement);
                }
                progress.House.Placements = kept;
                progress.DifficultyLevel = Math.Max(DifficultyLadder.MinLevel, Math.Min(DifficultyLadder.MaxLevel, progress.DifficultyLevel));
                progress.NumberHuntLevel = Math.Max(DifficultyLadder.MinLevel, Math.Min(DifficultyLadder.MaxLevel, progress.NumberHuntLevel));
                if (progress.NumberHuntBuffer == null) progress.NumberHuntBuffer = new List<bool>();
                progress.LastPlace = Places.ParseOrHouse(progress.LastPlace).ToString();
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
