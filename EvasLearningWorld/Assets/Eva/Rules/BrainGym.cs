using System;
using System.Collections.Generic;
using System.Linq;

namespace EvasLearningWorld.Rules
{
    // Brain Gym (M4.8, docs/kids-games/full-catalogue-plan.md "10. Brain Gym"): the plan's biggest single
    // building by game count (21). Two new presenters are stood up once and reused: MemoryBoardScreen (Classic
    // Memory's flip-and-match-pairs board) and SequenceRecallScreen (a flash-then-recall study phase in front of
    // SequenceScreen's own tap-in-order shape, shared by Remember the Sequence and Simon Says - the plan's own
    // note that "Simon Says shares Remember the Sequence's mechanic"). Sequence Ordering reuses SequenceScreen
    // directly (Science Lab's Plant Growth precedent), needing nothing new here beyond a stage pool. Every other
    // game (17 of the 21) reduces to the existing MATCH presenter, the same "reduce a judged-comparison or
    // categorisation game to tap-the-correct-tile" call every earlier building has made for its own harder rows
    // (Workshop's Balance, Art Studio's Finish the Drawing, Zoo & Farm's whole SORT cluster).
    //
    // Design calls (2026-09-27, same pattern as every earlier building's simplifications, flagged for Adrian's
    // review):
    // - What's Disappeared, Same or Different, Match Rotation, Complete the Picture, Find the Differences, Spot
    //   the Object, What's Behind, Perspective, Copy the Construction and Find the Missing Piece are all built as
    //   single-round MATCH (a self-referential id/value pair, same shape as Art Studio's Finish the Drawing)
    //   rather than their own bespoke compare/spot/construct mechanic. Copy the Construction in particular drops
    //   DRAG & DROP for tap-the-matching-photo, the same "reduce placement to tap-choice" call Science Lab's
    //   Sink or Float made.
    // - Which Is Bigger is built as a binary MATCH (tap the bigger/smaller size icon for a shown animal) rather
    //   than a side-by-side pairwise comparison tap.
    // - Remember the Sequence and Simon Says both drop true Simon-style repeat-allowed growing sequences for a
    //   fixed-pool, no-repeat order (the same order-of-a-prefix shape SequenceRoundBuilder already gives Plant
    //   Growth), with a flash/study phase in front of it - see SequenceRecallScreen. Simon Says is the same
    //   presenter over a second, colour-pad content pool, exactly as Art Studio's Drawing Challenges reused
    //   Guided Drawing's presenter over its own pack.
    // - Classic Memory's board size (2-6 pairs by level) and its hint/demonstrate steps (peek a pair / auto-match
    //   a pair) are a new small ladder built for this one game - see MemoryBoardRoundGenerator.
    // Content across every dataset below is placeholder, pending a real art/content pass, same caveat as every
    // catalogue built this session.

    // --- Classic Memory (its own mechanic - see App/Screens/MemoryBoardScreen.cs) --------------------------------

    public sealed class MemoryRound
    {
        // Board cells in display order, each an item id; every id appears exactly twice.
        public string[] Board;
    }

    public static class MemoryBoardRoundGenerator
    {
        public const int RoundsPerSession = 3;
        public const string CardSpritePrefix = "braingym/memory_";
        public const string CardBackSprite = "braingym/memory_back";

        private static readonly int[] PairsByLevel = { 2, 3, 4, 4, 5, 6 };
        private static readonly string[] ItemIds = { "cat", "dog", "ball", "star", "sun", "tree" };

        public static MemoryRound Create(int level, Random rng)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var pairs = Math.Min(PairsByLevel[level - DifficultyLadder.MinLevel], ItemIds.Length);

            var board = new List<string>();
            for (var i = 0; i < pairs; i++) { board.Add(ItemIds[i]); board.Add(ItemIds[i]); }
            Shuffle(board, rng);

            return new MemoryRound { Board = board.ToArray() };
        }

        private static void Shuffle(List<string> list, Random rng)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = rng.Next(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }

    // --- Remember the Sequence / Simon Says (own mechanic - see App/Screens/SequenceRecallScreen.cs) -------------

    public enum SequenceRecallGameKind { RememberTheSequence, SimonSays }

    public static class SequenceRecallRoundGenerator
    {
        public const int RoundsPerSession = 4;
        private static readonly int[] StageCountByLevel = { 2, 2, 3, 3, 4, 4 };

        public const string RememberTheSequenceTileSpritePrefix = "braingym/seqtile_";
        public const string SimonSaysTileSpritePrefix = "braingym/pad_";

        private static readonly string[] RememberTheSequenceTiles = { "circle", "square", "triangle", "star", "heart", "diamond" };
        private static readonly string[] SimonSaysTiles = { "red", "blue", "green", "yellow", "purple", "orange" };

        public static SequenceRound Create(SequenceRecallGameKind kind, int level, Random rng) =>
            SequenceRoundBuilder.Build(kind == SequenceRecallGameKind.RememberTheSequence ? RememberTheSequenceTiles : SimonSaysTiles,
                level, rng, StageCountByLevel);
    }

    // --- Sequence Ordering (reuses SequenceScreen directly, same as Science Lab's Plant Growth) --------------------

    public static class BrainGymSequenceOrderingRoundGenerator
    {
        public const int RoundsPerSession = 5;
        public const string TileSpritePrefix = "braingym/routine_";

        private static readonly string[] Stages = { "wake", "breakfast", "school", "play", "dinner", "sleep" };
        private static readonly int[] StageCountByLevel = { 2, 3, 4, 4, 5, 6 };

        public static SequenceRound Create(int level, Random rng) => SequenceRoundBuilder.Build(Stages, level, rng, StageCountByLevel);
    }

    // --- MATCH-shaped games (17 of Brain Gym's 21) ----------------------------------------------------------------

    public enum BrainGymMatchGameKind
    {
        WhatsDisappeared, RememberTheLocation, SameOrDifferent, MatchRotation, WhichIsBigger, CompleteThePicture,
        FindTheDifferences, SpotTheObject, FollowThePath, WhatsBehind, Perspective, CopyTheConstruction,
        FindTheMissingPiece, Sorting, Recycling, MatchItemToCategory, SortLaundryChores,
    }

    public static class BrainGymMatchRoundGenerator
    {
        public const int RoundsPerSession = 5;

        private static readonly int[] DefaultPoolSizeByLevel = { 4, 5, 6, 6, 6, 6 };
        private static readonly int[] DefaultChoiceCountByLevel = { 2, 3, 3, 4, 4, 4 };
        // Only two possible values exist (a binary judgement), so the choice count never grows past 2.
        private static readonly int[] BinaryPoolSizeByLevel = { 2, 3, 4, 5, 6, 6 };
        private static readonly int[] BinaryChoiceCountByLevel = { 2, 2, 2, 2, 2, 2 };

        public static MatchRound Create(BrainGymMatchGameKind kind, int level, Random rng, string previousTargetId)
        {
            var config = Config(kind);
            var poolSizes = kind == BrainGymMatchGameKind.WhichIsBigger ? BinaryPoolSizeByLevel : DefaultPoolSizeByLevel;
            var choiceCounts = kind == BrainGymMatchGameKind.WhichIsBigger ? BinaryChoiceCountByLevel : DefaultChoiceCountByLevel;
            return MatchRoundBuilder.Build(config.items, level, rng, previousTargetId, poolSizes, choiceCounts,
                config.choicePrefix, config.targetPrefix, config.promptKey, config.targetVoicePrefix);
        }

        private static (IReadOnlyList<(string id, string value)> items, string choicePrefix, string targetPrefix,
            string promptKey, string targetVoicePrefix) Config(BrainGymMatchGameKind kind)
        {
            switch (kind)
            {
                case BrainGymMatchGameKind.WhatsDisappeared:
                    return (WhatsDisappearedItems, "braingym/item_", "braingym/scene_",
                        "braingym_prompt_whatsdisappeared", "braingym_item_");
                case BrainGymMatchGameKind.RememberTheLocation:
                    return (RememberTheLocationItems, "braingym/position_", "braingym/scene_",
                        "braingym_prompt_rememberthelocation", "braingym_position_");
                case BrainGymMatchGameKind.SameOrDifferent:
                    return (SameOrDifferentItems, "braingym/match_", "braingym/ref_",
                        "braingym_prompt_sameordifferent", "braingym_match_");
                case BrainGymMatchGameKind.MatchRotation:
                    return (MatchRotationItems, "braingym/rotated_", "braingym/shape_",
                        "braingym_prompt_matchrotation", "braingym_shape_");
                case BrainGymMatchGameKind.WhichIsBigger:
                    return (WhichIsBiggerItems, "braingym/size_", "braingym/sizeitem_",
                        "braingym_prompt_whichisbigger", "braingym_sizeitem_");
                case BrainGymMatchGameKind.CompleteThePicture:
                    return (CompleteThePictureItems, "braingym/piece_", "braingym/incomplete_",
                        "braingym_prompt_completethepicture", "braingym_piece_");
                case BrainGymMatchGameKind.FindTheDifferences:
                    return (FindTheDifferencesItems, "braingym/spot_", "braingym/scenepair_",
                        "braingym_prompt_findthedifferences", "braingym_spot_");
                case BrainGymMatchGameKind.SpotTheObject:
                    return (SpotTheObjectItems, "braingym/found_", "braingym/hidden_",
                        "braingym_prompt_spottheobject", "braingym_found_");
                case BrainGymMatchGameKind.FollowThePath:
                    return (FollowThePathItems, "braingym/destination_", "braingym/path_",
                        "braingym_prompt_followthepath", "braingym_destination_");
                case BrainGymMatchGameKind.WhatsBehind:
                    return (WhatsBehindItems, "braingym/behind_", "braingym/infront_",
                        "braingym_prompt_whatsbehind", "braingym_behind_");
                case BrainGymMatchGameKind.Perspective:
                    return (PerspectiveItems, "braingym/viewb_", "braingym/viewa_",
                        "braingym_prompt_perspective", "braingym_viewb_");
                case BrainGymMatchGameKind.CopyTheConstruction:
                    return (CopyTheConstructionItems, "braingym/build_", "braingym/model_",
                        "braingym_prompt_copytheconstruction", "braingym_build_");
                case BrainGymMatchGameKind.FindTheMissingPiece:
                    return (FindTheMissingPieceItems, "braingym/piece2_", "braingym/puzzle_",
                        "braingym_prompt_findthemissingpiece", "braingym_piece2_");
                case BrainGymMatchGameKind.Sorting:
                    return (SortingItems, "braingym/category_", "braingym/sortitem_",
                        "braingym_prompt_sorting", "braingym_category_");
                case BrainGymMatchGameKind.Recycling:
                    return (RecyclingItems, "braingym/bin_", "braingym/waste_",
                        "braingym_prompt_recycling", "braingym_bin_");
                case BrainGymMatchGameKind.MatchItemToCategory:
                    return (MatchItemToCategoryItems, "braingym/categorylabel_", "braingym/catitem_",
                        "braingym_prompt_matchitemtocategory", "braingym_categorylabel_");
                case BrainGymMatchGameKind.SortLaundryChores:
                    return (SortLaundryChoresItems, "braingym/room_", "braingym/choreitem_",
                        "braingym_prompt_sortlaundrychores", "braingym_room_");
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        // Self-referential (value == id): the target picture and the correct choice tile name the same thing,
        // same shape as Art Studio's Finish the Drawing/Draw What You Hear.
        private static readonly (string, string)[] WhatsDisappearedItems =
        {
            ("apple", "apple"), ("ball", "ball"), ("cup", "cup"), ("hat", "hat"), ("kite", "kite"), ("shoe", "shoe"),
        };

        private static readonly (string, string)[] RememberTheLocationItems =
        {
            ("scene1", "posa"), ("scene2", "posb"), ("scene3", "posc"), ("scene4", "posd"), ("scene5", "posb"), ("scene6", "posc"),
        };

        private static readonly (string, string)[] SameOrDifferentItems =
        {
            ("star", "star"), ("heart", "heart"), ("cloud", "cloud"), ("leaf", "leaf"), ("shell", "shell"), ("gem", "gem"),
        };

        private static readonly (string, string)[] MatchRotationItems =
        {
            ("circle", "circle"), ("square", "square"), ("triangle", "triangle"), ("star", "star"), ("arrow", "arrow"), ("heart", "heart"),
        };

        private static readonly (string, string)[] WhichIsBiggerItems =
        {
            ("elephant", "bigger"), ("ant", "smaller"), ("whale", "bigger"), ("ladybug", "smaller"), ("giraffe", "bigger"), ("mouse", "smaller"),
        };

        private static readonly (string, string)[] CompleteThePictureItems =
        {
            ("sun", "sun"), ("flower", "flower"), ("house", "house"), ("tree", "tree"), ("car", "car"), ("balloon", "balloon"),
        };

        private static readonly (string, string)[] FindTheDifferencesItems =
        {
            ("scenea", "scenea"), ("sceneb", "sceneb"), ("scenec", "scenec"), ("scened", "scened"), ("scenee", "scenee"), ("scenef", "scenef"),
        };

        private static readonly (string, string)[] SpotTheObjectItems =
        {
            ("apple", "apple"), ("ball", "ball"), ("cup", "cup"), ("hat", "hat"), ("kite", "kite"), ("shoe", "shoe"),
        };

        private static readonly (string, string)[] FollowThePathItems =
        {
            ("path1", "house"), ("path2", "tree"), ("path3", "star"), ("path4", "flag"), ("path5", "house"), ("path6", "tree"),
        };

        private static readonly (string, string)[] WhatsBehindItems =
        {
            ("cat", "cat"), ("dog", "dog"), ("ball", "ball"), ("box", "box"), ("tree", "tree"), ("car", "car"),
        };

        private static readonly (string, string)[] PerspectiveItems =
        {
            ("cube", "cube"), ("cup", "cup"), ("chair", "chair"), ("house", "house"), ("car", "car"), ("ball", "ball"),
        };

        private static readonly (string, string)[] CopyTheConstructionItems =
        {
            ("towera", "towera"), ("towerb", "towerb"), ("towerc", "towerc"), ("towerd", "towerd"), ("towere", "towere"), ("towerf", "towerf"),
        };

        private static readonly (string, string)[] FindTheMissingPieceItems =
        {
            ("puzzle1", "puzzle1"), ("puzzle2", "puzzle2"), ("puzzle3", "puzzle3"), ("puzzle4", "puzzle4"), ("puzzle5", "puzzle5"), ("puzzle6", "puzzle6"),
        };

        private static readonly (string, string)[] SortingItems =
        {
            ("apple", "fruit"), ("carrot", "vegetable"), ("shirt", "clothes"), ("truck", "vehicle"), ("banana", "fruit"), ("pants", "clothes"),
        };

        private static readonly (string, string)[] RecyclingItems =
        {
            ("bottle", "plastic"), ("newspaper", "paper"), ("jar", "glass"), ("bananapeel", "organic"), ("can", "plastic"), ("cardboard", "paper"),
        };

        private static readonly (string, string)[] MatchItemToCategoryItems =
        {
            ("guitar", "music"), ("ball", "sports"), ("book", "reading"), ("paintbrush", "art"), ("drum", "music"), ("bat", "sports"),
        };

        private static readonly (string, string)[] SortLaundryChoresItems =
        {
            ("shirt", "hamper"), ("dish", "kitchen"), ("toy", "bedroom"), ("towel", "bathroom"), ("sock", "hamper"), ("book", "bedroom"),
        };
    }
}
