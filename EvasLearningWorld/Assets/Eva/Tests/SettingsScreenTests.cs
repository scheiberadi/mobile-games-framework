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
        public void TheVolumeButtonsChangeAndSaveTheVoiceVolume()
        {
            OpenGate();
            Answer(CorrectIndex()).onClick.Invoke();
            Settings.Find("VolumeLow").GetComponent<Button>().onClick.Invoke();
            Assert.That(_game.Progress.VoiceVolumeStep, Is.EqualTo(0));
            Assert.That(_game.Voice.Volume, Is.EqualTo(VoiceSettings.Volume(0)).Within(0.0001f));
            Assert.That(new SaveStore(_store).Load().VoiceVolumeStep, Is.EqualTo(0));
            Settings.Find("VolumeMedium").GetComponent<Button>().onClick.Invoke();
            Assert.That(_game.Progress.VoiceVolumeStep, Is.EqualTo(1));
            Settings.Find("VolumeHigh").GetComponent<Button>().onClick.Invoke();
            Assert.That(_game.Progress.VoiceVolumeStep, Is.EqualTo(2));
            Assert.That(_game.Voice.Volume, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void TheSavedVolumeIsAppliedWhenTheGameStarts()
        {
            _game.Progress.VoiceVolumeStep = 0;
            _game.Commit();
            Object.DestroyImmediate(_game.gameObject);
            Object.DestroyImmediate(_canvasObject);
            _canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            ((RectTransform)_canvasObject.transform).sizeDelta = new Vector2(EvaLayout.DesignWidth, EvaLayout.DesignHeight);
            _game = new GameObject("TestEvaGame").AddComponent<EvaGame>();
            _game.Build(_canvasObject.GetComponent<Canvas>(), _store);
            Assert.That(_game.Voice.Volume, Is.EqualTo(VoiceSettings.Volume(0)).Within(0.0001f));
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
            Settings.Find("StartOver").GetComponent<Button>().onClick.Invoke();
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
            Settings.Find("StartOver").GetComponent<Button>().onClick.Invoke();
            Settings.Find("ConfirmPanel/Yes").GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(raised);
            var fresh = new SaveStore(_store).Load();
            Assert.That(fresh.Coins, Is.EqualTo(0));
            Assert.IsFalse(fresh.HasCharacter);
        }
    }
}
