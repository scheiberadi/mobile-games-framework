using System;
using System.Collections.Generic;
using System.Linq;

namespace EvasLearningWorld.Rules
{
    // Science Lab's Cooking Measures: "Fill the cup". Eva asks for a cup that is full, half full or empty of water, flour or milk; a recipe
    // card shows the cup to match. The child pours with a pitcher (the level rises while it pours) or tips the cup out (the level falls),
    // and lets go when it looks right: then the level is judged. Two misses in a row call for a hint.
    //
    // Pure logic: no clock, no positions (the screen decides what "pouring" looks like). Levels run from 0 (empty) to 1 (full).
    public enum CupLevel { Empty, Half, Full }

    public enum CookJudge { Right, TooLittle, TooMuch }

    public static class CookingMeasure
    {
        public const int RoundsPerSession = 3;
        public const int WrongsForHint = 2;
        public const float HalfTolerance = 0.15f, FullFrom = 0.92f, EmptyTo = 0.1f;

        public static readonly string[] Ingredients = { "water", "flour", "milk" };

        public static float LevelValue(CupLevel level) => level == CupLevel.Empty ? 0f : level == CupLevel.Half ? 0.5f : 1f;

        public static string IdOf(CupLevel level) => level == CupLevel.Empty ? "empty" : level == CupLevel.Half ? "half" : "full";

        // The recipe card cup picture: water a/d/g, flour b/e/h, milk c/f/i (full / half / empty).
        public static string CupPicture(string ingredient, CupLevel level)
        {
            var column = Array.IndexOf(Ingredients, ingredient);
            var row = level == CupLevel.Full ? 0 : level == CupLevel.Half ? 1 : 2;
            return "cup_" + (char)('a' + row * 3 + column);
        }

        // One round for each of the three levels, in a random order, each with its own ingredient.
        public static CookRound[] CreateSession(Random rng)
        {
            var levels = Shuffle(new[] { CupLevel.Empty, CupLevel.Half, CupLevel.Full }, rng);
            var ingredients = Shuffle(Ingredients, rng);
            return levels.Select((level, i) => new CookRound(ingredients[i], level)).ToArray();
        }

        private static List<T> Shuffle<T>(IEnumerable<T> source, Random rng)
        {
            var list = source.ToList();
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                var swap = list[i];
                list[i] = list[j];
                list[j] = swap;
            }
            return list;
        }
    }

    // One round: the ingredient, the level asked for and the cup level now. A cup to empty starts full; the others start empty.
    public sealed class CookRound
    {
        public CookRound(string ingredient, CupLevel target)
        {
            Ingredient = ingredient;
            Target = target;
            Level = target == CupLevel.Empty ? 1f : 0f;
        }

        public string Ingredient { get; }
        public CupLevel Target { get; }
        public float Level { get; private set; }
        public int Wrongs { get; private set; }       // misses in a row since the last hint
        public int TotalWrongs { get; private set; }
        public bool Done { get; private set; }
        public bool NeedsHint => !Done && Wrongs >= CookingMeasure.WrongsForHint;
        public bool NeedsMore => Level < CookingMeasure.LevelValue(Target);

        // The level changes (pouring raises it, tipping out lowers it); it stays between empty and full.
        public void Add(float amount)
        {
            if (Done) return;
            Level = Math.Max(0f, Math.Min(1f, Level + amount));
        }

        // How the cup looks against the recipe right now (does not end the round).
        public CookJudge Look()
        {
            switch (Target)
            {
                case CupLevel.Empty: return Level <= CookingMeasure.EmptyTo ? CookJudge.Right : CookJudge.TooMuch;
                case CupLevel.Full: return Level >= CookingMeasure.FullFrom ? CookJudge.Right : CookJudge.TooLittle;
                default:
                    if (Level < 0.5f - CookingMeasure.HalfTolerance) return CookJudge.TooLittle;
                    return Level > 0.5f + CookingMeasure.HalfTolerance ? CookJudge.TooMuch : CookJudge.Right;
            }
        }

        // The child let go: judge the cup. A right cup ends the round.
        public CookJudge Judge()
        {
            var result = Look();
            if (Done) return CookJudge.Right;
            if (result == CookJudge.Right)
            {
                Done = true;
                Wrongs = 0;
            }
            else
            {
                Wrongs++;
                TotalWrongs++;
            }
            return result;
        }

        // The hint was shown: the count starts again.
        public void HintGiven() => Wrongs = 0;
    }
}
