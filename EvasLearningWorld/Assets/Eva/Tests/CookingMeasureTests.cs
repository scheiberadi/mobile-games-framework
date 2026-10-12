using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;

namespace EvasLearningWorld.Tests
{
    public class CookingMeasureTests
    {
        [Test]
        public void EveryRecipeStartsWithACupStepAndHasEggsAndSugar()
        {
            foreach (var recipe in CookingMeasure.Recipes)
            {
                Assert.That(recipe.Steps[0].Kind, Is.EqualTo(CookStepKind.Cups), recipe.Dish);
                Assert.That(recipe.Steps.Any(s => s.What == "eggs"), recipe.Dish);
                Assert.That(recipe.Steps.Any(s => s.What == "sugar"), recipe.Dish);
                Assert.That(recipe.Steps.Count(s => s.Kind == CookStepKind.Cups), Is.EqualTo(2), recipe.Dish);
            }
        }

        [Test]
        public void EveryRecipeHasItsArtAndVoiceLines()
        {
            foreach (var name in new[] { "cookbowl", "cookitem_egg", "cookitem_sugar" })
                Assert.IsNotNull(Resources.Load<Sprite>("Art/sciencelab/" + name), name);
            foreach (var ingredient in CookingMeasure.Ingredients)
            {
                Assert.IsNotNull(Resources.Load<Sprite>("Art/sciencelab/cookpour_" + ingredient), ingredient + " pitcher");
                Assert.IsNotNull(Resources.Load<Sprite>("Art/sciencelab/cookstream_" + ingredient), ingredient + " stream");
            }

            var keys = new System.Collections.Generic.List<string> { "ck_book", "ck_busy", "ck_book", "ck_busy", "ck_half", "ck_full", "ck_tipall", "ck_cups_1", "ck_cups_2", "ck_eggs_1", "ck_eggs_2", "ck_sugar_1", "ck_more", "ck_less" };
            foreach (var recipe in CookingMeasure.Recipes)
            {
                Assert.IsNotNull(Resources.Load<Sprite>("Art/sciencelab/cookdish_" + recipe.Dish), recipe.Dish + " dish");
                keys.Add("ck_make_" + recipe.Dish);
                keys.Add("ck_dish_" + recipe.Dish);
                keys.AddRange(recipe.Steps.Select(step => "ck_step_" + step.Id));
            }

            foreach (var key in keys)
                Assert.IsNotNull(Resources.Load<AudioClip>("Voice/en/" + key), key);
        }

        [Test]
        public void ASessionIsOneRecipeWithARoundPerStep()
        {
            for (var seed = 0; seed < 40; seed++)
            {
                var recipe = CookingMeasure.CreateRecipe(new System.Random(seed));
                var rounds = CookingMeasure.CreateRounds(recipe);
                Assert.That(rounds.Length, Is.EqualTo(recipe.Steps.Length));
                Assert.That(rounds.All(r => r.Level == 0f && !r.Done));
            }
        }

        [Test]
        public void TheLevelStaysBetweenEmptyAndFull()
        {
            var round = new CookRound(CookStep.Cups("water", CupLevel.Half));
            round.Add(5f);
            Assert.That(round.Level, Is.EqualTo(1f));
            round.Add(-5f);
            Assert.That(round.Level, Is.EqualTo(0f));
        }

        [Test]
        public void AHalfCupIsReadyWithinATolerance()
        {
            var round = new CookRound(CookStep.Cups("milk", CupLevel.Half));
            round.Add(0.2f);
            Assert.That(round.Judge(), Is.EqualTo(CookJudge.TooLittle));
            round.Add(0.3f);
            Assert.That(round.Look(), Is.EqualTo(CookJudge.Right));
            Assert.That(round.Judge(), Is.EqualTo(CookJudge.CupReady));
            Assert.That(round.CupReady);
            Assert.That(round.Done, Is.False, "the cup still has to go into the bowl");
        }

        [Test]
        public void TooMuchInTheCupIsAMissUntilSomeIsTippedOut()
        {
            var round = new CookRound(CookStep.Cups("milk", CupLevel.Half));
            round.Add(0.9f);
            Assert.That(round.Judge(), Is.EqualTo(CookJudge.TooMuch));
            round.Add(-0.4f);
            Assert.That(round.Judge(), Is.EqualTo(CookJudge.CupReady));
        }

        [Test]
        public void ACupCountsOnlyWhenItIsReadyAndThenTippedOutCompletely()
        {
            var round = new CookRound(CookStep.Cups("flour", CupLevel.Full, 2));
            round.Add(0.5f);
            Assert.That(round.Judge(), Is.EqualTo(CookJudge.TooLittle), "half a cup is not a full cup");
            round.Add(0.6f);
            Assert.That(round.Judge(), Is.EqualTo(CookJudge.CupReady));
            round.Add(-0.5f);
            Assert.That(round.Judge(), Is.EqualTo(CookJudge.TooLittle), "tipped only half of it in");
            Assert.That(round.CupReady, "the cup is still the one that was ready");
            round.Add(-0.6f);
            Assert.That(round.Judge(), Is.EqualTo(CookJudge.CupInBowl));
            Assert.That(round.InBowl, Is.EqualTo(1));
            Assert.That(round.Done, Is.False);
            round.Add(1f);
            Assert.That(round.Judge(), Is.EqualTo(CookJudge.CupReady));
            round.Add(-1f);
            Assert.That(round.Judge(), Is.EqualTo(CookJudge.Right));
            Assert.That(round.Done);
            Assert.That(round.InBowl, Is.EqualTo(2));
        }

        [Test]
        public void AnEmptyCupIsNoCupInTheBowl()
        {
            var round = new CookRound(CookStep.Cups("milk", CupLevel.Full));
            Assert.That(round.Judge(), Is.EqualTo(CookJudge.TooLittle));
            Assert.That(round.InBowl, Is.EqualTo(0));
        }

        [Test]
        public void EggsAndSugarAreAddedOneByOne()
        {
            var round = new CookRound(CookStep.Add("eggs", 3));
            Assert.That(round.IsAdd);
            Assert.That(round.AddOne(), Is.EqualTo(CookJudge.Added));
            Assert.That(round.AddOne(), Is.EqualTo(CookJudge.Added));
            Assert.That(round.Done, Is.False);
            Assert.That(round.AddOne(), Is.EqualTo(CookJudge.Right));
            Assert.That(round.Done);
            Assert.That(round.InBowl, Is.EqualTo(3));
        }

        [Test]
        public void TwoMissesInARowCallForAHintAndAReadyCupClearsTheCount()
        {
            var round = new CookRound(CookStep.Cups("water", CupLevel.Full));
            round.Judge();
            Assert.That(round.NeedsHint, Is.False);
            round.Judge();
            Assert.That(round.NeedsHint);
            round.HintGiven();
            Assert.That(round.NeedsHint, Is.False);
            Assert.That(round.TotalWrongs, Is.EqualTo(2));
        }

        [Test]
        public void TheHintPoursUntilTheCupIsReadyThenTips()
        {
            var round = new CookRound(CookStep.Cups("milk", CupLevel.Half));
            Assert.That(round.NeedsMore);
            round.Add(0.5f);
            round.Judge();
            Assert.That(round.NeedsMore, Is.False, "ready: the next help is the tip");
        }

        [Test]
        public void TheStepIdNamesItsVoiceLine()
        {
            Assert.That(CookStep.Cups("milk", CupLevel.Half).Id, Is.EqualTo("milk_half_1"));
            Assert.That(CookStep.Cups("flour", CupLevel.Full, 2).Id, Is.EqualTo("flour_full_2"));
            Assert.That(CookStep.Add("eggs", 3).Id, Is.EqualTo("eggs_3"));
        }

        [Test]
        public void TheCupPictureMatchesIngredientAndLevel()
        {
            Assert.That(CookingMeasure.CupPicture("water", CupLevel.Full), Is.EqualTo("cup_a"));
            Assert.That(CookingMeasure.CupPicture("flour", CupLevel.Half), Is.EqualTo("cup_e"));
            Assert.That(CookingMeasure.CupPicture("milk", CupLevel.Empty), Is.EqualTo("cup_i"));
        }
    }
}
