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

            var gender = look.Gender == Gender.Boy ? "boy" : "girl";
            var skin = Palette.Skin[Mathf.Clamp(look.Skin, 0, Palette.Skin.Length - 1)];
            var face = Mathf.Clamp(look.Face, 0, CharacterCreator.FacesPerGender - 1);
            _face.sprite = EvaUi.Sprite("character/face_" + gender + "_" + face);
            _face.color = skin;
            _torso.color = skin;
            _armL.color = skin;
            _armR.color = skin;
            _legL.color = skin;
            _legR.color = skin;

            // Face, hair and glasses share one pixel scale (RigFactory.ArtPixelsPerHead), so each is sized from its own sprite.
            var faceSize = HeadArtSize(_face.sprite);
            _face.rectTransform.sizeDelta = faceSize;
            _face.rectTransform.pivot = new Vector2(0.5f, NeckOverlap / faceSize.y); // chin sits a little below the torso top, no gap
            faceSize.y -= NeckOverlap;

            var styleCount = look.Gender == Gender.Boy ? RigFactory.HairStylesBoy : RigFactory.HairStylesGirl;
            var style = Mathf.Clamp(look.HairStyle, 0, styleCount - 1);
            var hairColor = Palette.HairColor[Mathf.Clamp(look.HairColor, 0, Palette.HairColor.Length - 1)];
            _hairBack.sprite = EvaUi.Sprite("character/hair" + gender + "_" + style + "_back");
            _hairFront.sprite = EvaUi.Sprite("character/hair" + gender + "_" + style + "_front");
            _hairBack.color = hairColor;
            _hairFront.color = hairColor;
            // Back hair: top a little above the head, hangs as far down as its art does. Front fringe: top of the head, centred.
            var backSize = HeadArtSize(_hairBack.sprite);
            var backRect = _hairBack.rectTransform;
            backRect.sizeDelta = backSize;
            backRect.anchoredPosition = new Vector2(0f, faceSize.y + RigFactory.HeadSize * 0.04f - backSize.y);
            var frontSize = HeadArtSize(_hairFront.sprite);
            var frontRect = _hairFront.rectTransform;
            frontRect.anchorMin = frontRect.anchorMax = new Vector2(0.5f, 1f);
            frontRect.pivot = new Vector2(0.5f, 1f);
            frontRect.sizeDelta = frontSize;
            frontRect.anchoredPosition = new Vector2(0f, RigFactory.HeadSize * 0.02f);

            // The generated faces have closed, unfilled eyes, so there is no iris to tint; the colour is still stored.
            _eyeIris.color = Palette.EyeColor[Mathf.Clamp(look.EyeColor, 0, Palette.EyeColor.Length - 1)];
            _eyeIris.enabled = false;

            var dressWorn = look.Dress != null;
            SetWorn(_dress, dressWorn, look.Dress);
            SetWorn(_top, !dressWorn && look.Top != null, look.Top);
            SetWorn(_bottom, !dressWorn && look.Bottom != null, look.Bottom);
            SetWorn(_glasses, look.Glasses != null, look.Glasses);
            SetWorn(_shoeL, look.Shoes != null, look.Shoes);
            SetWorn(_shoeR, look.Shoes != null, look.Shoes);
            if (look.Glasses != null)
            {
                var glassesRect = _glasses.rectTransform;
                glassesRect.sizeDelta = HeadArtSize(_glasses.sprite);
                glassesRect.anchoredPosition = new Vector2(0f, faceSize.y * 0.40f);
            }
        }

        private const float NeckOverlap = 4f;

        private static Vector2 HeadArtSize(Sprite sprite) => sprite.rect.size * (RigFactory.HeadSize / RigFactory.ArtPixelsPerHead);

        private static void SetWorn(Image image, bool worn, string itemId)
        {
            image.gameObject.SetActive(worn);
            if (!worn) return;
            image.sprite = EvaUi.Sprite("character/" + itemId);
            image.color = Color.white;
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
