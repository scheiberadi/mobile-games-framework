using System.Collections;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // The first-run screen (Progress.HasCharacter == false): the child picks a face, a skin tone and a top for
    // their own character, then taps the big green check to confirm. The preview on the left updates
    // immediately on every tap, and the choice with a bright ring is always the one currently applied.
    //
    // M5 interim state (docs/superpowers/plans/2026-09-27-m5-character-system.md, Task 1): this screen is
    // deliberately NOT rebuilt yet - that is Task 3, gated behind Gate 1's data-model review and Task 2's
    // style lock, neither of which has happened. This is the same three-row layout as before M5, just
    // repointed at the new CharacterLook fields so the project keeps compiling and the screen keeps working
    // with placeholder art: Gender is fixed to Boy here (no picker yet - Task 3 adds one), the face row still
    // shows only the 4 legacy placeholder head sprites (real 10-per-gender face art doesn't exist yet either),
    // and the third row - what used to be a Shirt colour tint - now picks one of a few placeholder Top item
    // ids; every one of them renders as the same flat coloured box on the rig today (see CharacterRig.
    // ApplyLook) since no real wardrobe art exists, so the swatch colours below are button-icon variety only,
    // not a preview of what actually gets worn.
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

        // Interim-only: how many of CharacterLook.FaceCount this screen's single row offers until Task 3's
        // real per-gender picker exists, and the placeholder Top item ids the third row cycles through (see
        // the class comment above). Button-icon colours only - CharacterRig.ApplyLook renders every one of
        // these the same flat placeholder box regardless of which is picked.
        private const int FaceRowCount = 4;
        // Real v1 boy t-shirt ids (art/character/STYLE.md's approved list - 4 boy t-shirts), not arbitrary
        // placeholders: once Task 3's real art imports under "character/top_boy_<n>", this row (and the
        // rig preview it drives) picks it up with no further code change, the same way Dress the Character
        // already does. Boy-only for now since this screen fixes Gender to Boy until Task 3's rebuild.
        private static readonly string[] TopChoiceIds = { "top_boy_0", "top_boy_1", "top_boy_2", "top_boy_3" };
        private static readonly Color[] TopSwatchPreview =
        {
            new Color(1.00f, 0.35f, 0.35f),
            new Color(1.00f, 0.65f, 0.15f),
            new Color(0.30f, 0.75f, 0.35f),
            new Color(0.25f, 0.55f, 1.00f),
        };

        // The production canvas is height-matched (EvaGame's CanvasScaler uses matchWidthOrHeight = 1 against
        // a 900-tall reference), so unlike width - which only grows on wider devices - the canvas is ALWAYS
        // exactly 900 units tall: y in [-450, 450] is the real on-device frame, not a nominal guide, and every
        // TapTarget's full rect (not just its centre) must stay inside it. FaceRowY sits at the same height as
        // Home instead of below it: Home only occupies x in [-690, -450], so FaceX is shifted clear on the X
        // axis instead (a real, non-overlapping gap, not the 20-unit tolerance), which frees the space a lower
        // face row used to cost and lets every row below fit inside the frame with real margin instead of
        // spilling past y = -450.
        private const float FaceRowY = 300f;
        private const float SkinRowY = 40f;
        private const float TopRowY = -220f;
        private static readonly float[] FaceX = { -280f, -20f, 240f, 500f };
        private static readonly float[] SkinX = { -480f, -240f, 0f, 240f, 480f };
        private static readonly float[] TopX = { -590f, -365f, -140f, 85f, 310f };
        private static readonly Vector2 CheckPosition = new Vector2(580f, -220f);

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _rig;
        private RectTransform _previewRoot;
        private Vector3 _previewBaseScale;
        // Gender is fixed to Boy here - see the class comment on why this screen doesn't pick it yet.
        private readonly CharacterLook _look = new CharacterLook { Gender = Gender.Boy };

        private Image _faceRing, _skinRing, _topRing;
        private bool _facePicked, _colorPicked;

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();
            // Matches the old Shirt=0 default: index 0 is already applied before the first frame, same as
            // every other row here, so the ring/preview/character-if-confirmed-unchanged all agree from the
            // start (see the "default look" comment below).
            _look.SetTop(TopChoiceIds[0]);

            AddBackground(new Color(0.95f, 0.9f, 1f));
            AddPreview();

            _faceRing = AddRing();
            _skinRing = AddRing();
            _topRing = AddRing();

            for (var i = 0; i < FaceRowCount; i++)
            {
                var index = i;
                EvaUi.IconButton(Root, "Face" + i, EvaUi.Sprite("characters/char_head_" + i),
                    new Vector2(0.5f, 0.5f), new Vector2(FaceX[i], FaceRowY), IconSize, () => SelectFace(index));
            }

            for (var i = 0; i < Palette.Skin.Length; i++)
            {
                var index = i;
                var button = EvaUi.IconButton(Root, "Skin" + i, EvaUi.Sprite("icons/dot"),
                    new Vector2(0.5f, 0.5f), new Vector2(SkinX[i], SkinRowY), IconSize, () => SelectSkin(index));
                ((Image)button.targetGraphic).color = Palette.Skin[i];
            }

            for (var i = 0; i < TopChoiceIds.Length; i++)
            {
                var index = i;
                var button = EvaUi.IconButton(Root, "Top" + i, EvaUi.Sprite("icons/dot"),
                    new Vector2(0.5f, 0.5f), new Vector2(TopX[i], TopRowY), IconSize, () => SelectTop(index));
                ((Image)button.targetGraphic).color = TopSwatchPreview[i];
            }

            EvaUi.IconButton(Root, "CheckButton", EvaUi.Sprite("icons/check"), new Vector2(0.5f, 0.5f),
                CheckPosition, CheckSize, Confirm);

            // The default look (index 0 everywhere, no Top worn) is already a valid, saved-ready character, so
            // its choices show a ring and the preview reflects it from the very first frame - no selection is
            // required before the check button works.
            MoveRing(_faceRing, new Vector2(FaceX[0], FaceRowY));
            MoveRing(_skinRing, new Vector2(SkinX[0], SkinRowY));
            MoveRing(_topRing, new Vector2(TopX[0], TopRowY));
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

        private void SelectFace(int index)
        {
            _look.Face = index;
            MoveRing(_faceRing, new Vector2(FaceX[index], FaceRowY));
            ApplyAndHop();
            if (_facePicked) return;
            _facePicked = true;
            _game.Voice.Say("create_color");
        }

        private void SelectSkin(int index)
        {
            _look.Skin = index;
            MoveRing(_skinRing, new Vector2(SkinX[index], SkinRowY));
            ApplyAndHop();
            OnColorPicked();
        }

        private void SelectTop(int index)
        {
            _look.SetTop(TopChoiceIds[index]);
            MoveRing(_topRing, new Vector2(TopX[index], TopRowY));
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
