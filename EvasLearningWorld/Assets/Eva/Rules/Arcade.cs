using System;

namespace EvasLearningWorld.Rules
{
    // Arcade (M4's last building, docs/kids-games/full-catalogue-plan.md "5. Arcade"), built last on purpose since
    // the plan's own brief for it is "fun-first, the educational rule layers on top of a reusable arcade
    // mechanic" - every game here is meant to reskin a mechanic already built for an earlier building rather than
    // invent a new one. Six of its seven games (all but Platformer) reduce to the existing MATCH presenter, the
    // same "reduce to tap-the-correct-tile" call every earlier building's harder rows already made (Zoo & Farm's
    // SORT cluster, Workshop's Balance, Brain Gym's whole 17-game MATCH cluster):
    //
    // Design calls (2026-09-27, flagged for Adrian's review same as every earlier building's simplifications):
    // - Balloon Popping is the binary judgement shape (WhichIsBigger's precedent): tap the balloon matching a
    //   shown rule (even/odd), not a moving-target timing game.
    // - Whack-a-Mole, Fishing and Space Shooter are each a plain self-referential MATCH (WhatsDisappeared's
    //   precedent): tap the mole/fish/ship matching the shown card, no timed pop-up or aim-and-shoot physics.
    // - Fruit Catcher and Fishing (the tracker's own "own mechanic" rows) are simplified the same way rather than
    //   building a bespoke moving-basket/aim mechanic - the same call Workshop made for Simple Physics/Balance and
    //   Zoo & Farm made for its SORT games: a real drag/aim mechanic with no Unity session to touch-test it is
    //   higher risk than reusing a presenter already proven across nine buildings.
    // - Treasure Hunt, the tracker's "meta-mechanic chaining several other mechanics' small challenges into one
    //   run", is simplified to a single MATCH round over a mixed icon pool drawn from the building's other five
    //   games (balloon/mole/fish/ship/fruit, plus a gem) - it reads as "find the treasure map symbol" rather than
    //   literally chaining five separate mini-games end to end, same reduction Sequence Ordering-style games make
    //   everywhere else.
    // - Platformer is the one NAVIGATION-adjacent game and reuses SequenceScreen directly (Brain Gym's Sequence
    //   Ordering / Science Lab's Plant Growth precedent): tap the numbered platforms in order, not a real jump/
    //   physics platformer.
    // Content across every dataset below is placeholder, pending a real art/content pass, same caveat as every
    // catalogue built this session. No Unity build or phone test has happened for Arcade either.

    // --- MATCH-shaped games (6 of Arcade's 7) ---------------------------------------------------------------------

    public enum ArcadeMatchGameKind { BalloonPopping, WhackAMole, Fishing, SpaceShooter, FruitCatcher, TreasureHunt }

    public static class ArcadeMatchRoundGenerator
    {
        public const int RoundsPerSession = 5;

        private static readonly int[] DefaultPoolSizeByLevel = { 4, 5, 6, 6, 6, 6 };
        private static readonly int[] DefaultChoiceCountByLevel = { 2, 3, 3, 4, 4, 4 };
        // Balloon Popping is a binary judgement (even/odd), so the choice count never grows past 2 - same
        // reasoning as Brain Gym's WhichIsBigger.
        private static readonly int[] BinaryPoolSizeByLevel = { 2, 3, 4, 5, 6, 6 };
        private static readonly int[] BinaryChoiceCountByLevel = { 2, 2, 2, 2, 2, 2 };

        public static MatchRound Create(ArcadeMatchGameKind kind, int level, Random rng, string previousTargetId)
        {
            var config = Config(kind);
            var poolSizes = kind == ArcadeMatchGameKind.BalloonPopping ? BinaryPoolSizeByLevel : DefaultPoolSizeByLevel;
            var choiceCounts = kind == ArcadeMatchGameKind.BalloonPopping ? BinaryChoiceCountByLevel : DefaultChoiceCountByLevel;
            return MatchRoundBuilder.Build(config.items, level, rng, previousTargetId, poolSizes, choiceCounts,
                config.choicePrefix, config.targetPrefix, config.promptKey, config.targetVoicePrefix);
        }

        private static (System.Collections.Generic.IReadOnlyList<(string id, string value)> items, string choicePrefix,
            string targetPrefix, string promptKey, string targetVoicePrefix) Config(ArcadeMatchGameKind kind)
        {
            switch (kind)
            {
                case ArcadeMatchGameKind.BalloonPopping:
                    return (BalloonPoppingItems, "arcade/balloon_", "arcade/balloonrule_",
                        "arcade_prompt_balloonpopping", "arcade_balloonrule_");
                case ArcadeMatchGameKind.WhackAMole:
                    return (WhackAMoleItems, "arcade/mole_", "arcade/molecard_",
                        "arcade_prompt_whackamole", "arcade_mole_");
                case ArcadeMatchGameKind.Fishing:
                    return (FishingItems, "arcade/fish_", "arcade/fishcard_",
                        "arcade_prompt_fishing", "arcade_fish_");
                case ArcadeMatchGameKind.SpaceShooter:
                    return (SpaceShooterItems, "arcade/ship_", "arcade/shapecard_",
                        "arcade_prompt_spaceshooter", "arcade_ship_");
                case ArcadeMatchGameKind.FruitCatcher:
                    return (FruitCatcherItems, "arcade/fruit_", "arcade/fruitcard_",
                        "arcade_prompt_fruitcatcher", "arcade_fruit_");
                case ArcadeMatchGameKind.TreasureHunt:
                    return (TreasureHuntItems, "arcade/treasure_", "arcade/mapsymbol_",
                        "arcade_prompt_treasurehunt", "arcade_treasure_");
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        // Self-referential (value == id) except Balloon Popping, whose value is the even/odd rule it stands for.
        private static readonly (string, string)[] BalloonPoppingItems =
        {
            ("one", "odd"), ("two", "even"), ("three", "odd"), ("four", "even"), ("five", "odd"), ("six", "even"),
        };

        private static readonly (string, string)[] WhackAMoleItems =
        {
            ("a", "a"), ("b", "b"), ("c", "c"), ("d", "d"), ("e", "e"), ("f", "f"),
        };

        private static readonly (string, string)[] FishingItems =
        {
            ("red", "red"), ("blue", "blue"), ("green", "green"), ("yellow", "yellow"), ("purple", "purple"), ("orange", "orange"),
        };

        private static readonly (string, string)[] SpaceShooterItems =
        {
            ("circle", "circle"), ("square", "square"), ("triangle", "triangle"), ("star", "star"), ("heart", "heart"), ("diamond", "diamond"),
        };

        private static readonly (string, string)[] FruitCatcherItems =
        {
            ("apple", "apple"), ("banana", "banana"), ("grape", "grape"), ("orange", "orange"), ("pear", "pear"), ("plum", "plum"),
        };

        // The mixed icon pool Treasure Hunt draws from - one symbol per other Arcade game plus a gem, so a single
        // MATCH round over this pool reads as "find the treasure map symbol" across the whole building.
        private static readonly (string, string)[] TreasureHuntItems =
        {
            ("balloon", "balloon"), ("mole", "mole"), ("fish", "fish"), ("ship", "ship"), ("fruit", "fruit"), ("gem", "gem"),
        };
    }

    // --- Platformer (reuses SequenceScreen directly, same as Brain Gym's Sequence Ordering) ------------------------

    public static class PlatformerRoundGenerator
    {
        public const int RoundsPerSession = 5;
        public const string TileSpritePrefix = "arcade/platform_";

        private static readonly string[] Platforms = { "p1", "p2", "p3", "p4", "p5", "p6" };
        private static readonly int[] StageCountByLevel = { 2, 3, 4, 4, 5, 6 };

        public static SequenceRound Create(int level, Random rng) => SequenceRoundBuilder.Build(Platforms, level, rng, StageCountByLevel);
    }
}
