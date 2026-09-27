using MobileGamesFramework.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Disposable spike. Question: is a hierarchical cutout character (sprites parented under body parts,
// driven by Animator clips, with swappable parts) cheap and smooth enough for Eva and the player?
// The rig, clips and controller are built by RigSpikeBuild and referenced from the scene, so they
// end up in the player build. This component only wires the two buttons and the on-screen readout.
public class RigSpike : MonoBehaviour
{
    public Animator animator;
    public SpriteRenderer head;
    public Sprite[] headSprites;
    public Transform torso;
    public Transform armRight;

    private Text _stats;
    private float _smoothedFps = 60f;
    private float _nextTextUpdate;
    private int _headIndex;
    private int _waveTaps;

    // Frame pacing readout. The app bootstrap caps Application.targetFrameRate at 60; the panel may run at 120 Hz.
    private int _panelHz;
    private int _mode;
    private int _bucketFrames;
    private float _measuredFps;
    private float _bucketStart;
    private float _bucketMax;
    private float _prevBucketMax;
    private float _minFpsSinceTap = 999f;
    private float _worstMsSinceTap;
    private int _slowFramesSinceTap;

    private void Start()
    {
        // The app bootstrap draws its own opaque canvas and event system in every scene; remove them.
        var bootstrapInfo = GameObject.Find("Info");
        if (bootstrapInfo != null) Destroy(bootstrapInfo.transform.root.gameObject);
        foreach (var eventSystem in FindObjectsByType<EventSystem>())
            Destroy(eventSystem.gameObject);

        // Move the rig right so the longer readout does not cover the head.
        animator.transform.position += new Vector3(6f, 0f, 0f);
        _panelHz = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);
        SetFrameRate(1);

        var canvas = UiFactory.CreateCanvas(new Vector2(1600, 900), 1f);

        // Landscape 2340x1080 with a 96 px safe area on the left: buttons on the right, readout top left.
        var wave = UiFactory.CreateButton(canvas.transform, "WAVE", new Vector2(-100f, 270f), new Vector2(240f, 240f), true, OnWave, new Vector2(1f, 0.5f));
        var swap = UiFactory.CreateButton(canvas.transform, "HEAD", new Vector2(-100f, 0f), new Vector2(240f, 240f), true, OnSwapHead, new Vector2(1f, 0.5f));
        var rate = UiFactory.CreateButton(canvas.transform, "FPS", new Vector2(-100f, -270f), new Vector2(240f, 240f), true, () => SetFrameRate((_mode + 1) % 3), new Vector2(1f, 0.5f));
        BigLabel(wave, 56);
        BigLabel(swap, 56);
        BigLabel(rate, 56);

        _stats = UiFactory.CreateText(canvas.transform, "Stats", 34, TextAnchor.UpperLeft);
        UiFactory.SetRect(_stats.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(110f, -20f), new Vector2(1100f, 420f));
        _stats.rectTransform.pivot = new Vector2(0f, 1f);
    }

    private static void BigLabel(Button button, int size)
    {
        var text = button.GetComponentInChildren<Text>();
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
    }

    // Modes: 0 = 60 cap (the bootstrap's setting, vsync 1); 1 = request 120 with vsync off;
    // 2 = as 1, plus Screen.SetResolution with an explicit 120 Hz refresh rate. The FPS button cycles them.
    private void SetFrameRate(int mode)
    {
        _mode = mode;
        QualitySettings.vSyncCount = mode == 0 ? 1 : 0;
        Application.targetFrameRate = mode == 0 ? 60 : 120;
        if (mode == 2)
            Screen.SetResolution(Screen.width, Screen.height, Screen.fullScreenMode, new RefreshRate { numerator = 120, denominator = 1 });
    }

    private void OnWave()
    {
        _waveTaps++;
        _minFpsSinceTap = 999f;
        _worstMsSinceTap = 0f;
        _slowFramesSinceTap = 0;
        // A trigger stays set until consumed, so mashing the button would replay the wave later; ignore taps mid-wave.
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Wave"))
            animator.SetTrigger("Wave");
    }

    private void OnSwapHead()
    {
        _headIndex = (_headIndex + 1) % headSprites.Length;
        head.sprite = headSprites[_headIndex];
    }

    private void Update()
    {
        var dt = Time.unscaledDeltaTime;
        if (dt > 0f)
        {
            _smoothedFps = Mathf.Lerp(_smoothedFps, 1f / dt, 0.05f);
            if (_waveTaps > 0)
            {
                _minFpsSinceTap = Mathf.Min(_minFpsSinceTap, 1f / dt);
                _worstMsSinceTap = Mathf.Max(_worstMsSinceTap, dt * 1000f);
                if (dt > 0.025f) _slowFramesSinceTap++;
            }
            _bucketMax = Mathf.Max(_bucketMax, dt);
            _bucketFrames++;
        }
        if (Time.unscaledTime - _bucketStart >= 1f)
        {
            _measuredFps = _bucketFrames / (Time.unscaledTime - _bucketStart);
            _bucketFrames = 0;
            _bucketStart = Time.unscaledTime;
            _prevBucketMax = _bucketMax;
            _bucketMax = 0f;
        }

        if (Time.unscaledTime < _nextTextUpdate || _stats == null) return;
        _nextTextUpdate = Time.unscaledTime + 0.25f;
        _panelHz = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);
        var state = animator.GetCurrentAnimatorStateInfo(0).IsName("Wave") ? "Wave" : "Idle";
        _stats.text = "MEASURED fps (1 s avg) " + _measuredFps.ToString("0.0") + "   smoothed " + _smoothedFps.ToString("0.0")
            + "\nstate " + state + "  wave taps " + _waveTaps
            + "\nmode " + _mode + " " + (_mode == 0 ? "60 CAP" : _mode == 1 ? "120 target, vsync 0" : "120 + SetResolution")
            + "\npanel " + _panelHz + " Hz  target " + Application.targetFrameRate + "  vsync " + QualitySettings.vSyncCount
            + "\nworst frame (last 2 s) " + (Mathf.Max(_bucketMax, _prevBucketMax) * 1000f).ToString("0.0") + " ms"
            + "\nsince tap: min fps " + (_minFpsSinceTap > 900f ? "-" : _minFpsSinceTap.ToString("0")) + "  worst " + _worstMsSinceTap.ToString("0.0") + " ms  over 25 ms " + _slowFramesSinceTap
            + "\nhead " + (_headIndex + 1) + "/" + headSprites.Length
            + "\ntorso y " + torso.localPosition.y.ToString("0.000")
            + "  arm z " + Mathf.DeltaAngle(0f, armRight.localEulerAngles.z).ToString("0");
    }
}
