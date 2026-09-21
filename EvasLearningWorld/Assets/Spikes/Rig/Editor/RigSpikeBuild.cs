using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

// Disposable spike builder: generates placeholder sprites, the rig hierarchy, two clips written with
// AnimationClip.SetCurve, the AnimatorController, and the scene.
// Unity.exe -batchmode -quit -projectPath <eva> -executeMethod RigSpikeBuild.CreateScene
public static class RigSpikeBuild
{
    private const string Dir = "Assets/Spikes/Rig";
    private const string SpriteDir = Dir + "/Sprites";
    private const string ScenePath = Dir + "/RigSpike.unity";

    private static readonly Color Ink = new Color(0.15f, 0.12f, 0.2f);

    public static void CreateScene()
    {
        Directory.CreateDirectory(SpriteDir);
        var torsoSprite = MakeSprite("torso", 200, 260, 40, new Color(0.25f, 0.55f, 0.95f), Face.None, new Vector2(0.5f, 0.5f));
        var armSprite = MakeSprite("arm", 60, 200, 30, new Color(0.95f, 0.65f, 0.35f), Face.None, new Vector2(0.5f, 1f));
        var legSprite = MakeSprite("leg", 70, 220, 30, new Color(0.35f, 0.35f, 0.7f), Face.None, new Vector2(0.5f, 1f));
        var heads = new[]
        {
            MakeSprite("head1", 200, 200, 100, new Color(1f, 0.85f, 0.4f), Face.Smile, new Vector2(0.5f, 0.5f)),
            MakeSprite("head2", 200, 200, 60, new Color(1f, 0.6f, 0.7f), Face.Open, new Vector2(0.5f, 0.5f)),
            MakeSprite("head3", 200, 200, 100, new Color(0.55f, 0.85f, 0.55f), Face.Flat, new Vector2(0.5f, 0.5f))
        };

        EvaSpikeScenes.Create(ScenePath, typeof(RigSpike));
        var scene = EditorSceneManager.GetActiveScene();
        var spike = Object.FindFirstObjectByType<RigSpike>();

        var camera = Camera.main;
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.transform.position = new Vector3(0f, 0f, -10f);

        // Rig: root(Animator) > Torso > Head, ArmL, ArmR ; root > LegL, LegR. Limbs sort behind the torso, head in front.
        var root = new GameObject("Rig");
        root.transform.position = new Vector3(-2.5f, -0.4f, 0f);
        var animator = root.AddComponent<Animator>();

        var torso = Part("Torso", root.transform, torsoSprite, new Vector3(0f, 0f, 0f), 10);
        var head = Part("Head", torso.transform, heads[0], new Vector3(0f, 2.2f, 0f), 30);
        var armL = Part("ArmL", torso.transform, armSprite, new Vector3(-1.15f, 1.05f, 0f), 5);
        var armR = Part("ArmR", torso.transform, armSprite, new Vector3(1.15f, 1.05f, 0f), 5);
        armL.transform.localEulerAngles = new Vector3(0f, 0f, -8f);
        armR.transform.localEulerAngles = new Vector3(0f, 0f, 8f);
        Part("LegL", root.transform, legSprite, new Vector3(-0.5f, -1.25f, 0f), 0);
        Part("LegR", root.transform, legSprite, new Vector3(0.5f, -1.25f, 0f), 0);

        var idle = MakeIdleClip();
        var wave = MakeWaveClip();
        animator.runtimeAnimatorController = MakeController(idle, wave);

        spike.animator = animator;
        spike.head = head;
        spike.headSprites = heads;
        spike.torso = torso.transform;
        spike.armRight = armR.transform;
        EditorUtility.SetDirty(spike);

        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("RIG_SCENE_CREATED " + ScenePath);
    }

    private static SpriteRenderer Part(string name, Transform parent, Sprite sprite, Vector3 localPosition, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private static AnimationClip MakeIdleClip()
    {
        var clip = new AnimationClip { name = "Idle", frameRate = 60f };
        var bob = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.6f, 0.08f), new Keyframe(1.2f, 0f));
        Smooth(bob);
        clip.SetCurve("Torso", typeof(Transform), "localPosition.y", bob);
        var tilt = new AnimationCurve(new Keyframe(0f, -2f), new Keyframe(0.6f, 2f), new Keyframe(1.2f, -2f));
        Smooth(tilt);
        clip.SetCurve("Torso/Head", typeof(Transform), "localEulerAnglesRaw.z", tilt);
        SetLoop(clip, true);
        return Save(clip);
    }

    private static AnimationClip MakeWaveClip()
    {
        var clip = new AnimationClip { name = "Wave", frameRate = 60f };
        // Right arm hangs at 8 degrees; positive z swings it outward and up. The last key is the rest value
        // again, because an Animator leaves a property at its last written value once a clip stops animating it.
        var arm = new AnimationCurve(
            new Keyframe(0f, 8f), new Keyframe(0.3f, 150f), new Keyframe(0.55f, 120f), new Keyframe(0.8f, 150f),
            new Keyframe(1.05f, 120f), new Keyframe(1.3f, 150f), new Keyframe(1.6f, 8f));
        Smooth(arm);
        clip.SetCurve("Torso/ArmR", typeof(Transform), "localEulerAnglesRaw.z", arm);
        SetLoop(clip, false);
        return Save(clip);
    }

    private static void Smooth(AnimationCurve curve)
    {
        for (var i = 0; i < curve.length; i++) curve.SmoothTangents(i, 0f);
    }

    private static void SetLoop(AnimationClip clip, bool loop)
    {
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
    }

    private static AnimationClip Save(AnimationClip clip)
    {
        var path = Dir + "/" + clip.name + ".anim";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static AnimatorController MakeController(AnimationClip idle, AnimationClip wave)
    {
        var path = Dir + "/RigController.controller";
        AssetDatabase.DeleteAsset(path);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("Wave", AnimatorControllerParameterType.Trigger);
        var machine = controller.layers[0].stateMachine;

        var idleState = machine.AddState("Idle");
        idleState.motion = idle;
        machine.defaultState = idleState;
        var waveState = machine.AddState("Wave");
        waveState.motion = wave;

        var toWave = idleState.AddTransition(waveState);
        toWave.AddCondition(AnimatorConditionMode.If, 0f, "Wave");
        toWave.hasExitTime = false;
        toWave.duration = 0.1f;

        var toIdle = waveState.AddTransition(idleState);
        toIdle.hasExitTime = true;
        toIdle.exitTime = 1f;
        toIdle.duration = 0.1f;
        return controller;
    }

    private enum Face { None, Smile, Open, Flat }

    // Writes a rounded rectangle (anti-aliased, dark outline) to a PNG and imports it as a Sprite, 100 pixels per unit.
    private static Sprite MakeSprite(string name, int w, int h, float radius, Color fill, Face face, Vector2 pivot)
    {
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var pixels = new Color[w * h];
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            var d = RoundedBoxDistance(x + 0.5f, y + 0.5f, w, h, radius);
            var inside = Mathf.Clamp01(0.5f - d);
            var border = Mathf.Clamp01(0.5f - (d + 5f));
            var color = Color.Lerp(Ink, fill, border);
            color.a = inside;
            pixels[y * w + x] = color;
        }
        if (face != Face.None) DrawFace(pixels, w, h, face);
        texture.SetPixels(pixels);
        texture.Apply();

        var path = SpriteDir + "/" + name + ".png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.mipmapEnabled = false;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        importer.SetTextureSettings(settings);
        importer.spritePivot = pivot;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static float RoundedBoxDistance(float x, float y, float w, float h, float r)
    {
        var px = Mathf.Abs(x - w * 0.5f) - (w * 0.5f - r);
        var py = Mathf.Abs(y - h * 0.5f) - (h * 0.5f - r);
        var outside = new Vector2(Mathf.Max(px, 0f), Mathf.Max(py, 0f)).magnitude;
        return outside + Mathf.Min(Mathf.Max(px, py), 0f) - r;
    }

    private static void DrawFace(Color[] pixels, int w, int h, Face face)
    {
        Disc(pixels, w, h, w * 0.32f, h * 0.60f, 13f, Ink);
        Disc(pixels, w, h, w * 0.68f, h * 0.60f, 13f, Ink);
        switch (face)
        {
            case Face.Smile:
                for (var t = 0f; t <= 1f; t += 0.02f)
                    Disc(pixels, w, h, Mathf.Lerp(w * 0.3f, w * 0.7f, t), h * 0.34f - 16f * Mathf.Sin(t * Mathf.PI), 6f, Ink);
                break;
            case Face.Open:
                Disc(pixels, w, h, w * 0.5f, h * 0.30f, 18f, Ink);
                break;
            case Face.Flat:
                for (var t = 0f; t <= 1f; t += 0.02f)
                    Disc(pixels, w, h, Mathf.Lerp(w * 0.35f, w * 0.65f, t), h * 0.30f, 5f, Ink);
                break;
        }
    }

    private static void Disc(Color[] pixels, int w, int h, float cx, float cy, float radius, Color color)
    {
        for (var y = Mathf.Max(0, (int)(cy - radius - 1)); y < Mathf.Min(h, (int)(cy + radius + 2)); y++)
        for (var x = Mathf.Max(0, (int)(cx - radius - 1)); x < Mathf.Min(w, (int)(cx + radius + 2)); x++)
        {
            var a = Mathf.Clamp01(radius - Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy)) + 0.5f);
            if (a <= 0f) continue;
            var existing = pixels[y * w + x];
            pixels[y * w + x] = new Color(Mathf.Lerp(existing.r, color.r, a), Mathf.Lerp(existing.g, color.g, a), Mathf.Lerp(existing.b, color.b, a), Mathf.Max(existing.a, a));
        }
    }
}
