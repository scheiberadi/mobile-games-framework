using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Runtime handle to a rig built by RigFactory. The player is an Animator-driven humanoid whose parts ApplyLook
    // tints/toggles; Eva is a code-driven cat (CatMotion) with no Animator.
    public sealed class CharacterRig
    {
        private const string TalkingParam = "Talking";
        private const string WaveTrigger = "Wave";
        private const string CheerTrigger = "Cheer";
        private const string WaveStateName = "Wave";
        private const string CheerStateName = "Cheer";

        // Crude placeholder tints for the still-artless wardrobe slots (M5 Task 1 - see the M5 plan/spec).
        // Real wardrobe art (Task 2/3) replaces these with actual illustrated items; until then every worn
        // item in a slot renders as the same flat box, so these exist only to make "something is worn here"
        // visually legible, never to preview a specific item's real colour.
        private static readonly Color TopPlaceholderColor = new Color(0.35f, 0.55f, 0.85f);
        private static readonly Color BottomPlaceholderColor = new Color(0.30f, 0.35f, 0.55f);
        private static readonly Color DressPlaceholderColor = new Color(0.75f, 0.35f, 0.65f);
        private static readonly Color ShoesPlaceholderColor = new Color(0.40f, 0.28f, 0.18f);
        private static readonly Color GlassesPlaceholderColor = new Color(0.20f, 0.20f, 0.22f);

        public RectTransform Root { get; }
        // Null for Eva (the cat is driven by CatMotion).
        public Animator Animator { get; }

        // Every part RigFactory.Build() lays out for the player, handed to CharacterRig as one group so its
        // own constructor doesn't grow a parameter per M5 slot. Internal: only RigFactory constructs one.
        internal struct PlayerParts
        {
            public Image Torso, ArmL, ArmR, Face, LegL, LegR;
            public Image HairBack, HairFront, EyeIris, Glasses;
            public Image Top, Bottom, Dress, ShoeL, ShoeR;
        }

        private readonly CatMotion _cat;
        private readonly Image _torso, _armL, _armR, _face, _legL, _legR;
        private readonly Image _hairBack, _hairFront, _eyeIris, _glasses;
        private readonly Image _top, _bottom, _dress, _shoeL, _shoeR;

        internal CharacterRig(Animator animator, RectTransform root, PlayerParts parts)
        {
            Animator = animator;
            Root = root;
            _torso = parts.Torso; _armL = parts.ArmL; _armR = parts.ArmR; _face = parts.Face;
            _legL = parts.LegL; _legR = parts.LegR;
            _hairBack = parts.HairBack; _hairFront = parts.HairFront; _eyeIris = parts.EyeIris; _glasses = parts.Glasses;
            _top = parts.Top; _bottom = parts.Bottom; _dress = parts.Dress; _shoeL = parts.ShoeL; _shoeR = parts.ShoeR;
        }

        internal CharacterRig(CatMotion cat, RectTransform root)
        {
            _cat = cat;
            Root = root;
        }

        // Player only: a no-op on Eva's rig, whose cat art is never tinted or dressed.
        //
        // Face/limbs are tinted by Skin, same as before M5. Hair (back + front pieces) is tinted by HairColor;
        // HairFront's own height is resized per HairStyle's front-coverage (RigFactory.HairShapeFor) so
        // different styles genuinely cover a different amount of forehead, not just a fixed strip. EyeIris is
        // tinted by EyeColor. Every wardrobe slot (Top/Bottom/Dress/Shoes/Glasses) is null-checked and simply
        // shown or hidden - Dress, when set, is shown INSTEAD of Top/Bottom (never alongside them: this is
        // CharacterLook's own exclusivity rule, see CharacterLook.Normalize, applied again here defensively so
        // a look assembled without going through the setters still renders correctly).
        public void ApplyLook(CharacterLook look)
        {
            if (_cat != null) return;

            var skin = Palette.Skin[Mathf.Clamp(look.Skin, 0, Palette.Skin.Length - 1)];
            _face.sprite = EvaUi.Sprite("characters/char_head_" + RigFactory.FaceSpriteIndex(look.Face));
            _face.color = skin;
            _torso.color = skin;
            _armL.color = skin;
            _armR.color = skin;
            _legL.color = skin;
            _legR.color = skin;

            var hairColor = Palette.HairColor[Mathf.Clamp(look.HairColor, 0, Palette.HairColor.Length - 1)];
            _hairBack.color = hairColor;
            _hairFront.color = hairColor;
            var frontCoverage = RigFactory.HairShapeFor(look.HairStyle).FrontCoverage;
            var hairFrontRect = _hairFront.rectTransform;
            hairFrontRect.anchorMin = new Vector2(hairFrontRect.anchorMin.x, 1f - frontCoverage);

            _eyeIris.color = Palette.EyeColor[Mathf.Clamp(look.EyeColor, 0, Palette.EyeColor.Length - 1)];

            var dressWorn = look.Dress != null;
            SetWorn(_dress, dressWorn, DressPlaceholderColor);
            SetWorn(_top, !dressWorn && look.Top != null, TopPlaceholderColor);
            SetWorn(_bottom, !dressWorn && look.Bottom != null, BottomPlaceholderColor);
            SetWorn(_glasses, look.Glasses != null, GlassesPlaceholderColor);
            SetWorn(_shoeL, look.Shoes != null, ShoesPlaceholderColor);
            SetWorn(_shoeR, look.Shoes != null, ShoesPlaceholderColor);
        }

        private static void SetWorn(Image image, bool worn, Color placeholderColor)
        {
            image.gameObject.SetActive(worn);
            if (worn) image.color = placeholderColor;
        }

        // Ignored while Wave is already playing or the Animator is blending into or out of a state, so
        // mashing the tap cannot queue a repeat.
        public void Wave()
        {
            if (_cat != null) { _cat.Greet(); return; }
            if (IsBusy(WaveStateName)) return;
            Animator.SetTrigger(WaveTrigger);
        }

        public void Cheer()
        {
            if (_cat != null) { _cat.Cheer(); return; }
            if (IsBusy(CheerStateName)) return;
            Animator.SetTrigger(CheerTrigger);
        }

        public void SetTalking(bool talking)
        {
            if (_cat != null) _cat.SetTalking(talking);
            else Animator.SetBool(TalkingParam, talking);
        }

        // Eva only: the wrong-answer reaction (tail lash, ears back, eyes narrowed); a no-op on the player.
        public void Angry() => _cat?.Angry();

        private bool IsBusy(string stateName) =>
            Animator.IsInTransition(0) || Animator.GetCurrentAnimatorStateInfo(0).IsName(stateName);
    }
}
