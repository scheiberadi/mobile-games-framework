using UnityEditor;
using UnityEngine;

// Disposable spike builder: makes the Auto Backup probe scene.
public static class BackupSpikeBuild
{
    public static void CreateScene()
    {
        EvaSpikeScenes.Create("Assets/Spikes/Backup/BackupProbe.unity", typeof(BackupProbe));
        AssetDatabase.SaveAssets();
        Debug.Log("BACKUP_SCENE_CREATED");
    }
}
