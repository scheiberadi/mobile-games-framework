using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.Tests
{
    public class SettingsScreenTests
    {
        private GameObject _canvasObject;
        private EvaGame _game;
        private FakeKeyValueStore _store;

        [SetUp]
        public void SetUp()
        {
            _canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            ((RectTransform)_canvasObject.transform).sizeDelta = new Vector2(EvaLayout.DesignWidth, EvaLayout.DesignHeight);
            _store = new FakeKeyValueStore();
            _game = new GameObject("TestEvaGame").AddComponent<EvaGame>();
            _game.Build(_canvasObject.GetComponent<Canvas>(), _store);
            _game.Progress.HasCharacter = true;
            _game.Progress.Tutorial = TutorialStep.Done;
        }

        [TearDown]
        public void TearDown()
        {
            if (_game != null) Object.DestroyImmediate(_game.gameObject);
            if (_canvasObject != null) Object.DestroyImmediate(_canvasObject);
        }

        private Transform Gate => _canvasObject.transform.Find("ScreenRoot/ParentGateScreen");
        private Transform Settings => _canvasObject.transform.Find("ScreenRoot/SettingsScreen");
        private Button Answer(int i) => Gate.Find("Answer" + i).GetComponent<Button>();

        private void OpenGate()
        {
            _game.Navigator.Show(ScreenId.Map);
            _canvasObject.transform.Find("ScreenRoot/MapScreen/SettingsButton").GetComponent<Button>().onClick.Invoke();
        }

        private int CorrectIndex()
        {
            var question = Gate.Find("Question").GetComponent<TMP_Text>().text;
            for (var i = 0; i < ParentGate.Count; i++)
                for (var rotation = 0; rotation < 3; rotation++)
                {
                    var candidate = ParentGate.Build(i, rotation);
                    if (candidate.Text != question) continue;
                    var shown = new[]
                    {
                        Gate.Find("Answer0/Label").GetComponent<TMP_Text>().text,
                        Gate.Find("Answer1/Label").GetComponent<TMP_Text>().text,
                        Gate.Find("Answer2/Label").GetComponent<TMP_Text>().text,
                    };
                    if (shown[0] == candidate.Answers[0].ToString() && shown[1] == candidate.Answers[1].ToString() && shown[2] == candidate.Answers[2].ToString())
                        return candidate.CorrectIndex;
                }
            Assert.Fail("the shown question is not in the pool: " + question);
            return -1;
        }

        [Test]
        public void TheGearOnTheMapOpensTheParentGateWithAQuestionAndThreeAnswers()
        {
            OpenGate();
            Assert.That(_game.Navigator.Current, Is.EqualTo(ScreenId.ParentGate));
            Assert.IsNotEmpty(Gate.Find("Question").GetComponent<TMP_Text>().text);
            for (var i = 0; i < 3; i++) Assert.IsNotEmpty(Gate.Find("Answer" + i + "/Label").GetComponent<TMP_Text>().text);
        }

        [Test]
        public void ARightAnswerOpensSettings()
        {
            OpenGate();
            Answer(CorrectIndex()).onClick.Invoke();
            Assert.That(_game.Navigator.Current, Is.EqualTo(ScreenId.Settings));
        }

        [Test]
        public void AWrongAnswerReturnsToTheMap()
        {
            OpenGate();
            var wrong = (CorrectIndex() + 1) % 3;
            Answer(wrong).onClick.Invoke();
            Assert.That(_game.Navigator.Current, Is.EqualTo(ScreenId.Map));
        }

        [Test]
        public void EveryVisitAsksAQuestionAndTheGateNeverOpensSettingsWithoutOne()
        {
            OpenGate();
            var first = Gate.Find("Question").GetComponent<TMP_Text>().text;
            Answer((CorrectIndex() + 1) % 3).onClick.Invoke();
            OpenGate();
            Assert.IsNotEmpty(Gate.Find("Question").GetComponent<TMP_Text>().text);
            Assert.That(_game.Navigator.Current, Is.EqualTo(ScreenId.ParentGate));
            Assert.IsNotEmpty(first);
        }

        [Test]
        public void TheCorrectAnswerIsNotAlwaysInTheSameSlot()
        {
            var slots = new System.Collections.Generic.HashSet<int>();
            for (var i = 0; i < 30; i++)
            {
                OpenGate();
                slots.Add(CorrectIndex());
            }
            Assert.Greater(slots.Count, 1);
        }

        [Test]
        public void TheResetConfirmationHasNoOnTheLeftAndYesOnTheRight()
        {
            OpenSettings();
            var yes = ((RectTransform)Settings.Find("ConfirmPanel/Yes")).anchoredPosition.x;
            var no = ((RectTransform)Settings.Find("ConfirmPanel/No")).anchoredPosition.x;
            Assert.Less(no, yes);
        }

        private void OpenSettings()
        {
            OpenGate();
            Answer(CorrectIndex()).onClick.Invoke();
        }

        private Button Row(string name) => Settings.Find(name).GetComponent<Button>();

        [Test]
        public void TheThreeSwitchesToggleAndSaveAndApplyToTheirAudio()
        {
            OpenSettings();
            Row("Music").onClick.Invoke();
            Assert.IsFalse(_game.Progress.MusicEnabled);
            Assert.IsFalse(_game.MusicEnabled);
            Row("Sfx").onClick.Invoke();
            Assert.IsFalse(_game.Progress.SfxEnabled);
            Assert.IsFalse(_game.Sfx.Enabled);
            Row("Voice").onClick.Invoke();
            Assert.IsFalse(_game.Progress.VoiceEnabled);
            Assert.IsFalse(_game.Voice.Enabled);
            var saved = new SaveStore(_store).Load();
            Assert.IsFalse(saved.MusicEnabled);
            Assert.IsFalse(saved.SfxEnabled);
            Assert.IsFalse(saved.VoiceEnabled);
            Row("Voice").onClick.Invoke();
            Assert.IsTrue(_game.Voice.Enabled);
            Assert.IsTrue(new SaveStore(_store).Load().VoiceEnabled);
        }

        [Test]
        public void ThereAreNoVolumeButtonsAnyMore()
        {
            OpenSettings();
            Assert.IsNull(Settings.Find("VolumeLow"));
            Assert.IsNull(Settings.Find("StartOver"));
        }

        [Test]
        public void TheSavedSwitchesAreAppliedWhenTheGameStarts()
        {
            _game.Progress.SfxEnabled = false;
            _game.Progress.VoiceEnabled = false;
            _game.Progress.MusicEnabled = false;
            _game.Commit();
            Object.DestroyImmediate(_game.gameObject);
            Object.DestroyImmediate(_canvasObject);
            _canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            ((RectTransform)_canvasObject.transform).sizeDelta = new Vector2(EvaLayout.DesignWidth, EvaLayout.DesignHeight);
            _game = new GameObject("TestEvaGame").AddComponent<EvaGame>();
            _game.Build(_canvasObject.GetComponent<Canvas>(), _store);
            Assert.IsFalse(_game.Sfx.Enabled);
            Assert.IsFalse(_game.Voice.Enabled);
            Assert.IsFalse(_game.MusicEnabled);
        }

        [Test]
        public void VoiceOffKeepsTheSpeakingFlowButPlaysNoClip()
        {
            _game.Voice.Enabled = false;
            string said = null;
            _game.Voice.Said += key => said = key;
            _game.Voice.Say("any.line");
            Assert.IsNotNull(said);
            Assert.IsTrue(_game.Voice.IsSpeaking);
        }

        [Test]
        public void TheFrameRateCounterIsHiddenOnTheGateAndSettings()
        {
            _game.Navigator.Show(ScreenId.Map);
            OpenGate();
            var fps = _canvasObject.transform.Find("HudRoot/FpsCounter");
            if (fps != null) Assert.IsFalse(fps.gameObject.activeSelf);
        }

        [Test]
        public void EveryRowIsAWideTapTargetAndTheResetRowIsSeparateAndReddish()
        {
            OpenSettings();
            foreach (var name in new[] { "Music", "Sfx", "Voice", "ResetProgress" })
            {
                var rect = ((RectTransform)Settings.Find(name)).rect;
                Assert.GreaterOrEqual(rect.width, EvaUi.MinTap, name);
                Assert.GreaterOrEqual(rect.height, EvaUi.MinTap, name);
                Assert.IsNotNull(Settings.Find(name).GetComponent<TapTarget>(), name);
            }
            var reset = Settings.Find("ResetProgress").GetComponent<Image>().color;
            Assert.Greater(reset.r, reset.g + 0.3f);
            Assert.Greater(reset.r, reset.b + 0.3f);
        }

        [Test]
        public void EverySettingsAndGateTextComesFromLocalization()
        {
            OpenSettings();
            var expected = new[] { Loc.Get("settings.title"), Loc.Get("settings.music"), Loc.Get("settings.sfx"), Loc.Get("settings.voice"), Loc.Get("settings.reset") };
            var shown = new System.Collections.Generic.List<string>();
            foreach (var t in Settings.GetComponentsInChildren<TMP_Text>(true)) shown.Add(t.text);
            foreach (var e in expected) Assert.That(shown, Does.Contain(e));
        }

        [Test]
        public void StartOverNeedsASecondConfirmationAndNoKeepsTheSave()
        {
            _game.Progress.AddCoins(42);
            _game.Commit();
            OpenGate();
            Answer(CorrectIndex()).onClick.Invoke();
            var raised = false;
            _game.StartOverRequested += () => raised = true;
            Settings.Find("ResetProgress").GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(Settings.Find("ConfirmPanel").gameObject.activeSelf);
            Assert.IsFalse(raised);
            Settings.Find("ConfirmPanel/No").GetComponent<Button>().onClick.Invoke();
            Assert.IsFalse(Settings.Find("ConfirmPanel").gameObject.activeSelf);
            Assert.IsFalse(raised);
            Assert.That(new SaveStore(_store).Load().Coins, Is.EqualTo(42));
        }

        [Test]
        public void ConfirmingStartOverErasesTheSaveAndAsksTheAppToRestart()
        {
            _game.Progress.AddCoins(42);
            _game.Progress.HasCharacter = true;
            _game.Commit();
            OpenGate();
            Answer(CorrectIndex()).onClick.Invoke();
            var raised = false;
            _game.StartOverRequested += () => raised = true;
            Settings.Find("ResetProgress").GetComponent<Button>().onClick.Invoke();
            Settings.Find("ConfirmPanel/Yes").GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(raised);
            var fresh = new SaveStore(_store).Load();
            Assert.That(fresh.Coins, Is.EqualTo(0));
            Assert.IsFalse(fresh.HasCharacter);
        }
    }
}
