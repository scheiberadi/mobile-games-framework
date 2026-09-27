using System.IO;
using EvasLearningWorld.App;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Builds the Animator Controller and clips shared by Eva and the player character, generated from code so
// they can be regenerated instead of hand-authored as data files. Run once (or whenever the layout in
// RigFactory changes):
// Unity.exe -batchmode -quit -projectPath <eva> -executeMethod EvaRigAssets.Generate
//
// The clips are authored in absolute values against RigFactory's canonical (unscaled) body layout, because
// RigFactory scales the whole rig with Root.localScale rather than resizing individual parts: this lets the
// same Idle/Talk/Wave/Cheer clips, and the same Rig.controller, drive both Eva (560 units tall on screen)
// and the player (420 or 520 units) without any per-height authoring.
public static class EvaRigAssets
{
    private const string Dir = "Assets/Eva/Resources/Anim";
    private const string ControllerPath = Dir + "/Rig.controller";

    // The right arm's rest rotation, matching RigFactory (new parts start at localEulerAngles == 0), so
    // Wave's first and last keys equal the arm's rest angle in the hierarchy, per the brief.
    private const float RestArmAngle = 0f;
    private const float RestTorsoY = RigFactory.LegLength;

    private const string TorsoPath = "Torso";
    private const string HeadPath = "Torso/Head";
    private const string ArmLPath = "Torso/ArmL";
    private const string ArmRPath = "Torso/ArmR";

    public static void Generate()
    {
        Directory.CreateDirectory(Dir);

        var idle = MakeClip("Idle", 2f, true, clip =>
        {
            SetCurve(clip, TorsoPath, "m_AnchoredPosition.y", Keys((0f, RestTorsoY), (1f, RestTorsoY + 6f), (2f, RestTorsoY)));
            SetCurve(clip, HeadPath, "localEulerAnglesRaw.z", Keys((0f, -2f), (1f, 2f), (2f, -2f)));
        });

        var talk = MakeClip("Talk", 0.5f, true, clip =>
        {
            SetCurve(clip, TorsoPath, "m_AnchoredPosition.y", Keys((0f, RestTorsoY), (0.25f, RestTorsoY + 4f), (0.5f, RestTorsoY)));
            SetCurve(clip, HeadPath, "localEulerAnglesRaw.z", Keys((0f, 0f), (0.25f, 3f), (0.5f, 0f)));
        });

        var wave = MakeClip("Wave", 0.9f, false, clip =>
        {
            // Raise (0-0.15s), three swings between 120 and 150 degrees at 0.15s each way, then return to rest.
            SetCurve(clip, ArmRPath, "localEulerAnglesRaw.z", Keys(
                (0f, RestArmAngle), (0.15f, 150f), (0.30f, 120f), (0.45f, 150f), (0.60f, 120f), (0.75f, 150f), (0.90f, RestArmAngle)));
        });

        var cheer = MakeClip("Cheer", 1.0f, false, clip =>
        {
            SetCurve(clip, ArmLPath, "localEulerAnglesRaw.z", Keys((0f, 0f), (0.3f, -140f), (0.6f, -140f), (1.0f, 0f)));
            SetCurve(clip, ArmRPath, "localEulerAnglesRaw.z", Keys((0f, 0f), (0.3f, 140f), (0.6f, 140f), (1.0f, 0f)));
            SetCurve(clip, TorsoPath, "m_AnchoredPosition.y", Keys((0f, RestTorsoY), (0.3f, RestTorsoY + 14f), (0.6f, RestTorsoY), (1.0f, RestTorsoY)));
        });

        BuildController(idle, talk, wave, cheer);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("EVA_RIG_ASSETS_GENERATED " + ControllerPath);
    }

    private static void BuildController(AnimationClip idle, AnimationClip talk, AnimationClip wave, AnimationClip cheer)
    {
        AssetDatabase.DeleteAsset(ControllerPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Talking", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Wave", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Cheer", AnimatorControllerParameterType.Trigger);

        var machine = controller.layers[0].stateMachine;
        var idleState = machine.AddState("Idle");
        idleState.motion = idle;
        machine.defaultState = idleState;

        var talkState = machine.AddState("Talk");
        talkState.motion = talk;
        var waveState = machine.AddState("Wave");
        waveState.motion = wave;
        var cheerState = machine.AddState("Cheer");
        cheerState.motion = cheer;

        var toTalk = idleState.AddTransition(talkState);
        toTalk.hasExitTime = false;
        toTalk.duration = 0.1f;
        toTalk.AddCondition(AnimatorConditionMode.If, 0f, "Talking");

        var toIdleFromTalk = talkState.AddTransition(idleState);
        toIdleFromTalk.hasExitTime = false;
        toIdleFromTalk.duration = 0.1f;
        toIdleFromTalk.AddCondition(AnimatorConditionMode.IfNot, 0f, "Talking");

        var toWave = machine.AddAnyStateTransition(waveState);
        toWave.hasExitTime = false;
        toWave.duration = 0.1f;
        toWave.canTransitionToSelf = false;
        toWave.AddCondition(AnimatorConditionMode.If, 0f, "Wave");

        var waveToIdle = waveState.AddTransition(idleState);
        waveToIdle.hasExitTime = true;
        waveToIdle.exitTime = 1f;
        waveToIdle.duration = 0.1f;

        var toCheer = machine.AddAnyStateTransition(cheerState);
        toCheer.hasExitTime = false;
        toCheer.duration = 0.1f;
        toCheer.canTransitionToSelf = false;
        toCheer.AddCondition(AnimatorConditionMode.If, 0f, "Cheer");

        var cheerToIdle = cheerState.AddTransition(idleState);
        cheerToIdle.hasExitTime = true;
        cheerToIdle.exitTime = 1f;
        cheerToIdle.duration = 0.1f;
    }

    private static AnimationClip MakeClip(string name, float length, bool loop, System.Action<AnimationClip> author)
    {
        var clip = new AnimationClip { name = name, frameRate = 60f };
        author(clip);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        var path = Dir + "/" + name + ".anim";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static void SetCurve(AnimationClip clip, string path, string property, AnimationCurve curve)
    {
        for (var i = 0; i < curve.length; i++) curve.SmoothTangents(i, 0f);
        clip.SetCurve(path, typeof(RectTransform), property, curve);
    }

    private static AnimationCurve Keys(params (float time, float value)[] points)
    {
        var keyframes = new Keyframe[points.Length];
        for (var i = 0; i < points.Length; i++) keyframes[i] = new Keyframe(points[i].time, points[i].value);
        return new AnimationCurve(keyframes);
    }
}
