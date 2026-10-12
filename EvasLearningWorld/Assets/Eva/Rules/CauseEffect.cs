using System;
using System.Collections.Generic;
using System.Linq;

namespace EvasLearningWorld.Rules
{
    // Science Lab's Cause and Effect: "Make it happen". One thing waits on the meadow (a sprout, an ice cream, a balloon, a lamp, ...) and
    // Eva asks the child to change it ("Can you make the plant grow?"). Three tool badges lie in a tray, one of them is the cause. The child
    // drops the right one on the thing and it changes into its after picture; a wrong one goes back and Eva says what it is for. Two wrong
    // tools in a row call for a hint.
    //
    // Pure logic: no clock, no positions (the screen decides what "dropped on the thing" means).
    public sealed class CausePair
    {
        public CausePair(string id, string tool, bool snap = false)
        {
            Id = id;
            Tool = tool;
            Snap = snap;
        }

        public string Id { get; }
        public string Tool { get; }   // the id of the tool that makes it happen
        public bool Snap { get; }     // it changes at once (a pop, a light) instead of fading
    }

    public enum CauseDrop { Right, Wrong, Again }

    public static class CauseEffect
    {
        public const int RoundsPerSession = 3;
        public const int ToolsPerRound = 3;
        public const int WrongsForHint = 2;

        public static readonly CausePair[] Pairs =
        {
            new CausePair("plant", "wateringcan"), new CausePair("icecream", "sun"),
            new CausePair("ground", "raincloud"), new CausePair("paper", "crayon"), new CausePair("balloon", "pin", true),
            new CausePair("bread", "oven"), new CausePair("boot", "soap"), new CausePair("ice", "snowflake"), new CausePair("lamp", "switch", true),
        };

        public static readonly string[] Tools = Pairs.Select(p => p.Tool).ToArray();

        // Three different pairs in a random order.
        public static CauseRound[] CreateSession(Random rng)
        {
            return Shuffle(Pairs, rng).Take(RoundsPerSession).Select(pair => CreateRound(pair, rng)).ToArray();
        }

        // The right tool and two others, in a random order.
        public static CauseRound CreateRound(CausePair pair, Random rng)
        {
            var others = Shuffle(Tools.Where(t => t != pair.Tool), rng).Take(ToolsPerRound - 1);
            return new CauseRound(pair, Shuffle(others.Concat(new[] { pair.Tool }), rng).ToArray());
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

    // One round: the pair and the tools in the tray (by index).
    public sealed class CauseRound
    {
        public CauseRound(CausePair pair, string[] tools)
        {
            Pair = pair;
            Tools = tools;
        }

        public CausePair Pair { get; }
        public string[] Tools { get; }
        public int Wrongs { get; private set; }       // wrong tools in a row since the last hint
        public int TotalWrongs { get; private set; }
        public bool Done { get; private set; }
        public bool NeedsHint => !Done && Wrongs >= CauseEffect.WrongsForHint;

        public int RightTool => Array.IndexOf(Tools, Pair.Tool);

        public CauseDrop Drop(int tool)
        {
            if (Done) return CauseDrop.Again;
            if (Tools[tool] == Pair.Tool)
            {
                Done = true;
                Wrongs = 0;
                return CauseDrop.Right;
            }
            Wrongs++;
            TotalWrongs++;
            return CauseDrop.Wrong;
        }

        // The hint was shown: the count starts again.
        public void HintGiven() => Wrongs = 0;
    }
}
