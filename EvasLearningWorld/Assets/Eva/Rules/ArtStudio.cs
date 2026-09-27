using System;
using System.Collections.Generic;
using System.Linq;

namespace EvasLearningWorld.Rules
{
    // Art Studio (M4.7, docs/kids-games/full-catalogue-plan.md "9. Art Studio"): introduces TRACE (finger
    // follows a path within tolerance) - the building's one new must-have mechanic, built once below and
    // reused by all three trace games (Trace Shapes/Letters/Numbers). The other six reduce to presenters that
    // already exist: four to MATCH (Color by Number, Color by Instruction, Finish the Drawing, Draw What You
    // Hear) and two to SEQUENCE (Guided Drawing, Drawing Challenges - the latter is explicitly "Guided
    // Drawing's presenter over a second offline-authored content pack" per the plan). Free Drawing, the tenth,
    // has no round/target/help-ladder shape at all - see Rules/ArtStudio.cs's FreeDrawing catalogue at the
    // bottom and App/Screens/FreeDrawingScreen.cs for its own small mechanic.
    //
    // Design calls (2026-09-27, same pattern as every earlier building's simplifications, flagged for Adrian's
    // review):
    // - Color by Number / Color by Instruction are both built as single-round MATCH (tap the correct color
    //   swatch for a shown/spoken scene part) rather than a multi-region full coloring page per round - lower
    //   risk, ships now, still teaches "which color goes where."
    // - Finish the Drawing is built as MATCH (tap the correct missing-half tile among choices) rather than
    //   DRAG & DROP - same "reduce a placement game to a tap-choice" call Science Lab's Sink or Float/Magnet
    //   made.
    // - Draw What You Hear is built as MATCH (Eva narrates a scene, tap the one completed picture that matches)
    //   rather than assembling the scene from individual shape pieces - reuses the audio-led shape Weather/
    //   Space already established (TargetSprite null, only voice lines).
    // - Trace Shapes/Letters/Numbers' path data (circle/triangle/square/star, and the letter/number outlines)
    //   is procedurally generated placeholder geometry, not real glyph/shape art - flagged same as every other
    //   catalogue this session; a real art/content pass owns the final paths.
    // Content across every dataset below is placeholder, pending a real art/content pass, same caveat as
    // every catalogue built this session.

    // --- TRACE -----------------------------------------------------------------------------------------------

    public enum TraceGameKind { Shapes, Letters, Numbers }

    public sealed class TraceRound
    {
        public string Id;
        // The path to trace, in canvas units (centre origin, y up) - open for a single-stroke glyph, or
        // closed (last point repeats the first) for a shape. TraceScreen only ever reads this as a polyline.
        public WorldPoint[] Path;
        public string PromptVoiceKey;
    }

    public static class TraceRoundGenerator
    {
        public const int RoundsPerSession = 5;

        // More shapes/glyphs enter the rotation as level rises - same "poolSizeByLevel" shape as
        // MatchRoundBuilder, applied to which ids are eligible instead of how many choice tiles are shown.
        private static readonly int[] PoolSizeByLevel = { 2, 2, 3, 4, 5, 6 };

        private static readonly string[] ShapeIds = { "circle", "square", "triangle", "star" };
        private static readonly string[] LetterIds = { "l", "o", "t", "c", "x", "s" };
        private static readonly string[] NumberIds = { "one", "zero", "seven", "two", "four", "three" };

        public static TraceRound Create(TraceGameKind kind, int level, Random rng, string previousId)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var ids = IdsFor(kind);
            var poolSize = Math.Min(PoolSizeByLevel[level - DifficultyLadder.MinLevel], ids.Length);
            var pool = ids.Take(poolSize).ToList();

            var id = pool[rng.Next(pool.Count)];
            if (previousId != null && pool.Count > 1)
                while (id == previousId) id = pool[rng.Next(pool.Count)];

            return new TraceRound
            {
                Id = id,
                Path = TraceCatalog.PathFor(kind, id),
                PromptVoiceKey = "artstudio_trace_" + Prefix(kind) + "_" + id,
            };
        }

        private static string[] IdsFor(TraceGameKind kind)
        {
            switch (kind)
            {
                case TraceGameKind.Shapes: return ShapeIds;
                case TraceGameKind.Letters: return LetterIds;
                case TraceGameKind.Numbers: return NumberIds;
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        private static string Prefix(TraceGameKind kind)
        {
            switch (kind)
            {
                case TraceGameKind.Shapes: return "shape";
                case TraceGameKind.Letters: return "letter";
                case TraceGameKind.Numbers: return "number";
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
    }

    // Generates every trace path in canvas units, centred at (Cx, Cy) with a fixed size, so TraceScreen never
    // needs to know how a shape/glyph is built - only that it gets an open-or-closed polyline back. Shapes are
    // regular polygons/stars (closed: the first point repeats at the end); letters and numbers are small
    // hand-picked single-stroke polylines (open) - a continuous path a finger can trace without lifting,
    // chosen for legibility over faithfulness to the real glyph. All placeholder geometry, see the class
    // comment above.
    public static class TraceCatalog
    {
        public const float Cx = -150f;
        public const float Cy = 40f;
        public const float ShapeRadius = 220f;
        public const float GlyphSize = 220f;

        public static WorldPoint[] PathFor(TraceGameKind kind, string id)
        {
            switch (kind)
            {
                case TraceGameKind.Shapes: return ShapePath(id);
                case TraceGameKind.Letters: return FromNormalized(LetterPaths[id]);
                case TraceGameKind.Numbers: return FromNormalized(NumberPaths[id]);
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        private static WorldPoint[] ShapePath(string id)
        {
            switch (id)
            {
                case "circle": return RegularPolygon(12, ShapeRadius, -90f);
                case "square": return RegularPolygon(4, ShapeRadius, -45f);
                case "triangle": return RegularPolygon(3, ShapeRadius, -90f);
                case "star": return Star(ShapeRadius, ShapeRadius * 0.45f);
                default: throw new ArgumentOutOfRangeException(nameof(id));
            }
        }

        // A closed regular polygon (or, at 12 sides, a circle placeholder): `sides` outer vertices plus the
        // first vertex repeated at the end so the traced path visibly closes.
        private static WorldPoint[] RegularPolygon(int sides, float radius, float startAngleDeg)
        {
            var points = new WorldPoint[sides + 1];
            for (var i = 0; i <= sides; i++)
            {
                var angle = (startAngleDeg + 360.0 * i / sides) * Math.PI / 180.0;
                points[i] = new WorldPoint(Cx + (float)(radius * Math.Cos(angle)), Cy + (float)(radius * Math.Sin(angle)));
            }
            return points;
        }

        // A closed 5-point star: 10 vertices alternating outer/inner radius, plus the first repeated to close.
        private static WorldPoint[] Star(float outerRadius, float innerRadius)
        {
            var points = new WorldPoint[11];
            for (var i = 0; i <= 10; i++)
            {
                var r = i % 2 == 0 ? outerRadius : innerRadius;
                var angle = (-90.0 + 36.0 * i) * Math.PI / 180.0;
                points[i] = new WorldPoint(Cx + (float)(r * Math.Cos(angle)), Cy + (float)(r * Math.Sin(angle)));
            }
            return points;
        }

        private static WorldPoint[] FromNormalized((float x, float y)[] pts)
        {
            var result = new WorldPoint[pts.Length];
            for (var i = 0; i < pts.Length; i++)
                result[i] = new WorldPoint(Cx + pts[i].x * GlyphSize, Cy + pts[i].y * GlyphSize);
            return result;
        }

        // Single-stroke placeholder outlines, normalised to [-1, 1] (y up). Not real glyph art - see the class
        // comment above.
        private static readonly Dictionary<string, (float x, float y)[]> LetterPaths = new Dictionary<string, (float x, float y)[]>
        {
            { "l", new[] { (0f, 1f), (0f, -1f), (0.6f, -1f) } },
            { "o", new[] { (0f, 1f), (0.7f, 0.6f), (0.7f, -0.6f), (0f, -1f), (-0.7f, -0.6f), (-0.7f, 0.6f), (0f, 1f) } },
            { "t", new[] { (-0.6f, 1f), (0.6f, 1f), (0f, 1f), (0f, -1f) } },
            { "c", new[] { (0.5f, 0.8f), (-0.3f, 1f), (-0.8f, 0.3f), (-0.8f, -0.3f), (-0.3f, -1f), (0.5f, -0.8f) } },
            { "x", new[] { (-0.7f, 1f), (0.7f, -1f), (0.7f, 1f), (-0.7f, -1f) } },
            { "s", new[] { (0.5f, 1f), (-0.5f, 0.6f), (0.5f, 0.1f), (-0.5f, -0.4f), (0.5f, -1f) } },
        };

        private static readonly Dictionary<string, (float x, float y)[]> NumberPaths = new Dictionary<string, (float x, float y)[]>
        {
            { "one", new[] { (-0.3f, 0.7f), (0f, 1f), (0f, -1f), (-0.5f, -1f), (0.5f, -1f) } },
            { "zero", new[] { (0f, 1f), (0.6f, 0.5f), (0.6f, -0.5f), (0f, -1f), (-0.6f, -0.5f), (-0.6f, 0.5f), (0f, 1f) } },
            { "seven", new[] { (-0.6f, 1f), (0.6f, 1f), (-0.2f, -1f) } },
            { "two", new[] { (-0.5f, 0.7f), (0.1f, 1f), (0.6f, 0.5f), (-0.6f, -1f), (0.6f, -1f) } },
            { "four", new[] { (0.3f, 1f), (-0.6f, -0.2f), (0.6f, -0.2f), (0.3f, -1f) } },
            { "three", new[] { (-0.5f, 0.9f), (0.5f, 0.9f), (-0.1f, 0.1f), (0.5f, -0.5f), (-0.5f, -0.9f) } },
        };
    }

    // --- MATCH-shaped games (Color by Number, Color by Instruction, Finish the Drawing, Draw What You Hear) --

    public enum ArtStudioMatchGameKind { ColorByNumber, ColorByInstruction, FinishTheDrawing, DrawWhatYouHear }

    public static class ArtStudioMatchRoundGenerator
    {
        public const int RoundsPerSession = 5;

        private static readonly int[] SixItemPool = { 4, 5, 6, 6, 6, 6 };
        private static readonly int[] ChoiceCountByLevel = { 3, 3, 4, 4, 4, 4 };

        public static MatchRound Create(ArtStudioMatchGameKind kind, int level, Random rng, string previousTargetId)
        {
            var config = Config(kind);
            return MatchRoundBuilder.Build(config.items, level, rng, previousTargetId, SixItemPool, ChoiceCountByLevel,
                config.choicePrefix, config.targetPrefix, config.promptKey, config.targetVoicePrefix);
        }

        private static (IReadOnlyList<(string id, string value)> items, string choicePrefix, string targetPrefix,
            string promptKey, string targetVoicePrefix) Config(ArtStudioMatchGameKind kind)
        {
            switch (kind)
            {
                case ArtStudioMatchGameKind.ColorByNumber:
                    return (ScenePartColorItems, "artstudio/swatch_", "artstudio/numbered_region_",
                        "artstudio_prompt_colorbynumber", "artstudio_region_");
                case ArtStudioMatchGameKind.ColorByInstruction:
                    return (ScenePartColorItems, "artstudio/swatch_", "artstudio/plain_region_",
                        "artstudio_prompt_colorbyinstruction", "artstudio_instruction_");
                case ArtStudioMatchGameKind.FinishTheDrawing:
                    return (FinishTheDrawingItems, "artstudio/piece_", "artstudio/half_",
                        "artstudio_prompt_finishthedrawing", "artstudio_piece_");
                case ArtStudioMatchGameKind.DrawWhatYouHear:
                    return (DrawWhatYouHearItems, "artstudio/scene_", null,
                        "artstudio_prompt_drawwhatyouhear", "artstudio_scene_");
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        // Scene part -> the color it should be painted, shared by Color by Number (a numbered region names the
        // part) and Color by Instruction (Eva names the part instead) - same target/value shape, just a
        // different target sprite prefix and prompt per game (see Config above).
        private static readonly (string, string)[] ScenePartColorItems =
        {
            ("roof", "red"), ("wall", "yellow"), ("door", "brown"), ("window", "blue"), ("sun", "orange"), ("tree", "green"),
        };

        // Half-drawn picture -> its own missing half (value equals id, same self-referential shape as
        // Science Lab's Weather/Space audio-led items).
        private static readonly (string, string)[] FinishTheDrawingItems =
        {
            ("sun", "sun"), ("flower", "flower"), ("house", "house"), ("tree", "tree"), ("car", "car"), ("balloon", "balloon"),
        };

        // Audio-led (TargetSprite null - Eva narrates the whole scene): the choice tiles are candidate
        // completed pictures, only one matches the narration.
        private static readonly (string, string)[] DrawWhatYouHearItems =
        {
            ("scene1", "scene1"), ("scene2", "scene2"), ("scene3", "scene3"),
            ("scene4", "scene4"), ("scene5", "scene5"), ("scene6", "scene6"),
        };
    }

    // --- SEQUENCE-shaped games (Guided Drawing, Drawing Challenges) -------------------------------------------

    public enum ArtStudioSequenceGameKind { GuidedDrawing, DrawingChallenges }

    public static class ArtStudioSequenceRoundGenerator
    {
        public const int RoundsPerSession = 4;

        private static readonly string[] GuidedDrawingStages = { "roof", "walls", "door", "windows" };
        private static readonly string[] DrawingChallengesStages = { "body", "head", "arms", "legs" };
        private static readonly int[] StageCountByLevel = { 2, 2, 3, 3, 4, 4 };

        public static SequenceRound Create(ArtStudioSequenceGameKind kind, int level, Random rng) =>
            SequenceRoundBuilder.Build(kind == ArtStudioSequenceGameKind.GuidedDrawing ? GuidedDrawingStages : DrawingChallengesStages,
                level, rng, StageCountByLevel);
    }

    // --- Free Drawing (its own mechanic - see App/Screens/FreeDrawingScreen.cs) --------------------------------

    // Free Drawing has no goal, round or help ladder, so no round-generator shape applies - just a fixed
    // palette/stamp catalogue for the open canvas.
    public static class FreeDrawingCatalog
    {
        public static readonly string[] Colors = { "red", "orange", "yellow", "green", "blue", "purple" };
        public static readonly string[] Stamps = { "circle", "star", "heart", "sun", "tree", "flower" };
    }
}
