using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EvaSpikeScenes
{
    public static void Create(string scenePath, Type componentType)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.75f, 0.91f, 1f);
        if (componentType != null)
            new GameObject(componentType.Name).AddComponent(componentType);
        Directory.CreateDirectory(Path.GetDirectoryName(scenePath));
        EditorSceneManager.SaveScene(scene, scenePath);
    }
}
