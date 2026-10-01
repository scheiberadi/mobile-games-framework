using System.Collections;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // The first-run screen (Progress.HasCharacter == false): the child builds their own character, then taps the
    // big green check. Rebuilt for M5 Task 3 (docs/superpowers/plans/2026-09-27-m5-character-system.md) for a
    // 4-5-year-old who cannot read: no text anywhere, the live preview does most of the communicating.
    //
    // Shape: a paged category rail instead of one long list. The top row is [previous arrow] [current category's
    // picture] [next arrow] [randomize dice]; below it a small grid of big choice buttons for that one category
    // (never more than CharacterCreator.MaxOptions); the green check sits bottom right. Paging steps through
    // CharacterCreator.CategoriesFor(gender) - gender first, so the very first choice is boy or girl - and the
    // Dress step only exists for girls. All the rules (counts, selection, Dress/Top/Bottom exclusivity,
    // randomize validity) live in Rules/CharacterCreator.cs and are unit-tested there; this class only draws them.
    //
    // Art is still placeholder (hash-coloured boxes from EvaUi.Sprite) for everything Task 3's batch has not
    // produced yet: each choice button asks for the sprite key the real art will land under, so importing it
    // needs no code change. Faces still use the 4 legacy head sprites for now.
    //
    // Layout (production canvas is height-matched, so y in [-450, 450] is the real on-device frame, and every
    // full TapTarget rect must stay inside it): 4 columns x 3 rows of 240-unit buttons on a 260/240 pitch, so no
    // two tap targets overlap at all. Column 0 sits clear of the Hud's Home button (x in [-690, -450]) and the
    // preview occupies the left band underneath it. Row 0 holds the rail, rows 1-2 the choices (<= 6, so row 2
    // only ever fills columns 0-1), and the check button takes row 2's right half - space no choice can use.
    public sealed class CreatorScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private const float ButtonSize = EvaUi.MinTap; // 240
        private const float RingSize = 280f;
        private const float CheckSize = 260f;
        private const float PreviewHeight = 520f;
        private static readonly Vector2 PreviewPosition = new Vector2(-600f, -450f);

        private static readonly float[] ColumnX = { -230f, 30f, 290f, 550f };
        private const float RailY = 330f;
        private static readonly float[] ChoiceRowY = { 90f, -150f };
        private static readonly Vector2 CheckPosition = new Vector2(550f, -170f);

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _rig;
        private JoyReactions _joy;
        private readonly CharacterLook _look = CharacterCreator.DefaultLook(Gender.Boy);
        private readonly System.Random _rng = new System.Random();

        private int _step; // index into CharacterCreator.CategoriesFor(_look.Gender)
        private Image _categoryIcon;
        private Image _ring;
        private readonly Button[] _choices = new Button[CharacterCreator.MaxOptions];
        private bool _firstPickMade;

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddBackground(new Color(0.95f, 0.9f, 1f));
            AddPreview();
            _joy = new JoyReactions(_runner, _rig);

            // The ring is created before every button so plain child order draws it underneath them (do not
            // SetAsFirstSibling: that would put it behind AddBackground's panel, which also calls it).
            _ring = AddRing();

            // icons/arrow points right at rotation 0 (House's Nav uses 90 for up, 270 for down), so Previous is the
            // same icon turned half a circle.
            var previous = EvaUi.IconButton(Root, "PrevCategory", EvaUi.Sprite("icons/arrow"), new Vector2(0.5f, 0.5f),
                new Vector2(ColumnX[0], RailY), ButtonSize, () => Page(-1));
            previous.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            _categoryIcon = AddCategoryIcon();
            EvaUi.IconButton(Root, "NextCategory", EvaUi.Sprite("icons/arrow"), new Vector2(0.5f, 0.5f),
                new Vector2(ColumnX[2], RailY), ButtonSize, () => Page(1));
            // icons/dice has no art yet (placeholder box until a dice/shuffle icon is generated and imported).
            EvaUi.IconButton(Root, "Randomize", EvaUi.Sprite("icons/dice"), new Vector2(0.5f, 0.5f),
                new Vector2(ColumnX[3], RailY), ButtonSize, Randomize);

            for (var i = 0; i < _choices.Length; i++)
            {
                var index = i;
                _choices[i] = EvaUi.IconButton(Root, "Choice" + i, EvaUi.Sprite("icons/dot"), new Vector2(0.5f, 0.5f),
                    ChoicePosition(i), ButtonSize, () => Choose(index));
            }

            EvaUi.IconButton(Root, "CheckButton", EvaUi.Sprite("icons/check"), new Vector2(0.5f, 0.5f),
                CheckPosition, CheckSize, Confirm);

            // The default look is already a valid, confirm-ready character; the rail starts on Gender.
            ShowStep(0);
        }

        public override void OnShow() => _game.Voice.Say("create_head");

        private static Vector2 ChoicePosition(int slot) => new Vector2(ColumnX[slot % 4], ChoiceRowY[slot / 4]);

        private CreatorCategory[] Categories => CharacterCreator.CategoriesFor(_look.Gender);
        private CreatorCategory Category => Categories[_step];

        private void AddPreview()
        {
            _rig = RigFactory.CreatePlayer(Root, _look, PreviewHeight);
            var root = _rig.Root;
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = PreviewPosition;
        }

        // A non-interactive picture of the category being edited (no Button/TapTarget, so it is exempt from the
        // overlap audit like the ring): the same icon the category's first choice shows.
        private Image AddCategoryIcon()
        {
            var go = new GameObject("CategoryIcon", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(Root, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(ColumnX[1], RailY);
            rect.sizeDelta = new Vector2(ButtonSize, ButtonSize);
            var image = go.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        // A ring is a decoration, never tapped: no Button, no TapTarget.
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

        // Wraps around at both ends, so a child can keep tapping one arrow and see everything.
        private void Page(int direction)
        {
            var count = Categories.Length;
            ShowStep((_step + direction + count) % count);
        }

        private void ShowStep(int step)
        {
            _step = step;
            var category = Category;
            var gender = _look.Gender;
            var count = CharacterCreator.OptionCount(category, gender);

            SetIcon(_categoryIcon, category, gender, 0);
            for (var i = 0; i < _choices.Length; i++)
            {
                var button = _choices[i];
                button.gameObject.SetActive(i < count);
                if (i < count) SetIcon((Image)button.targetGraphic, category, gender, i);
            }
            RefreshRing();
        }

        private void RefreshRing()
        {
            var selected = CharacterCreator.SelectedIndex(_look, Category);
            var shown = selected >= 0 && selected < CharacterCreator.OptionCount(Category, _look.Gender);
            _ring.gameObject.SetActive(shown);
            if (shown) ((RectTransform)_ring.transform).anchoredPosition = ChoicePosition(selected);
        }

        // The picture for one choice: wardrobe and hair pieces ask for the sprite key the real art lands under
        // (placeholder box until then), colour categories are tinted dots, Glasses' trailing "none" is a cross.
        private void SetIcon(Image image, CreatorCategory category, Gender gender, int index)
        {
            var prefix = gender == Gender.Boy ? "boy" : "girl";
            image.color = Color.white;
            switch (category)
            {
                case CreatorCategory.Gender:
                    image.sprite = EvaUi.Sprite("character/top_" + (index == 0 ? "boy" : "girl") + "_0");
                    break;
                case CreatorCategory.Skin: Dot(image, Palette.Skin[index]); break;
                case CreatorCategory.HairColor: Dot(image, Palette.HairColor[index]); break;
                case CreatorCategory.EyeColor: Dot(image, Palette.EyeColor[index]); break;
                case CreatorCategory.Face:
                    image.sprite = EvaUi.Sprite("character/face_" + prefix + "_" + index);
                    break;
                case CreatorCategory.HairStyle:
                    image.sprite = EvaUi.Sprite("character/hair" + prefix + "_" + index + "_front");
                    break;
                default:
                    var id = CharacterCreator.ItemIdAt(category, gender, index);
                    image.sprite = id == null ? EvaUi.Sprite("icons/cross") : EvaUi.Sprite("character/" + id);
                    break;
            }
        }

        private static void Dot(Image image, Color color)
        {
            image.sprite = EvaUi.Sprite("icons/dot");
            image.color = color;
        }

        private void Choose(int index)
        {
            var wasGender = Category == CreatorCategory.Gender;
            CharacterCreator.Select(_look, Category, index);
            _rig.ApplyLook(_look);
            _joy.Pleased();
            // A gender change swaps the category list (Dress appears or disappears) and every gendered icon;
            // the step stays on Gender (index 0), so just redraw it.
            if (wasGender) ShowStep(_step); else RefreshRing();
            OnFirstPick();
        }

        // Rolls one pick per category (see CharacterCreator.Randomize) and fires the big reaction. Gender is kept.
        private void Randomize()
        {
            CharacterCreator.Randomize(_look, n => _rng.Next(0, n));
            _rig.ApplyLook(_look);
            _joy.Big();
            ShowStep(_step);
            OnFirstPick();
        }

        private void OnFirstPick()
        {
            if (_firstPickMade) return;
            _firstPickMade = true;
            _game.Voice.Say("create_done");
        }

        private void Confirm()
        {
            _game.Progress.HasCharacter = true;
            _game.Progress.Look = _look;
            _game.Progress.Advance(TutorialEvent.LookConfirmed);
            _game.Commit();
            _game.Navigator.Show(ScreenId.Map);
        }
    }
}
