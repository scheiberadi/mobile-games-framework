using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Runtime handle to a rig built by RigFactory. The player is an Animator-driven humanoid whose parts ApplyLook
    // tints; Eva is a code-driven cat (CatMotion) with no Animator.
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

        private readonly CatMotion _cat;
        private readonly Image _torso, _armL, _armR, _head, _legL, _legR;

        internal CharacterRig(Animator animator, RectTransform root, Image torso, Image armL, Image armR,
            Image head, Image legL, Image legR)
        {
            Animator = animator;
            Root = root;
            _torso = torso;
            _armL = armL;
            _armR = armR;
            _head = head;
            _legL = legL;
            _legR = legR;
        }

        internal CharacterRig(CatMotion cat, RectTransform root)
        {
            _cat = cat;
            Root = root;
        }

        // Player only: swaps the head sprite to look.Head and tints the torso with Shirt and everything
        // else (arms, legs, head) with Skin. A no-op on Eva's rig, whose cat art is never tinted.
        public void ApplyLook(CharacterLook look)
        {
            if (_cat != null) return;

            var headIndex = Mathf.Clamp(look.Head, 0, CharacterLook.HeadCount - 1);
            _head.sprite = EvaUi.Sprite("characters/char_head_" + headIndex);

            var shirt = Palette.Shirt[Mathf.Clamp(look.Shirt, 0, Palette.Shirt.Length - 1)];
            var skin = Palette.Skin[Mathf.Clamp(look.Skin, 0, Palette.Skin.Length - 1)];
            _torso.color = shirt;
            _armL.color = skin;
            _armR.color = skin;
            _head.color = skin;
            _legL.color = skin;
            _legR.color = skin;
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
