using System;

namespace EvasLearningWorld.Rules
{
    public sealed class RotateThePieceRound
    {
        // Which placeholder piece sprite to show ("rotatepiece/piece_0".."piece_5") - a stand-in for "piece
        // complexity" until real art exists to actually vary in shape; for now it only guarantees variety
        // round to round, same as OddOneOut's item catalogue before real art.
        public string PieceSprite;

        public float StartAngle;
        public float TargetAngle;

        // 0 means any angle counts; otherwise the piece may only ever be shown at a multiple of this many
        // degrees (RotateDragger, App/Ui, is the one that actually enforces the snap while dragging).
        public float StepDegrees;

        // How close (in degrees, shortest way around) the piece's current angle must be to TargetAngle to count
        // as matched.
        public float ToleranceDegrees;
    }

    // Rotate the Piece (Playground, spec 4.1): the plan's one new mechanic between the NAVIGATION cluster and
    // the DRAG&DROP cluster - a rotate handle/gesture rather than a slide (PathDragger) or a free drag
    // (DragItem). Progression: rotation steps (90 degrees only, easing toward any angle) and piece "complexity"
    // (see PieceSprite above). See RotateDragger (App/Ui) for the drag-to-rotate control itself.
    public static class RotateThePieceRoundGenerator
    {
        public const int RoundsPerSession = 5;
        public const int PieceCount = 6;

        // Index i = level (i + 1): coarser steps ease into free rotation as level rises; tolerance widens a
        // little for the free-rotation levels since matching an exact angle by feel alone is harder than
        // snapping to a quarter-turn.
        private static readonly float[] StepDegreesByLevel = { 90f, 90f, 45f, 45f, 0f, 0f };
        private static readonly float[] ToleranceDegreesByLevel = { 10f, 10f, 8f, 8f, 14f, 14f };

        public static RotateThePieceRound Create(int level, Random rng)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var index = level - DifficultyLadder.MinLevel;
            var step = StepDegreesByLevel[index];
            var tolerance = ToleranceDegreesByLevel[index];
            var pieceSprite = "rotatepiece/piece_" + rng.Next(PieceCount);

            float targetAngle, startAngle;
            if (step > 0f)
            {
                var positions = (int)Math.Round(360f / step);
                targetAngle = rng.Next(positions) * step;
                // Never the same orientation as the target - some non-zero multiple of the step away from it.
                var offset = 1 + rng.Next(positions - 1);
                startAngle = Normalize(targetAngle + offset * step);
            }
            else
            {
                targetAngle = (float)(rng.NextDouble() * 360.0);
                // Comfortably outside the tolerance band in either direction, so the round never starts already
                // matched.
                var minOffset = tolerance * 3f;
                var offset = minOffset + (float)(rng.NextDouble() * (360f - minOffset * 2f));
                startAngle = Normalize(targetAngle + offset);
            }

            return new RotateThePieceRound
            {
                PieceSprite = pieceSprite,
                StartAngle = startAngle,
                TargetAngle = targetAngle,
                StepDegrees = step,
                ToleranceDegrees = tolerance,
            };
        }

        public static bool IsMatch(RotateThePieceRound round, float currentAngle) =>
            AngleDifference(currentAngle, round.TargetAngle) <= round.ToleranceDegrees;

        // The shortest angular distance between two angles (0-180 degrees), whichever way around is closer.
        public static float AngleDifference(float a, float b)
        {
            var diff = Normalize(a - b);
            if (diff > 180f) diff = 360f - diff;
            return diff;
        }

        public static float Normalize(float angle)
        {
            angle %= 360f;
            if (angle < 0f) angle += 360f;
            return angle;
        }
    }
}
