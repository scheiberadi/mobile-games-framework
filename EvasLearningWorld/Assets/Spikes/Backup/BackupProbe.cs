using UnityEngine;
using UnityEngine.UI;

// Disposable spike. Question: is a PlayerPrefs value included in Android Auto Backup?
public class BackupProbe : MonoBehaviour
{
    private const string Key = "eva.spike.launches";

    private void Start()
    {
        var launches = PlayerPrefs.GetInt(Key, 0) + 1;
        PlayerPrefs.SetInt(Key, launches);
        PlayerPrefs.Save();
        Debug.Log("BACKUP_PROBE launches: " + launches);

        var canvasObject = new GameObject("BackupProbeCanvas");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);

        var textObject = new GameObject("BackupProbeText");
        textObject.transform.SetParent(canvasObject.transform, false);
        var label = textObject.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 96;
        label.color = Color.black;
        label.alignment = TextAnchor.MiddleCenter;
        label.text = "launches: " + launches;
        var rect = label.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
