using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

public enum DamageKind
{
    Dealt,
    Taken
}

[DisallowMultipleComponent]
public class DamageNumbers : MonoBehaviour
{
    [Header("Type")]
    public FontAsset font;
    public float fontSize = 19.4f;
    public float critSizeRatio = 30.2f / 19.4f;
    public Color color = Palette.Ink;
    public Color takenColor = Palette.HpHi;
    public float takenSizeRatio = 1.15f;
    public Color critColor = Palette.Gold;

    [Header("Motion")]
    public float duration = 1f;
    public float rise = 64.8f;
    public float drift = 18f;
    [Range(4, 120)] public int maxNumbers = 40;

    [Header("Range")]
    public float maxDistance = 60f;

    private const float PopStart = 0.55f;
    private const float PopPeak = 1.18f;
    private const float PopPeakAt = 0.11f;

    private const float ArcPeakAt = 0.72f;

    public static DamageNumbers Instance { get; private set; }

    public static void Spawn(float amount, Vector3 worldPoint, DamageKind kind = DamageKind.Dealt, bool crit = false)
    {
        if (Instance != null) Instance.Emit(amount, worldPoint, kind, crit);
    }

    private UIDocument doc;
    private VisualElement root;
    private VisualElement layer;
    private Camera cam;

    private class Num
    {
        public Label Label;
        public Vector3 World;
        public float Age;
        public float Drift;
        public float Scale;
        public bool Alive;
    }

    private readonly List<Num> pool = new List<Num>();

    private void OnEnable()
    {
        Instance = this;
        doc = GetComponent<UIDocument>();
        cam = Camera.main;
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    private void Emit(float amount, Vector3 worldPoint, DamageKind kind, bool crit)
    {
        if (!CacheTree()) return;

        Num n = null;
        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i].Alive) continue;
            n = pool[i];
            break;
        }
        if (n == null)
        {
            if (pool.Count >= maxNumbers)
            {
                n = pool[0];
                for (int i = 1; i < pool.Count; i++) if (pool[i].Age > n.Age) n = pool[i];
            }
            else
            {
                n = new Num { Label = new Label() };
                n.Label.AddToClassList("dmg");
                n.Label.pickingMode = PickingMode.Ignore;
                layer.Add(n.Label);
                pool.Add(n);
            }
        }

        n.World = worldPoint;
        n.Age = 0f;
        n.Alive = true;
        n.Drift = Random.Range(-drift, drift);
        n.Label.text = Mathf.CeilToInt(amount).ToString();
        float size = fontSize;
        if (kind == DamageKind.Taken) size *= takenSizeRatio;
        if (crit) size *= critSizeRatio;
        n.Label.style.fontSize = size;
        n.Label.style.color = crit ? critColor : (kind == DamageKind.Taken ? takenColor : color);
        if (font != null) n.Label.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromSDFFont(font));
        n.Label.style.display = DisplayStyle.Flex;
        n.Scale = PopStart;
    }

    private void LateUpdate()
    {
        if (!CacheTree()) return;
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        float panelW = root.resolvedStyle.width;
        float panelH = root.resolvedStyle.height;
        if (float.IsNaN(panelW) || panelW < 1f) return;

        float dt = Time.deltaTime;
        for (int i = 0; i < pool.Count; i++)
        {
            Num n = pool[i];
            if (!n.Alive) continue;

            n.Age += dt;
            float t = Mathf.Clamp01(n.Age / Mathf.Max(0.05f, duration));
            if (t >= 1f)
            {
                n.Alive = false;
                n.Label.style.display = DisplayStyle.None;
                continue;
            }

            Vector3 sp = cam.WorldToScreenPoint(n.World);
            if (sp.z <= 0f || sp.z > maxDistance)
            {
                n.Label.style.display = DisplayStyle.None;
                continue;
            }
            n.Label.style.display = DisplayStyle.Flex;

            float arc = (2f * t / ArcPeakAt) - (t * t / (ArcPeakAt * ArcPeakAt));
            float x = sp.x / Screen.width * panelW + n.Drift * t;
            float y = (1f - sp.y / Screen.height) * panelH - rise * arc;

            n.Scale = t < PopPeakAt
                ? Mathf.Lerp(PopStart, PopPeak, Mathf.SmoothStep(0f, 1f, t / PopPeakAt))
                : Mathf.Lerp(PopPeak, 1f, Mathf.SmoothStep(0f, 1f, (t - PopPeakAt) / (1f - PopPeakAt)));

            float fadeIn = Mathf.Clamp01(t / PopPeakAt);
            float fadeOut = t < 0.6f ? 1f : Mathf.Clamp01(1f - (t - 0.6f) / 0.4f);

            float w = n.Label.resolvedStyle.width;
            float h = n.Label.resolvedStyle.height;
            if (!float.IsNaN(w) && w > 0f) x -= w * 0.5f;
            if (!float.IsNaN(h) && h > 0f) y -= h * 0.5f;

            n.Label.style.translate = new Translate(x, y);
            n.Label.style.scale = new Scale(Vector2.one * n.Scale);
            n.Label.style.opacity = fadeIn * fadeOut;
        }
    }

    private bool CacheTree()
    {
        if (doc == null) doc = GetComponent<UIDocument>();
        VisualElement r = doc != null ? doc.rootVisualElement : null;
        if (r == null) return false;
        if (r == root && layer != null) return true;

        root = r;
        layer = root.Q<VisualElement>("damage-numbers");
        if (layer == null)
        {
            layer = new VisualElement { name = "damage-numbers" };
            layer.AddToClassList("damage-numbers");
            layer.pickingMode = PickingMode.Ignore;
            VisualElement plates = root.Q<VisualElement>("enemy-plates");
            int index = plates != null ? root.IndexOf(plates) + 1 : 0;
            root.Insert(index, layer);
        }
        pool.Clear();
        layer.Clear();
        return true;
    }
}
