using EvasLearningWorld.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // A plain adult-facing barrier in front of the settings: one arithmetic question with three answers. A wrong answer
    // (or the Hud Home button) goes back to the Map. Not security, only an accidental-access barrier.
    public sealed class ParentGateScreen : ScreenBase
    {
        private static readonly Vector2 AnswerSize = new Vector2(300f, 240f);

        private EvaGame _game;
        private TextMeshProUGUI _question;
        private readonly TextMeshProUGUI[] _labels = new TextMeshProUGUI[3];
        private GateQuestion _current;
        private int _next;

        public override void Build(EvaGame game)
        {
            _game = game;
            AddBackground(new Color(0.96f, 0.93f, 0.85f));

            _question = Label(Root, "Question", 90, new Vector2(0f, 200f), new Vector2(1100f, 200f));
            for (var i = 0; i < 3; i++)
            {
                var index = i;
                var button = EvaUi.IconButton(Root, "Answer" + i, EvaUi.Sprite("icons/tile"), new Vector2(0.5f, 0.5f),
                    new Vector2((i - 1) * 380f, -80f), AnswerSize, () => OnAnswer(index));
                _labels[i] = Label(button.transform, "Label", 90, Vector2.zero, AnswerSize);
            }
        }

        public override void OnShow()
        {
            _current = ParentGate.Build(_next, _next / ParentGate.Count);
            _next++;
            _question.text = _current.Text;
            for (var i = 0; i < 3; i++) _labels[i].text = _current.Answers[i].ToString();
        }

        private void OnAnswer(int index)
        {
            _game.Navigator.Show(index == _current.CorrectIndex ? ScreenId.Settings : ScreenId.Map);
        }

        private static TextMeshProUGUI Label(Transform parent, string name, int fontSize, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = go.GetComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.2f, 0.15f, 0.1f);
            text.raycastTarget = false;
            return text;
        }
    }
}
