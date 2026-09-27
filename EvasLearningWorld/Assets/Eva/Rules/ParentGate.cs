namespace EvasLearningWorld.Rules
{
    // One multiple-choice question for the parent gate: an accidental-access barrier for a young child, not security.
    public sealed class GateQuestion
    {
        public GateQuestion(string text, int[] answers, int correctIndex) { Text = text; Answers = answers; CorrectIndex = correctIndex; }
        public string Text { get; }
        public int[] Answers { get; }
        public int CorrectIndex { get; }
    }

    public static class ParentGate
    {
        // (question, correct answer, two wrong answers close to it). Plain arithmetic an adult does at a glance and a
        // 4 to 8 year old cannot.
        private static readonly (string text, int answer, int wrongA, int wrongB)[] Pool =
        {
            ("13 x 7 = ?", 91, 81, 97),
            ("8 x 14 = ?", 112, 102, 122),
            ("144 / 12 = ?", 12, 14, 16),
            ("250 - 87 = ?", 163, 173, 153),
            ("19 + 38 = ?", 57, 47, 67),
            ("9 x 9 = ?", 81, 72, 91),
            ("15 x 6 = ?", 90, 80, 96),
            ("200 - 68 = ?", 132, 142, 122),
            ("72 / 8 = ?", 9, 8, 12),
            ("47 + 26 = ?", 73, 63, 83),
            ("12 x 11 = ?", 132, 122, 144),
            ("300 - 145 = ?", 155, 165, 145),
            ("56 + 78 = ?", 134, 124, 144),
            ("7 x 16 = ?", 112, 102, 126),
            ("96 / 6 = ?", 16, 18, 14),
            ("64 + 59 = ?", 123, 113, 133),
            ("18 x 5 = ?", 90, 80, 85),
            ("500 - 237 = ?", 263, 273, 253),
            ("11 x 13 = ?", 143, 133, 153),
            ("81 / 9 = ?", 9, 7, 11),
        };

        public static int Count => Pool.Length;

        // The question at `poolIndex` (wrapping around the pool) with its three answers rotated by `rotation` places,
        // so the correct answer is not always in the same slot.
        public static GateQuestion Build(int poolIndex, int rotation)
        {
            var item = Pool[((poolIndex % Pool.Length) + Pool.Length) % Pool.Length];
            var ordered = new[] { item.answer, item.wrongA, item.wrongB };
            var shift = ((rotation % 3) + 3) % 3;
            var answers = new int[3];
            var correct = 0;
            for (var i = 0; i < 3; i++)
            {
                answers[(i + shift) % 3] = ordered[i];
                if (i == 0) correct = (i + shift) % 3;
            }
            return new GateQuestion(item.text, answers, correct);
        }
    }
}
