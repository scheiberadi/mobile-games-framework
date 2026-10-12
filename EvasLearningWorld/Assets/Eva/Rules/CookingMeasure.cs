using System;
using System.Collections.Generic;
using System.Linq;

namespace EvasLearningWorld.Rules
{
    // Science Lab's Cooking Measures: "Let's make pancakes!". Eva tells the child what they are making and walks through the recipe, one step
    // at a time: a cup step (fill the cup half full or full with milk, flour or water, then tip it all into the bowl; for two cups, twice) or an
    // add step (put some eggs or a spoon of sugar in the bowl). A recipe card shows the step. After the last step the dish appears.
    //
    // Pure logic: no clock, no positions (the screen decides what "pouring" looks like). Levels run from 0 (empty) to 1 (full). Two misses in
    // a row call for a hint.
    public enum CupLevel { Empty, Half, Full }

    // StillReady: a cup that was ready is still right; PartlyTipped: a ready cup was tipped part of the way into the bowl (neither is a miss).
    public enum CookJudge { Right, TooLittle, TooMuch, CupReady, CupInBowl, Added, StillReady, PartlyTipped }

    public enum CookStepKind { Cups, Add }

    public sealed class CookStep
    {
        private CookStep(CookStepKind kind, string what, CupLevel level, int count)
        {
            Kind = kind;
            What = what;
            Level = level;
            Count = count;
        }

        public static CookStep Cups(string ingredient, CupLevel level, int count = 1) => new CookStep(CookStepKind.Cups, ingredient, level, count);

        public static CookStep Add(string item, int count) => new CookStep(CookStepKind.Add, item, CupLevel.Empty, count);

        public CookStepKind Kind { get; }
        public string What { get; }      // the ingredient (milk, flour, water) or the item (eggs, sugar)
        public CupLevel Level { get; }   // a cup step: half or full
        public int Count { get; }        // cups, eggs or spoons

        // The id of the voice line that asks for this step: milk_half_1, flour_full_2, eggs_2, sugar_1.
        public string Id => Kind == CookStepKind.Cups ? What + "_" + CookingMeasure.IdOf(Level) + "_" + Count : What + "_" + Count;
    }

    public sealed class CookRecipe
    {
        public CookRecipe(string dish, params CookStep[] steps)
        {
            Dish = dish;
            Steps = steps;
        }

        public string Dish { get; }
        public CookStep[] Steps { get; }
    }

    public static class CookingMeasure
    {
        public const int WrongsForHint = 2;
        public const float HalfTolerance = 0.15f, FullFrom = 0.92f, EmptyTo = 0.1f;

        public static readonly string[] Ingredients = { "water", "flour", "milk" };

        public static readonly CookRecipe[] Recipes =
        {
            new CookRecipe("pancakes", CookStep.Cups("milk", CupLevel.Half), CookStep.Cups("flour", CupLevel.Full), CookStep.Add("eggs", 2), CookStep.Add("sugar", 1)),
            new CookRecipe("cake", CookStep.Cups("flour", CupLevel.Full, 2), CookStep.Cups("milk", CupLevel.Half), CookStep.Add("eggs", 3), CookStep.Add("sugar", 2)),
            new CookRecipe("cupcakes", CookStep.Cups("flour", CupLevel.Full), CookStep.Cups("water", CupLevel.Half), CookStep.Add("eggs", 2), CookStep.Add("sugar", 1)),
        };

        public static float LevelValue(CupLevel level) => level == CupLevel.Empty ? 0f : level == CupLevel.Half ? 0.5f : 1f;

        public static string IdOf(CupLevel level) => level == CupLevel.Empty ? "empty" : level == CupLevel.Half ? "half" : "full";

        // The cup picture: water a/d/g, flour b/e/h, milk c/f/i (full / half / empty).
        public static string CupPicture(string ingredient, CupLevel level)
        {
            var column = Array.IndexOf(Ingredients, ingredient);
            var row = level == CupLevel.Full ? 0 : level == CupLevel.Half ? 1 : 2;
            return "cup_" + (char)('a' + row * 3 + column);
        }

        // One recipe, chosen at random, one round per step.
        public static CookRecipe CreateRecipe(Random rng) => Recipes[rng.Next(Recipes.Length)];

        public static CookRound[] CreateRounds(CookRecipe recipe) => recipe.Steps.Select(step => new CookRound(step)).ToArray();
    }

    // One round (one step of the recipe): the cup level now, the units in the bowl so far, and what the child still has to do.
    public sealed class CookRound
    {
        private bool _ready;

        public CookRound(CookStep step)
        {
            Step = step;
        }

        public CookStep Step { get; }
        public bool IsAdd => Step.Kind == CookStepKind.Add;
        public string What => Step.What;
        public CupLevel Target => Step.Level;
        public int Count => Step.Count;
        public int InBowl { get; private set; }       // cups tipped in, or eggs / spoons added
        public float Level { get; private set; }
        public int Wrongs { get; private set; }       // misses in a row since the last hint
        public int TotalWrongs { get; private set; }
        public bool Done { get; private set; }
        public bool CupReady => _ready;               // the cup holds just what the recipe asks and waits to be tipped in
        public bool NeedsHint => !Done && Wrongs >= CookingMeasure.WrongsForHint;

        // The hint pours (a cup that is not ready and not at the level yet) or tips in (a ready cup) or adds (an add step).
        public bool NeedsMore => !IsAdd && !_ready && Level < CookingMeasure.LevelValue(Target);

        // The level changes (pouring raises it, tipping out lowers it); it stays between empty and full.
        public void Add(float amount)
        {
            if (Done || IsAdd) return;
            Level = Math.Max(0f, Math.Min(1f, Level + amount));
        }

        // How the cup looks against the recipe right now (does not end anything).
        public CookJudge Look()
        {
            if (Target == CupLevel.Full) return Level >= CookingMeasure.FullFrom ? CookJudge.Right : CookJudge.TooLittle;
            if (Level < 0.5f - CookingMeasure.HalfTolerance) return CookJudge.TooLittle;
            return Level > 0.5f + CookingMeasure.HalfTolerance ? CookJudge.TooMuch : CookJudge.Right;
        }

        // The child let go of the pitcher or the cup: a cup at the recipe level is ready to tip; a ready cup tipped out completely is one more
        // cup in the bowl; anything else is a miss.
        public CookJudge Judge()
        {
            if (Done || IsAdd) return CookJudge.Right;
            var look = Look();
            if (look == CookJudge.Right && Level > CookingMeasure.EmptyTo)
            {
                var already = _ready;
                _ready = true;
                Wrongs = 0;
                return already ? CookJudge.StillReady : CookJudge.CupReady;
            }
            if (Level <= CookingMeasure.EmptyTo && _ready)
            {
                _ready = false;
                return Count1();
            }
            if (_ready && look == CookJudge.TooLittle) return CookJudge.PartlyTipped; // let go while tipping it in: not a miss
            if (look == CookJudge.TooMuch) _ready = false; // poured more into a ready cup: it has to go back to the recipe first
            Wrongs++;
            TotalWrongs++;
            return look;
        }

        // An egg or a spoon of sugar went into the bowl.
        public CookJudge AddOne()
        {
            if (Done || !IsAdd) return CookJudge.Right;
            Wrongs = 0;
            return Count1();
        }

        private CookJudge Count1()
        {
            InBowl++;
            Wrongs = 0;
            if (InBowl < Count) return IsAdd ? CookJudge.Added : CookJudge.CupInBowl;
            Done = true;
            return CookJudge.Right;
        }

        // The hint was shown: the count starts again.
        public void HintGiven() => Wrongs = 0;
    }
}
