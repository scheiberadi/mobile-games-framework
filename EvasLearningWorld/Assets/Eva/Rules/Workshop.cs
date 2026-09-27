using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    // Workshop (M4.6, docs/kids-games/full-catalogue-plan.md "8. Workshop"): the first building whose plan
    // column calls for BUILD -> TEST -> OBSERVE. Seven of its ten games (the five Build-a-X games, Bridge
    // Building and Simple Physics) reduce to the same "drag parts onto their own fixed slot, then watch a
    // short test animation" shape - one shared AssemblyRound/AssemblyScreen (App/Screens/AssemblyScreen.cs),
    // reusing DragItem's slot-snap mechanic exactly as Dress for the Occasion/Pack a Suitcase do (read
    // DressForOccasionScreen first) but adding the TEST->OBSERVE tail those games don't have. The other three
    // (Tool Selection, Help the Character, Balance) reduce to the existing MATCH presenter instead - see the
    // design calls below, flagged for Adrian's review same as every earlier simplification this milestone.
    //
    // Design calls (2026-09-27, same pattern as Zoo & Farm's SORT-via-MATCH and Science Lab's Sink or
    // Float/Cause and Effect calls, flagged for Adrian's review):
    // - Simple Physics is built on the same assembly presenter as the five Build-a-X games (place 3 ramp/block
    //   pieces onto their fixed slots, then watch the ball roll to the target) rather than the plan's own free
    //   continuous placement + rolling simulation - lower risk, ships now, still teaches "which piece goes
    //   where."
    // - Balance is built as a single-round MATCH pick-the-correct-counterweight (a scale is shown tipped one
    //   way, tap the weight among choices that levels it) rather than a continuous add/remove-weights
    //   simulation - reuses the MatchScreen presenter fully, same shape as Science Lab's Magnet/Sink or Float.
    // - Tool Selection and Help the Character both reduce directly to MatchRoundBuilder's existing
    //   scenario -> pick-the-right-choice shape (no new presenter needed at all).
    // Content across every dataset below is placeholder, pending a real art/content pass, same caveat as
    // every catalogue built this session.
    public enum WorkshopBuildKind { Car, Rocket, House, Boat, Robot, Bridge, SimplePhysics }

    public sealed class WorkshopPart
    {
        // Sprite key: "workshop/part_<PartId>".
        public string PartId;
        public bool Correct; // true for one of this build's own 3 parts; false for a distractor from another build
        public WorldPoint TrayPosition;
    }

    public sealed class AssemblySlot
    {
        public string PartId; // the one part this slot wants
        public WorldPoint Position;
    }

    public sealed class AssemblyRound
    {
        public WorkshopBuildKind Kind;
        public AssemblySlot[] Slots;
        // The shelf: this build's own 3 parts plus 0-2 distractors from other builds (DistractorCountByLevel), shuffled.
        public WorkshopPart[] ShelfItems;
        public string IntroVoiceKey; // spoken once, before the child can touch anything (the BUILD cue)
        public string TestVoiceKey;  // spoken once every slot is filled (the OBSERVE line)
        public string TestSprite;    // "workshop/test_<kind>", shown during the TEST animation
    }

    public static class WorkshopAssemblyRoundGenerator
    {
        public const int RoundsPerSession = 3; // fewer, longer rounds - same reasoning as the Store dressing cluster.
        public const float SlotSize = 240f; // EvaUi.MinTap
        public const float SnapRadius = 110f;
        private static readonly float[] SlotColumnX = { -260f, 0f, 260f };
        private const float SlotY = 40f;
        private const float TrayPitch = 250f;
        private const float TrayY = -260f;
        private const int MaxShelfItems = 5;

        // Three parts per build, in slot order - placeholder catalogue, pending a real art/content pass, same
        // as every other catalogue this session.
        private static readonly Dictionary<WorkshopBuildKind, string[]> Parts = new Dictionary<WorkshopBuildKind, string[]>
        {
            { WorkshopBuildKind.Car, new[] { "car_body", "car_wheels", "car_windows" } },
            { WorkshopBuildKind.Rocket, new[] { "rocket_body", "rocket_fins", "rocket_nosecone" } },
            { WorkshopBuildKind.House, new[] { "house_walls", "house_roof", "house_door" } },
            { WorkshopBuildKind.Boat, new[] { "boat_hull", "boat_sail", "boat_mast" } },
            { WorkshopBuildKind.Robot, new[] { "robot_body", "robot_arms", "robot_head" } },
            { WorkshopBuildKind.Bridge, new[] { "bridge_block_a", "bridge_block_b", "bridge_block_c" } },
            { WorkshopBuildKind.SimplePhysics, new[] { "physics_ramp", "physics_block", "physics_balltrack" } },
        };

        // Distractor count grows with level (0 at 1-2, 1 at 3-4, 2 at 5-6) - same "harder = more distractors"
        // shape as Zoo & Farm/Science Lab's poolSizeByLevel, applied to shelf noise instead of pool size since
        // every build only ever has its own fixed 3 parts.
        private static readonly int[] DistractorCountByLevel = { 0, 0, 1, 1, 2, 2 };

        public static WorldPoint[] SlotPositions()
        {
            var positions = new WorldPoint[SlotColumnX.Length];
            for (var i = 0; i < SlotColumnX.Length; i++) positions[i] = new WorldPoint(SlotColumnX[i], SlotY);
            return positions;
        }

        public static AssemblyRound Create(WorkshopBuildKind kind, int level, Random rng)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var parts = Parts[kind];
            var slotPositions = SlotPositions();
            var slots = new AssemblySlot[parts.Length];
            for (var i = 0; i < parts.Length; i++) slots[i] = new AssemblySlot { PartId = parts[i], Position = slotPositions[i] };

            var items = new List<WorkshopPart>();
            foreach (var partId in parts) items.Add(new WorkshopPart { PartId = partId, Correct = true });

            var distractorPool = new List<string>();
            foreach (var pair in Parts)
            {
                if (pair.Key == kind) continue;
                distractorPool.AddRange(pair.Value);
            }
            Shuffle(distractorPool, rng);
            var distractorCount = Math.Min(DistractorCountByLevel[level - DifficultyLadder.MinLevel], MaxShelfItems - parts.Length);
            for (var i = 0; i < distractorCount; i++)
                items.Add(new WorkshopPart { PartId = distractorPool[i], Correct = false });

            Shuffle(items, rng);
            var pitchStart = -(items.Count - 1) / 2f * TrayPitch;
            for (var i = 0; i < items.Count; i++)
                items[i].TrayPosition = new WorldPoint(pitchStart + i * TrayPitch, TrayY);

            var name = kind.ToString().ToLowerInvariant();
            return new AssemblyRound
            {
                Kind = kind,
                Slots = slots,
                ShelfItems = items.ToArray(),
                IntroVoiceKey = "workshop_build_" + name,
                TestVoiceKey = "workshop_test_" + name,
                TestSprite = "workshop/test_" + name,
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

    // Tool Selection / Help the Character / Balance (Workshop's other three games): all reduce directly to
    // MatchRoundBuilder's existing (id, value) shape - see the class comment above for why. Balance's "value"
    // is the correct counterweight for a scale scene that has no other property to key off of; Tool
    // Selection's/Help the Character's is the correct tool/action for a shown problem.
    public enum WorkshopMatchGameKind { ToolSelection, HelpTheCharacter, Balance }

    public static class WorkshopMatchRoundGenerator
    {
        public const int RoundsPerSession = 5;

        private static readonly int[] SixItemPool = { 4, 5, 6, 6, 6, 6 };
        private static readonly int[] ChoiceCountByLevel = { 3, 3, 4, 4, 4, 4 };

        public static MatchRound Create(WorkshopMatchGameKind kind, int level, Random rng, string previousTargetId)
        {
            var config = Config(kind);
            return MatchRoundBuilder.Build(config.items, level, rng, previousTargetId, SixItemPool, ChoiceCountByLevel,
                config.choicePrefix, config.targetPrefix, config.promptKey, config.targetVoicePrefix);
        }

        private static (IReadOnlyList<(string id, string value)> items, string choicePrefix, string targetPrefix,
            string promptKey, string targetVoicePrefix) Config(WorkshopMatchGameKind kind)
        {
            switch (kind)
            {
                case WorkshopMatchGameKind.ToolSelection:
                    return (ToolSelectionItems, "workshop/tool_", "workshop/problem_", "workshop_prompt_toolselection", "workshop_problem_");
                case WorkshopMatchGameKind.HelpTheCharacter:
                    return (HelpTheCharacterItems, "workshop/action_", "workshop/scenario_", "workshop_prompt_helpthecharacter", "workshop_scenario_");
                case WorkshopMatchGameKind.Balance:
                    return (BalanceItems, "workshop/weight_", "workshop/scale_", "workshop_prompt_balance", "workshop_scale_");
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        // Problem -> the tool that solves it.
        private static readonly (string, string)[] ToolSelectionItems =
        {
            ("cut_apple", "knife"), ("hammer_nail", "hammer"), ("tighten_screw", "screwdriver"),
            ("cut_paper", "scissors"), ("cut_wood", "saw"), ("tighten_bolt", "wrench"),
        };

        // Scenario -> the action that solves it.
        private static readonly (string, string)[] HelpTheCharacterItems =
        {
            ("hungry_dog", "give_bone"), ("thirsty_plant", "water_it"), ("cold_bird", "give_nest"),
            ("lost_kitten", "lead_home"), ("messy_room", "tidy_up"), ("flat_tire", "pump_it"),
        };

        // Scale scene (tipped one way) -> the counterweight that levels it.
        private static readonly (string, string)[] BalanceItems =
        {
            ("light_left", "small_weight"), ("heavy_left", "large_weight"), ("light_right", "small_weight"),
            ("heavy_right", "large_weight"), ("medium_left", "medium_weight"), ("medium_right", "medium_weight"),
        };
    }
}
