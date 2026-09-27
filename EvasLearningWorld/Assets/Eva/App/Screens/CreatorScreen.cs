using System.Collections;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // The first-run screen (Progress.HasCharacter == false): the child picks a head, a skin tone and a shirt
    // colour for their own character, then taps the big green check to confirm. The preview on the left updates
    // immediately on every tap, and the choice with a bright ring is always the one currently applied.
    //
    // Layout: three single rows of 240-unit icon buttons (4 heads, 5 skins, 5 shirts) plus a 260-unit check
    // button, positioned to clear the Hud's Home button (always visible here, since Creator != Map) and to
    // stay fully inside the real on-device frame - the production canvas is height-matched, so y in
    // [-450, 450] is not a nominal guide but the actual on-screen bounds every full TapTarget rect must fit
    // inside - without ever exceeding the audit's 20-unit TapTarget overlap allowance. See the row/column
    // constants below, each chosen against the Hud's actual measured rects rather than guessed. The Hud's
    // speech-bubble button is suppressed while this screen is shown (Navigator wires this), which frees the
    // whole bottom-right corner for the check button instead of fighting over it.
    public sealed class CreatorScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private const float IconSize = EvaUi.MinTap; // 240
        private const float RingSize = 290f;
        private const float CheckSize = 260f;
        private const float PreviewHeight = 520f;
        private static readonly Vector2 PreviewPosition = new Vector2(-600f, -450f);

        // The production canvas is height-matched (EvaGame's CanvasScaler uses matchWidthOrHeight = 1 against
        // a 900-tall reference), so unlike width - which only grows on wider devices - the canvas is ALWAYS
        // exactly 900 units tall: y in [-450, 450] is the real on-device frame, not a nominal guide, and every
        // TapTarget's full rect (not just its centre) must stay inside it. HeadRowY sits at the same height as
        // Home instead of below it: Home only occupies x in [-690, -450], so HeadX is shifted clear on the X
        // axis instead (a real, non-overlapping gap, not the 20-unit tolerance), which frees the space a lower
        // head row used to cost and lets every row below fit inside the frame with real margin instead of
        // spilling past y = -450.
        private const float HeadRowY = 300f;
        private const float SkinRowY = 40f;
        private const float ShirtRowY = -220f;
        private static readonly float[] HeadX = { -280f, -20f, 240f, 500f };
        private static readonly float[] SkinX = { -480f, -240f, 0f, 240f, 480f };
        private static readonly float[] ShirtX = { -590f, -365f, -140f, 85f, 310f };
        private static readonly Vector2 CheckPosition = new Vector2(580f, -220f);

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _rig;
        private RectTransform _previewRoot;
        private Vector3 _previewBaseScale;
        private readonly CharacterLook _look = new CharacterLook();

        private Image _headRing, _skinRing, _shirtRing;
        private bool _headPicked, _colorPicked;

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddBackground(new Color(0.95f, 0.9f, 1f));
            AddPreview();

            _headRing = AddRing();
            _skinRing = AddRing();
            _shirtRing = AddRing();

            for (var i = 0; i < CharacterLook.HeadCount; i++)
            {
                var index = i;
                EvaUi.IconButton(Root, "Head" + i, EvaUi.Sprite("characters/char_head_" + i),
                    new Vector2(0.5f, 0.5f), new Vector2(HeadX[i], HeadRowY), IconSize, () => SelectHead(index));
            }

            for (var i = 0; i < Palette.Skin.Length; i++)
            {
                var index = i;
                var button = EvaUi.IconButton(Root, "Skin" + i, EvaUi.Sprite("icons/dot"),
                    new Vector2(0.5f, 0.5f), new Vector2(SkinX[i], SkinRowY), IconSize, () => SelectSkin(index));
                ((Image)button.targetGraphic).color = Palette.Skin[i];
            }

            for (var i = 0; i < Palette.Shirt.Length; i++)
            {
                var index = i;
                var button = EvaUi.IconButton(Root, "Shirt" + i, EvaUi.Sprite("icons/dot"),
                    new Vector2(0.5f, 0.5f), new Vector2(ShirtX[i], ShirtRowY), IconSize, () => SelectShirt(index));
                ((Image)button.targetGraphic).color = Palette.Shirt[i];
            }

            EvaUi.IconButton(Root, "CheckButton", EvaUi.Sprite("icons/check"), new Vector2(0.5f, 0.5f),
                CheckPosition, CheckSize, Confirm);

            // The default look (index 0 everywhere) is already a valid, saved-ready character, so its choices
            // show a ring and the preview reflects it from the very first frame - no selection is required
            // before the check button works.
            MoveRing(_headRing, new Vector2(HeadX[0], HeadRowY));
            MoveRing(_skinRing, new Vector2(SkinX[0], SkinRowY));
            MoveRing(_shirtRing, new Vector2(ShirtX[0], ShirtRowY));
        }

        public override void OnShow() => _game.Voice.Say("create_head");

        private void AddPreview()
        {
            _rig = RigFactory.CreatePlayer(Root, _look, PreviewHeight);
            _previewRoot = _rig.Root;
            _previewRoot.anchorMin = _previewRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _previewRoot.anchoredPosition = PreviewPosition;
            _previewBaseScale = _previewRoot.localScale;
        }

        // A ring is a decoration, never tapped: no Button, no TapTarget, so it never counts against the
        // audit's overlap rule or the "nothing interactive without a TapTarget" rule. Build() creates all
        // three rings before any of the icon buttons, so plain child order already draws them underneath
        // every button (do not SetAsFirstSibling here: that would put a ring behind AddBackground's panel
        // instead, since that panel also calls SetAsFirstSibling, hiding the ring completely).
        private Image AddRing()
        {
            var go = new GameObject("Ring", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(Root, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(RingSize, RingSize);
            var image = go.GetComponent<Image>();
            image.sprite = EvaUi.Sprite("icons/ring");
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static void MoveRing(Image ring, Vector2 position)
        {
            ((RectTransform)ring.transform).anchoredPosition = position;
        }

        private void SelectHead(int index)
        {
            _look.Head = index;
            MoveRing(_headRing, new Vector2(HeadX[index], HeadRowY));
            ApplyAndHop();
            if (_headPicked) return;
            _headPicked = true;
            _game.Voice.Say("create_color");
        }

        private void SelectSkin(int index)
        {
            _look.Skin = index;
            MoveRing(_skinRing, new Vector2(SkinX[index], SkinRowY));
            ApplyAndHop();
            OnColorPicked();
        }

        private void SelectShirt(int index)
        {
            _look.Shirt = index;
            MoveRing(_shirtRing, new Vector2(ShirtX[index], ShirtRowY));
            ApplyAndHop();
            OnColorPicked();
        }

        private void OnColorPicked()
        {
            if (_colorPicked) return;
            _colorPicked = true;
            _game.Voice.Say("create_done");
        }

        private void ApplyAndHop()
        {
            _rig.ApplyLook(_look);
            _runner.StartCoroutine(Hop(_previewRoot, _previewBaseScale));
        }

        private void Confirm()
        {
            _game.Progress.HasCharacter = true;
            _game.Progress.Look = _look;
            _game.Progress.Advance(TutorialEvent.LookConfirmed);
            _game.Commit();
            _game.Navigator.Show(ScreenId.Map);
        }

        // A small scale bounce (Cheer is an animator clip meant for Eva's own excitement, not a selection
        // acknowledgement here), scaled relative to the rig's own baseline so it never disturbs PreviewHeight.
        private static IEnumerator Hop(RectTransform target, Vector3 baseScale)
        {
            const float peak = 1.15f, duration = 0.22f, half = duration * 0.5f;
            for (var t = 0f; t < half; t += Time.deltaTime)
            {
                target.localScale = baseScale * Mathf.Lerp(1f, peak, t / half);
                yield return null;
            }
            for (var t = 0f; t < half; t += Time.deltaTime)
            {
                target.localScale = baseScale * Mathf.Lerp(peak, 1f, t / half);
                yield return null;
            }
            target.localScale = baseScale;
        }
    }
}
