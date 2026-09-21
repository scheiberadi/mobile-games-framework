using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

// Disposable spike. Question: can English stay local while an optional pack arrives from Play,
// with a clean fallback to English when it does not?
public class PadSpike : MonoBehaviour
{
    private const string PackName = "voice_ro";

    private Text _label;
    private string _en = "(english missing)";
    private string _ro = "(not delivered)";

    private IEnumerator Start()
    {
        BuildUi();

        var asset = Resources.Load<TextAsset>("voice_en");
        if (asset != null)
        {
            _en = asset.text.Trim();
            Debug.Log("PAD_EN_OK " + _en);
        }
        else
        {
            Debug.Log("PAD_EN_FAILED resource voice_en missing");
        }
        Render();

        yield return null;
#if UNITY_ANDROID && !UNITY_EDITOR
        yield return RequestPack();
#else
        Debug.Log("PAD_RO_FAILED not running on an Android player");
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private IEnumerator RequestPack()
    {
        Debug.Log("PAD_REQUEST_START " + PackName);

        DownloadAssetPackAsyncOperation op = null;
        try
        {
            op = AndroidAssetPacks.DownloadAssetPackAsync(new[] { PackName });
        }
        catch (System.Exception e)
        {
            Debug.Log("PAD_RO_FAILED request threw " + e.GetType().Name + ": " + e.Message);
        }

        if (op == null)
        {
            Render();
            yield break;
        }

        var waited = 0f;
        var reported = -1;
        while (!op.isDone && waited < 120f)
        {
            waited += Time.deltaTime;
            if ((int)waited != reported)
            {
                reported = (int)waited;
                Debug.Log("PAD_WAIT " + reported + "s progress=" + op.progress);
            }
            yield return null;
        }

        if (!op.isDone)
        {
            Debug.Log("PAD_RO_FAILED timed out after " + (int)waited + "s");
            Render();
            yield break;
        }

        Debug.Log("PAD_STATE downloaded=[" + string.Join(",", op.downloadedAssetPacks)
                  + "] failed=[" + string.Join(",", op.downloadFailedAssetPacks) + "]");

        var path = AndroidAssetPacks.GetAssetPackPath(PackName);
        Debug.Log("PAD_PATH '" + path + "'");

        if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
        {
            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            Debug.Log("PAD_FILES " + string.Join("|", files));
            foreach (var file in files)
            {
                if (!file.EndsWith("voice_ro.txt"))
                    continue;
                _ro = File.ReadAllText(file).Trim();
                Debug.Log("PAD_RO_OK " + _ro);
                Render();
                yield break;
            }
        }

        Debug.Log("PAD_RO_FAILED no readable file at '" + path + "'");
        Render();
    }
#endif

    private void BuildUi()
    {
        var canvasObject = new GameObject("PadSpikeCanvas");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);

        var textObject = new GameObject("PadSpikeText");
        textObject.transform.SetParent(canvasObject.transform, false);
        _label = textObject.AddComponent<Text>();
        _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _label.fontSize = 56;
        _label.color = Color.black;
        _label.alignment = TextAnchor.MiddleCenter;
        var rect = _label.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void Render()
    {
        if (_label != null)
            _label.text = "EN: " + _en + "\nRO: " + _ro;
    }
}
