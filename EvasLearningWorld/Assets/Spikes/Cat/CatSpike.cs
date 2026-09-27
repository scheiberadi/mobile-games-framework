using MobileGamesFramework.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Disposable spike. Question: do layered SVG-drawn cat sprites (sitting pose) with code-driven bounces, breathing,
// ear twitch, blink and mouth flap read as a real cat at Eva's real on-screen size? Not production code.
public class CatSpike : MonoBehaviour
{
    private const float Size = 740f;          // layer canvas (1000 art units) shown at 740 px -> cat ~560 px tall
    private const float GroundPivot = 0.06f;  // ground line at art y=940

    private static readonly string[] Backgrounds = { "world/map_bg", "world/school_bg", "world/house_bg", "world/store_bg" };

    private Image _bg;
    private int _bgIndex;
    private bool _countMode;
    private RectTransform _tail, _holder, _root, _body, _legL, _legR, _head, _earL, _earR, _eyes;
    private Image _mouth;
    private bool _talking;
    private float _angry;
    private float _bounceT = -1f, _bounceDur, _bounceHeight;
    private int _bounceCount;
    private float _tilt;
    private float _nextTwitch = 2f, _nextBlink = 3f, _twitchT = -1f, _blinkT = -1f;
    private bool _twitchLeft;
    private float _mouthTimer;
    private Text _label;

    private void Start()
    {
        var info = GameObject.Find("Info");
        if (info != null) DestroyImmediate(info.transform.root.gameObject);
        foreach (var es in FindObjectsByType<EventSystem>()) DestroyImmediate(es.gameObject);

        var canvas = UiFactory.CreateCanvas(new Vector2(1440, 900), 1f);
        _bg = Stretch("Bg", canvas.transform).gameObject.AddComponent<Image>();
        _bg.preserveAspect = false;
        _bg.raycastTarget = false;

        _holder = new GameObject("CatHolder", typeof(RectTransform)).GetComponent<RectTransform>();
        _holder.SetParent(canvas.transform, false);
        _holder.sizeDelta = Vector2.zero;

        _root = NewRect("Rig", _holder, new Vector2(0.5f, GroundPivot));
        _root.anchorMin = _root.anchorMax = new Vector2(0.5f, 0f);
        _root.sizeDelta = new Vector2(Size, Size);
        _root.anchoredPosition = Vector2.zero;

        Layer("cat_shadow", _root, 500, 948);
        _tail = Layer("cat_tail", _root, 700, 915);
        _body = Layer("cat_body", _root, 500, 940);
        _legL = Layer("cat_legL", _body, 452, 740);
        _legR = Layer("cat_legR", _body, 548, 740);
        Layer("cat_chest", _body, 500, 700);
        _head = Layer("cat_head", _body, 500, 510);
        // Ears rotate with the head but draw behind the head image.
        _earL = Layer("cat_earL", _head, 390, 320);
        _earR = Layer("cat_earR", _head, 610, 320);
        _earL.SetAsFirstSibling();
        _earR.SetAsFirstSibling();
        _eyes = Layer("cat_eyes", _head, 500, 380);
        _mouth = Layer("cat_mouth", _head, 500, 478).GetComponent<Image>();
        _mouth.enabled = false;

        _label = UiFactory.CreateText(canvas.transform, "Label", 30, TextAnchor.UpperLeft);
        UiFactory.SetRect(_label.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(110f, -20f), new Vector2(900f, 60f));
        _label.rectTransform.pivot = new Vector2(0f, 1f);
        _label.color = Color.white;

        Btn(canvas.transform, "TALK", 0, () => _talking = !_talking);
        Btn(canvas.transform, "CHEER", 1, Cheer);
        Btn(canvas.transform, "GREET", 2, Greet);
        Btn(canvas.transform, "BG", 3, () => SetBg(_bgIndex + 1));
        Btn(canvas.transform, "ANGRY", 5, () => _angry = 1.6f);
        Btn(canvas.transform, "MAP/CNT", 4, () => { _countMode = !_countMode; PlaceCat(); });
        SetBg(0);
        PlaceCat();
    }

    // Map: anchored right-bottom, offset (-260, 40). Count: centre anchor + (520,-120), mirrored to face left.
    private void PlaceCat()
    {
        _holder.anchorMin = _holder.anchorMax = _countMode ? new Vector2(0.5f, 0.5f) : new Vector2(1f, 0f);
        _holder.anchoredPosition = _countMode ? new Vector2(520f, -120f) : new Vector2(-350f, 40f);
        _holder.localScale = new Vector3(_countMode ? -1f : 1f, 1f, 1f);
        _label.text = (_countMode ? "COUNT " : "MAP ") + Backgrounds[_bgIndex];
    }

    private void SetBg(int i)
    {
        _bgIndex = (i % Backgrounds.Length + Backgrounds.Length) % Backgrounds.Length;
        _bg.sprite = Resources.Load<Sprite>("Art/" + Backgrounds[_bgIndex]);
        _label.text = (_countMode ? "COUNT " : "MAP ") + Backgrounds[_bgIndex];
    }

    private static void Btn(Transform parent, string text, int index, UnityEngine.Events.UnityAction act)
    {
        var button = UiFactory.CreateButton(parent, text, new Vector2(110f + index * 190f, 20f), new Vector2(175f, 130f), true, act, new Vector2(0f, 0f));
        var t = button.GetComponentInChildren<Text>();
        t.fontSize = 30;
        t.fontStyle = FontStyle.Bold;
    }

    private static RectTransform Stretch(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var r = (RectTransform)go.transform;
        r.SetParent(parent, false);
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        return r;
    }

    private static RectTransform NewRect(string name, Transform parent, Vector2 pivot)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var r = (RectTransform)go.transform;
        r.SetParent(parent, false);
        r.pivot = pivot;
        return r;
    }

    // Full-rect layer image whose pivot (art coords, y down) is where it rotates and scales.
    private static RectTransform Layer(string sprite, Transform parent, float px, float py)
    {
        var r = Stretch(sprite, parent);
        r.pivot = new Vector2(px / 1000f, 1f - py / 1000f);
        var img = r.gameObject.AddComponent<Image>();
        img.sprite = Resources.Load<Sprite>("Art/cat/" + sprite);
        img.raycastTarget = false;
        return r;
    }

    private void Cheer() { _bounceT = 0f; _bounceDur = 0.9f; _bounceHeight = 90f; _bounceCount = 2; _tilt = 0f; }
    private void Greet() { _bounceT = 0f; _bounceDur = 0.7f; _bounceHeight = 40f; _bounceCount = 2; _tilt = 1f; }

    private void Update()
    {
        var t = Time.time;
        var dt = Time.deltaTime;

        // idle: breathing, tiny head sway
        var breath = Mathf.Sin(t * Mathf.PI * 0.5f);
        var sy = 1f + 0.012f * breath;
        var sx = 1f - 0.004f * breath;
        var headRot = Mathf.Sin(t * 0.7f) * 1.2f;
        var headY = Mathf.Sin(t * 0.9f + 1f) * 3f;

        // bounce: N hops, each squash, stretch in the air, squash on landing
        var hop = 0f;
        var perk = 0f;
        var tiltDeg = 0f;
        if (_bounceT >= 0f)
        {
            _bounceT += dt;
            var per = _bounceDur / _bounceCount;
            var idx = Mathf.FloorToInt(_bounceT / per);
            if (idx >= _bounceCount) _bounceT = -1f;
            else
            {
                var u = (_bounceT - idx * per) / per;
                var crouch = Mathf.Clamp01(1f - u / 0.2f) * 0.5f + Mathf.Clamp01((u - 0.85f) / 0.15f) * 0.5f;
                var air = u > 0.2f && u < 0.85f ? Mathf.Sin((u - 0.2f) / 0.65f * Mathf.PI) : 0f;
                var squash = crouch * 0.08f;
                hop = air * _bounceHeight;
                sy += air * 0.06f - squash;
                sx += squash * 0.5f - air * 0.03f;
                perk = air;
                tiltDeg = _tilt * Mathf.Sin(u * Mathf.PI) * 6f * (idx == 0 ? 1f : -1f);
                headY -= air * 6f;
            }
        }

        _root.anchoredPosition = new Vector2(0f, hop);
        // normal cat tail: slow sway, livelier during bounces
        // idle: slow small sway; talking: livelier; angry (kid got it wrong): fast hard lashing, ears flatten outward
        if (_angry > 0f) _angry -= dt;
        var lash = _angry > 0f ? 1f : 0f;
        var tailSwing = 2f + 5f * perk + (_talking ? 6f : 0f) + 14f * lash;
        var tailRate = 1.3f + (_talking ? 2f : 0f) + 6f * lash;
        _tail.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(t * tailRate) * tailSwing);
        _body.localScale = new Vector3(sx, sy, 1f);
        _head.localEulerAngles = new Vector3(0f, 0f, headRot + tiltDeg);
        _head.anchoredPosition = new Vector2(0f, headY);
        // front legs tuck up and swing out a little in the air, extend again on landing
        _legL.localScale = _legR.localScale = new Vector3(1f, 1f - 0.22f * perk, 1f);
        _legL.localEulerAngles = new Vector3(0f, 0f, 10f * perk);
        _legR.localEulerAngles = new Vector3(0f, 0f, -10f * perk);

        // angry: ears shorten (laid back) and the head drops a little
        _earL.localScale = _earR.localScale = new Vector3(1f, lash > 0f ? 0.8f : 1f, 1f);
        _head.anchoredPosition += new Vector2(0f, -10f * lash);

        // ear twitch
        var angleL = 0f;
        var angleR = 0f;
        if (_twitchT < 0f && t > _nextTwitch) { _twitchT = 0f; _twitchLeft = Random.value < 0.5f; }
        if (_twitchT >= 0f)
        {
            _twitchT += dt;
            var a = Mathf.Sin(Mathf.Clamp01(_twitchT / 0.22f) * Mathf.PI) * 9f;
            if (_twitchLeft) angleL = a; else angleR = -a;
            if (_twitchT > 0.22f) { _twitchT = -1f; _nextTwitch = t + Random.Range(3f, 6f); }
        }
        _earL.localEulerAngles = new Vector3(0f, 0f, angleL + perk * 5f + lash * 9f);
        _earR.localEulerAngles = new Vector3(0f, 0f, angleR - perk * 5f - lash * 9f);

        // blink
        var blink = 1f;
        if (_blinkT < 0f && t > _nextBlink) _blinkT = 0f;
        if (_blinkT >= 0f)
        {
            _blinkT += dt;
            blink = 1f - Mathf.Sin(Mathf.Clamp01(_blinkT / 0.16f) * Mathf.PI);
            if (_blinkT > 0.16f) { _blinkT = -1f; _nextBlink = t + Random.Range(3f, 5f); }
        }
        _eyes.localScale = new Vector3(1f, Mathf.Max(0.05f, blink) * (lash > 0f ? 0.75f : 1f), 1f);

        // talk: mouth overlay flaps irregularly, tiny head bob
        if (_talking)
        {
            _mouthTimer -= dt;
            if (_mouthTimer <= 0f) { _mouth.enabled = !_mouth.enabled; _mouthTimer = Random.Range(0.07f, 0.2f); }
            _head.anchoredPosition += new Vector2(0f, Mathf.Abs(Mathf.Sin(t * 9f)) * 3f);
        }
        else _mouth.enabled = false;
    }
}
