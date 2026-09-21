using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Disposable spike. Question: does TextMeshPro with a Noto Sans base font plus Noto Sans JP, KR and SC
// dynamic fallbacks render all 11 planned languages on the phone? The TEXT_MISSING result is also shown
// on screen because logcat output from release-mode builds was not reliable in an earlier spike.
public class TextSpike : MonoBehaviour
{
    public TMP_FontAsset baseFont;

    private static readonly string[] Lines =
    {
        "en  Good morning, little explorer!",
        "es  ¡Buenos días, pequeña exploradora!",
        "pt  Bom dia, pequena exploradora! Coração",
        "de  Guten Morgen, kleine Entdeckerin! Ä Ö Ü ß",
        "fr  Bonjour, petite exploratrice ! Où ê œ",
        "it  Buongiorno, piccola esploratrice! è ì",
        // ă â î ș ț Ă Â Î Ș Ț with comma-below s and t (U+0219, U+021B, U+0218, U+021A)
        "ro  Bună dimineața! ă â î ș ț Ă Â Î Ș Ț",
        "ru  Доброе утро, маленький исследователь!",
        "ja  おはよう、ちいさなぼうけんか！漢字",
        "ko  안녕하세요, 작은 탐험가!",
        "zh-Hans  早上好，小小探险家！"
    };

    private void Start()
    {
        // The app bootstrap draws its own info canvas in every scene; remove it so it does not cover the spike text.
        var bootstrapInfo = GameObject.Find("Info");
        if (bootstrapInfo != null) Destroy(bootstrapInfo.transform.root.gameObject);

        var joined = string.Join("\n", Lines);

        // Ask for every character first so the dynamic atlases add the glyphs they can (base, then fallbacks).
        uint[] missing;
        baseFont.HasCharacters(joined, out missing, true, true);
        var missingCount = missing == null ? 0 : missing.Length;
        var missingText = new StringBuilder();
        if (missing != null)
            foreach (var codePoint in missing) missingText.Append("U+").Append(codePoint.ToString("X4")).Append(' ');
        Debug.Log("TEXT_MISSING " + missingCount + " " + missingText);

        var canvasObject = new GameObject("TextSpikeCanvas");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);
        scaler.matchWidthOrHeight = 0.5f;

        var header = MakeText(canvasObject.transform, "Header", 54, new Vector2(0f, 0.88f), new Vector2(1f, 1f));
        header.alignment = TextAlignmentOptions.Center;
        header.color = new Color(0.7f, 0f, 0f);

        var body = MakeText(canvasObject.transform, "Body", 46, new Vector2(0f, 0f), new Vector2(1f, 0.88f));
        body.alignment = TextAlignmentOptions.MidlineLeft;
        body.margin = new Vector4(60, 0, 40, 0);
        body.text = joined;
        body.ForceMeshUpdate();

        // Second check on what was actually laid out: visible characters that resolved to glyph index 0 (.notdef).
        var info = body.textInfo;
        var notdef = 0;
        for (var i = 0; i < info.characterCount; i++)
        {
            var c = info.characterInfo[i];
            if (!c.isVisible && !char.IsWhiteSpace(c.character)) { notdef++; continue; }
            if (c.isVisible && (c.textElement == null || c.textElement.glyphIndex == 0)) notdef++;
        }
        var faces = new HashSet<string>();
        for (var i = 0; i < info.characterCount; i++)
            if (info.characterInfo[i].isVisible && info.characterInfo[i].fontAsset != null)
                faces.Add(info.characterInfo[i].fontAsset.name);

        header.text = "TEXT_MISSING " + missingCount + "   (laid out .notdef: " + notdef + ")";
        Debug.Log("TEXT_LAYOUT_NOTDEF " + notdef + " fonts used: " + string.Join(", ", faces));
    }

    private TextMeshProUGUI MakeText(Transform parent, string name, float size, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var label = go.AddComponent<TextMeshProUGUI>();
        label.font = baseFont;
        label.fontSize = size;
        label.color = Color.black;
        label.enableWordWrapping = false;
        var rect = label.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return label;
    }
}
