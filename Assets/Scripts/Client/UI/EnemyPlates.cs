using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public class EnemyPlates : MonoBehaviour
{
    [Header("Plate")]
    public Vector2 barSize = new Vector2(104f, 8f);
    public float screenOffset = 4f;

    [Header("Range")]
    public float maxDistance = 40f;
    [Range(1, 60)] public int maxPlates = 24;

    [Header("Style")]
    public EnemyBarStyle style = EnemyBarStyle.Default;

    [Header("Timing")]
    public float fadeSpeed = 7f;
    public float holdDuration = 2.5f;
    public float chipDrainDelay = 0.25f;
    public float chipDrainSpeed = 0.6f;
    [Range(0f, 60f)] public float followSmoothing = 22f;

    [Header("Name")]
    public bool showName = true;
    public FontAsset nameFont;
    public float nameFontSize = 12f;
    public float nameGap = 2f;
    public Color nameTint = Palette.Ink;
    public Color nameEliteTint = Palette.Gold;
    public Color nameLevelTint = Palette.InkDim;
    [Range(0f, 1f)] public float nameOutlineWidth = 0.3f;
    public Color nameOutlineColor = Color.black;

    private static readonly List<EnemyPlateTarget> Targets = new List<EnemyPlateTarget>();

    public static void Register(EnemyPlateTarget t)
    {
        if (t != null && !Targets.Contains(t)) Targets.Add(t);
    }

    public static void Unregister(EnemyPlateTarget t) => Targets.Remove(t);

    private UIDocument doc;
    private VisualElement root;
    private VisualElement layer;
    private Camera cam;

    private readonly List<Plate> pool = new List<Plate>();

    private class Plate
    {
        public VisualElement Root;
        public CssBox Track;
        public CssBox Chip;
        public CssBox Fill;
        public Label Name;
        public Label Level;
        public VisualElement RankIcon;
        public VisualElement Title;
        public EnemyPlateTarget Owner;
        public float Visibility;
        public float HoldTimer;
        public float ChipFraction = 1f;
        public float ChipHold;
        public float LastFraction = 1f;
        public Vector2 ShownPos;
        public bool Placed;
        public bool Styled;
        public bool Claimed;
    }

    private void OnEnable()
    {
        doc = GetComponent<UIDocument>();
        cam = Camera.main;
    }

    private void LateUpdate()
    {
        if (!CacheTree()) return;
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        for (int i = 0; i < pool.Count; i++) pool[i].Claimed = false;

        int used = 0;
        for (int i = 0; i < Targets.Count && used < maxPlates; i++)
        {
            EnemyPlateTarget t = Targets[i];
            if (t == null || t.Health == null) continue;

            Vector3 world = t.transform.position + Vector3.up * t.HeightOffset;

            Vector3 sp = cam.WorldToScreenPoint(world);
            if (sp.z <= 0f) continue;

            float limit = HasPlate(t) ? maxDistance * 1.1f : maxDistance;
            if (sp.z > limit) continue;

            Plate p = ClaimPlate(t);
            if (p == null) continue;
            used++;
            Position(p, sp);
            Drive(p, t, Time.deltaTime);
        }

        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i].Claimed) continue;
            pool[i].Root.style.display = DisplayStyle.None;
            pool[i].Owner = null;
        }

        if (!styleHash.Equals(style))
        {
            styleHash = style;
            for (int i = 0; i < pool.Count; i++) pool[i].Styled = false;
        }
    }

    private bool CacheTree()
    {
        if (doc == null) doc = GetComponent<UIDocument>();
        VisualElement r = doc != null ? doc.rootVisualElement : null;
        if (r == null) return false;
        if (r == root && layer != null) return true;

        root = r;
        layer = root.Q<VisualElement>("enemy-plates");
        if (layer == null)
        {
            layer = new VisualElement { name = "enemy-plates" };
            layer.AddToClassList("enemy-plates");
            layer.pickingMode = PickingMode.Ignore;
            root.Insert(0, layer);
        }
        pool.Clear();
        layer.Clear();
        return true;
    }

    private bool HasPlate(EnemyPlateTarget owner)
    {
        for (int i = 0; i < pool.Count; i++) if (pool[i].Owner == owner) return true;
        return false;
    }

    private Plate ClaimPlate(EnemyPlateTarget owner)
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i].Owner != owner) continue;
            pool[i].Claimed = true;
            pool[i].Root.style.display = DisplayStyle.Flex;
            return pool[i];
        }

        Plate p = null;
        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i].Claimed || pool[i].Owner != null) continue;
            p = pool[i];
            break;
        }
        if (p == null)
        {
            if (pool.Count >= maxPlates) return null;
            p = Build();
            pool.Add(p);
        }

        p.Owner = owner;
        p.Claimed = true;
        p.Visibility = 0f;
        p.HoldTimer = 0f;
        p.ChipFraction = owner.Fraction;
        p.ChipHold = 0f;
        p.LastFraction = owner.Fraction;
        p.Name.text = owner.DisplayName;
        p.Level.text = owner.Level > 0 ? $"Lv {owner.Level}" : string.Empty;
        p.RankIcon.style.display = owner.Rank == EnemyRank.Normal ? DisplayStyle.None : DisplayStyle.Flex;
        p.Root.style.display = DisplayStyle.Flex;
        return p;
    }

    private Plate Build()
    {
        Plate p = new Plate { Root = new VisualElement() };
        p.Root.AddToClassList("plate");
        p.Root.pickingMode = PickingMode.Ignore;

        VisualElement row = new VisualElement();
        row.AddToClassList("plate-title");
        p.Title = row;
        p.RankIcon = new VisualElement();
        p.RankIcon.AddToClassList("plate-rank");
        p.Name = new Label();
        p.Name.AddToClassList("plate-name");
        p.Level = new Label();
        p.Level.AddToClassList("plate-level");
        row.Add(p.RankIcon);
        row.Add(p.Name);
        row.Add(p.Level);

        p.Track = new CssBox();
        p.Track.AddToClassList("plate-track");
        p.Chip = new CssBox();
        p.Chip.AddToClassList("plate-chip");
        p.Fill = new CssBox();
        p.Fill.AddToClassList("plate-fill");
        p.Track.Add(p.Chip);
        p.Track.Add(p.Fill);

        p.Root.Add(row);
        p.Root.Add(p.Track);
        layer.Add(p.Root);

        p.Root.Query<VisualElement>().ForEach(e => e.pickingMode = PickingMode.Ignore);

        return p;
    }

    private void ApplyStyle(Plate p)
    {
        float r = Mathf.Max(0f, style.cornerRadius);
        SetRadius(p.Track, r);
        SetRadius(p.Fill, Mathf.Max(0f, r - 1f));
        SetRadius(p.Chip, Mathf.Max(0f, r - 1f));

        p.Track.SetBaseGradient(style.trackColor, style.trackColor);
        p.Fill.SetBaseGradient(style.healthColor, style.healthColor);
        p.Chip.SetBaseGradient(style.chipColor, style.chipColor);
    }

    private static void SetRadius(VisualElement e, float r)
    {
        e.style.borderTopLeftRadius = r;
        e.style.borderTopRightRadius = r;
        e.style.borderBottomLeftRadius = r;
        e.style.borderBottomRightRadius = r;
    }

    private void Position(Plate p, Vector3 screenPoint)
    {
        float panelW = root.resolvedStyle.width;
        float panelH = root.resolvedStyle.height;
        if (float.IsNaN(panelW) || panelW < 1f || float.IsNaN(panelH) || panelH < 1f) return;

        float x = screenPoint.x / Screen.width * panelW;
        float y = (1f - screenPoint.y / Screen.height) * panelH;

        float height = p.Root.resolvedStyle.height;
        if (float.IsNaN(height)) height = barSize.y;

        Vector2 target = new Vector2(x - barSize.x * 0.5f, y - height - screenOffset);

        if (!p.Placed || followSmoothing <= 0f)
        {
            p.ShownPos = target;
            p.Placed = true;
        }
        else
        {
            p.ShownPos = Vector2.Lerp(p.ShownPos, target, 1f - Mathf.Exp(-followSmoothing * Time.deltaTime));
        }

        p.Root.style.left = Mathf.Round(p.ShownPos.x);
        p.Root.style.top = Mathf.Round(p.ShownPos.y);
        p.Root.style.width = Mathf.Round(barSize.x);
    }

    private EnemyBarStyle styleHash;

    private void Drive(Plate p, EnemyPlateTarget t, float dt)
    {
        float fraction = t.Fraction;

        if (fraction < p.LastFraction - 0.0001f)
        {
            p.ChipHold = chipDrainDelay;
            p.HoldTimer = holdDuration;
        }
        p.LastFraction = fraction;

        if (p.ChipHold > 0f) p.ChipHold -= dt;
        else p.ChipFraction = Mathf.Max(fraction, p.ChipFraction - chipDrainSpeed * dt);

        if (fraction > p.ChipFraction) p.ChipFraction = fraction;

        if (p.HoldTimer > 0f) p.HoldTimer -= dt;

        bool show = fraction < 0.999f || p.HoldTimer > 0f;
        p.Visibility = Mathf.MoveTowards(p.Visibility, show ? 1f : 0f, fadeSpeed * dt);
        p.Track.style.opacity = p.Visibility;

        p.Root.style.opacity = 1f - Mathf.Clamp01(t.VisionFade);

        p.Track.style.height = Mathf.Round(barSize.y);

        if (!p.Styled || !styleHash.Equals(style))
        {
            ApplyStyle(p);
            p.Styled = true;
        }
        p.Fill.SetLevel(fraction);
        bool hasChip = style.showChip && p.ChipFraction > fraction + 0.002f;
        p.Chip.style.display = hasChip ? DisplayStyle.Flex : DisplayStyle.None;
        if (hasChip) p.Chip.SetLevelRange(fraction, p.ChipFraction);

        p.Name.style.fontSize = nameFontSize;
        p.Name.style.color = nameTint;
        if (nameFont != null)
        {
            StyleFontDefinition fd = new StyleFontDefinition(FontDefinition.FromSDFFont(nameFont));
            p.Name.style.unityFontDefinition = fd;
            p.Level.style.unityFontDefinition = fd;
        }
        p.Level.style.fontSize = nameFontSize * 0.9f;
        p.Level.style.color = nameLevelTint;

        p.Name.style.unityTextOutlineWidth = nameOutlineWidth;
        p.Name.style.unityTextOutlineColor = nameOutlineColor;
        p.Level.style.unityTextOutlineWidth = nameOutlineWidth;
        p.Level.style.unityTextOutlineColor = nameOutlineColor;
        p.RankIcon.style.unityBackgroundImageTintColor = nameEliteTint;
        p.RankIcon.style.width = nameFontSize;
        p.RankIcon.style.height = nameFontSize;
        if (p.Title != null)
        {
            p.Title.style.display = showName ? DisplayStyle.Flex : DisplayStyle.None;
            p.Title.style.marginBottom = nameGap;
        }
    }
}
