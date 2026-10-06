using System;
using System.Collections.Generic;
using System.Linq;

namespace EvasLearningWorld.Rules
{
    // A shared MATCH/SORT round shape (M4.4 Zoo & Farm, docs/kids-games/full-catalogue-plan.md "6. Zoo & Farm"):
    // one target and 2-4 choice pictures, one of them correct. TargetSprite is null for an audio-led round
    // (Animal -> Sound): nothing is shown, only PromptVoiceKey then TargetVoiceKey are spoken. Geography reuses
    // the same shape with TargetSprite always null (a country/landmark is named, never pictured).
    public sealed class MatchRound
    {
        public string TargetId;
        public string TargetSprite;
        public string PromptVoiceKey;
        public string TargetVoiceKey;
        // For the Sound game: the animal whose real recording is the target (TargetVoiceKey is then null).
        public string TargetSoundId;
        public string[] ChoiceSprites;
        public int CorrectIndex;
    }

    // Builds a MatchRound from a plain (id, value) item list - the one piece every MATCH/SORT-shaped game
    // in this cluster actually varies: what the target is, what its correct value is, and how many other
    // animals/countries share that value (which is exactly what makes a SORT bucket game and an aspect-MATCH
    // game the same shape - "domestic"/"wild" is just a value two buckets' worth of animals happen to share,
    // no different from "farm"/"savanna" for Habitat). Level only gates how many items are in play
    // (poolSizeByLevel) and how many choice tiles are shown (choiceCountByLevel); everything else - layout,
    // help ladder, coins - lives in the shared MatchScreen presenter that consumes this.
    public static class MatchRoundBuilder
    {
        public static MatchRound Build(IReadOnlyList<(string id, string value)> items, int level, Random rng, string previousTargetId,
            int[] poolSizeByLevel, int[] choiceCountByLevel, string choiceSpritePrefix, string targetSpritePrefix,
            string promptVoiceKey, string targetVoiceKeyPrefix)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var index = level - DifficultyLadder.MinLevel;
            var poolSize = Math.Min(poolSizeByLevel[index], items.Count);
            var pool = items.Take(poolSize).ToList();

            var target = pool[rng.Next(pool.Count)];
            if (previousTargetId != null && pool.Count > 1)
                while (target.id == previousTargetId) target = pool[rng.Next(pool.Count)];

            var distractors = pool.Select(p => p.value).Distinct().Where(v => v != target.value).ToList();
            Shuffle(distractors, rng);
            var choiceCount = Math.Min(choiceCountByLevel[index], 1 + distractors.Count);

            var choices = new List<string> { target.value };
            for (var i = 0; i < distractors.Count && choices.Count < choiceCount; i++) choices.Add(distractors[i]);
            Shuffle(choices, rng);

            return new MatchRound
            {
                TargetId = target.id,
                TargetSprite = targetSpritePrefix == null ? null : targetSpritePrefix + target.id,
                PromptVoiceKey = promptVoiceKey,
                TargetVoiceKey = targetVoiceKeyPrefix == null ? null : targetVoiceKeyPrefix + target.id,
                TargetSoundId = targetVoiceKeyPrefix == null ? target.id : null,
                ChoiceSprites = choices.Select(v => choiceSpritePrefix + v).ToArray(),
                CorrectIndex = choices.IndexOf(target.value),
            };
        }

        private static void Shuffle<T>(List<T> list, Random rng)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = rng.Next(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
