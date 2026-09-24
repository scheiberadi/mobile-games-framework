using UnityEditor;

// Disposable spike builder. Unity.exe -batchmode -projectPath <eva> -executeMethod CatSpikeBuild.CreateScene
public static class CatSpikeBuild
{
    public static void CreateScene()
    {
        EvaSpikeScenes.Create("Assets/Spikes/Cat/CatSpike.unity", typeof(CatSpike));
        AssetDatabase.SaveAssets();
        UnityEngine.Debug.Log("CAT_SCENE_CREATED");
        EditorApplication.Exit(0);
    }
}
