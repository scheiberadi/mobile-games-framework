using System;
using System.Collections.Generic;
using System.Linq;

namespace EvasLearningWorld.Rules
{
    // One round of Covering: a texture (fur, feathers, skin or scales) and a grid of animals, exactly Wanted of which have it.
    public sealed class CoveringRound
    {
        public string Texture;
        public int Wanted;
        public string[] AnimalIds;
        public bool[] IsCorrect;
    }

    // Zoo & Farm Covering as "which animals have feathers?": a big picture of a texture on one side, a grid of animals on the other, and
    // Eva asks the child to find N of them. The child taps animals (tap again to take one back); at N the guess is checked (CoveringGuess).
    // Animals with wool (sheep, llama) are left out - wool or fur is not something a 4 year old can tell apart - while the turtle's shell is
    // only ever an answer to nothing, a filler. Scales have only three animals (fish, shark, snake), so a scales round asks for at most 3 (2 until the shark is in the pool).
    // The grid never has more than 8 animals: a tap target must be 240 units, and 4 x 2 of them is what fits beside the texture.
    public static class CoveringRules
    {
        public const int RoundsPerSession = 5;
        public const float IdleSeconds = 30f;
        public const int MaxGrid = 8;

        // Introduced one by one: levels 1-2 ask about fur and feathers, 3-4 add skin, 5-6 add scales.
        public static readonly string[] Textures = { "fur", "feathers", "skin", "scales" };
        public static readonly int[] TextureCountByLevel = { 2, 2, 3, 3, 4, 4 };
        public static readonly int[] GridSizeByLevel = { 6, 6, 8, 8, 8, 8 };
        public static readonly int[] WantedByLevel = { 2, 2, 2, 3, 3, 4 };

        public static bool InGame(Animal animal) => animal.Covering != "wool";

        public static string QuestionKey(CoveringRound round) => "covering_q_" + round.Texture + "_" + round.Wanted;
        public static string NotKey(string texture) => "covering_not_" + texture;

        public static CoveringRound Create(int level, Random rng, string previousTexture)
        {
            var index = Math.Max(0, Math.Min(GridSizeByLevel.Length - 1, level - DifficultyLadder.MinLevel));
            var everyone = ZooFarmAnimals.All.Where(InGame).ToList();
            var pool = everyone.Take(Math.Min(ZooFarmRoundGenerator.PoolSizeByLevel[index], ZooFarmAnimals.All.Length)).Where(InGame).ToList();

            var textures = Textures.Take(TextureCountByLevel[index]).Where(t => t != previousTexture).ToList();
            var texture = textures[rng.Next(textures.Count)];

            // At the first levels the pool is small; when it has too few of a texture (or of the others) the rest of the animals fill in.
            var correct = pool.Where(a => a.Covering == texture).ToList();
            if (correct.Count < 2) correct = everyone.Where(a => a.Covering == texture).ToList();
            var wanted = Math.Min(WantedByLevel[index], correct.Count);
            var grid = GridSizeByLevel[index];

            var picked = Shuffled(correct, rng).Take(wanted).ToList();
            var others = Shuffled(pool.Where(a => a.Covering != texture).ToList(), rng);
            if (others.Count < grid - wanted) others = Shuffled(everyone.Where(a => a.Covering != texture).ToList(), rng);
            var fillers = others.Take(grid - wanted).ToList();

            var all = picked.Select(a => (animal: a, correct: true)).Concat(fillers.Select(a => (animal: a, correct: false))).OrderBy(_ => rng.Next()).ToList();
            return new CoveringRound
            {
                Texture = texture,
                Wanted = wanted,
                AnimalIds = all.Select(x => x.animal.Id).ToArray(),
                IsCorrect = all.Select(x => x.correct).ToArray(),
            };
        }

        private static List<T> Shuffled<T>(IList<T> list, Random rng) => list.OrderBy(_ => rng.Next()).ToList();
    }

    public enum Verdict { Pending, Right, Wrong }

    // What the game tells the child after a wrong guess or half a minute of nothing. 1: gently ask again. 2: say what is wrong (WrongIndex, an
    // animal that was picked but does not have the texture; taken back) or, with nothing wrong picked, point at one that is still missing
    // (HintIndex). 3: the game picks the right animals itself.
    public sealed class GuessNotice
    {
        public int Level;
        public int WrongIndex = -1;
        public int HintIndex = -1;
    }

    // The state of one Covering round: which animals are picked and how many times the game had to step in.
    public sealed class CoveringGuess
    {
        private readonly Random _rng;

        public CoveringRound Round { get; }
        public bool[] Selected { get; }
        public int Notifications { get; private set; }
        public bool SolvedByGame { get; private set; }

        public CoveringGuess(CoveringRound round, Random rng)
        {
            Round = round;
            _rng = rng;
            Selected = new bool[round.AnimalIds.Length];
        }

        public int SelectedCount => Selected.Count(s => s);
        public bool IsSolved => SelectedCount == Round.Wanted && Selected.Select((s, i) => !s || Round.IsCorrect[i]).All(ok => ok);

        // Picks the animal, or takes it back. Once exactly Wanted animals are picked the guess is checked.
        public Verdict Toggle(int index)
        {
            Selected[index] = !Selected[index];
            if (SelectedCount != Round.Wanted) return Verdict.Pending;
            return IsSolved ? Verdict.Right : Verdict.Wrong;
        }

        // A wrong guess or a quiet half minute: the game steps in, a little more each time.
        public GuessNotice Escalate()
        {
            Notifications++;
            var notice = new GuessNotice { Level = Math.Min(3, Notifications) };
            if (notice.Level == 2)
            {
                var wrong = Enumerable.Range(0, Selected.Length).Where(i => Selected[i] && !Round.IsCorrect[i]).ToList();
                if (wrong.Count > 0) { notice.WrongIndex = wrong[_rng.Next(wrong.Count)]; Selected[notice.WrongIndex] = false; }
                else
                {
                    var missing = Enumerable.Range(0, Selected.Length).Where(i => !Selected[i] && Round.IsCorrect[i]).ToList();
                    if (missing.Count > 0) notice.HintIndex = missing[_rng.Next(missing.Count)];
                }
            }
            else if (notice.Level == 3) Solve();
            return notice;
        }

        // The right animals, and only those, are picked.
        public void Solve()
        {
            SolvedByGame = true;
            for (var i = 0; i < Selected.Length; i++) Selected[i] = Round.IsCorrect[i];
        }
    }
}
