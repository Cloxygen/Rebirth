using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.TextCore.Text;

[ExecuteAlways]
[RequireComponent(typeof(UIDocument))]
public class HudController : MonoBehaviour
{
    [Header("Levels")]
    [Range(0f, 100f)] public float health = 78f;
    [Range(0f, 100f)] public float mana = 55f;

    [Header("Health binding")]
    public bool bindPlayerHealth = true;
    public Health playerHealth;

    [Header("Mana binding")]
    public bool bindPlayerMana = true;
    public ResourcePool playerMana;
    [Range(0.5f, 2f)] public float uiScale = 1f;
    const float UiScaleBaseline = 1f;

    [Header("Shadows")]
    public bool shadows = true;
    [Range(0f, 1f)] public float shadowOpacity = 1f;

    [Header("Trail (damage band)")]
    [Range(0f, 1f)] public float trailHoldDelay = 0.2f;
    [Range(0.05f, 2f)] public float trailDuration = 0.55f;

    [Header("Globe colours")]
    public Color globeWell = new Color(0f, 0f, 0f, 0.75f);

    [Header("Globe (rim)")]
    [Range(0f, 2.2f)] public float globeRimWidth = 1f;
    public Color globeRimColor = new Color(138f / 255f, 122f / 255f, 92f / 255f, 1f);

    [Header("Essence (orb)")]
    public bool essenceOrb = true;
    public int essAmount = 24860;
    public Color essColor = new Color(216f / 255f, 207f / 255f, 166f / 255f, 1f);
    [Range(0f, 1f)] public float essGlow = 0.15f;

    [Header("Liquid swirl")]
    public bool liquidSwirl;
    [Range(0f, 1f)] public float swirlStrength = 0.6f;
    [Range(0f, 2f)] public float swirlSpeed = 1f;
    [Range(0f, 1.5f)] public float swirlWarp = 0.45f;

    [Header("Action bar")]
    public bool barTracery = true;

    [Header("Menu (icons)")]
    public Color menuIconColor = new Color(195f / 255f, 176f / 255f, 136f / 255f, 1f);
    public Color menuIconHoverColor = new Color(244f / 255f, 220f / 255f, 170f / 255f, 1f);

    private static readonly Color XpPlateTop = new Color(13f / 255f, 10f / 255f, 19f / 255f, 1f);
    private static readonly Color XpPlateBottom = new Color(5f / 255f, 4f / 255f, 8f / 255f, 1f);

    private static readonly Color XpDefColor = new Color(139f / 255f, 130f / 255f, 89f / 255f, 1f);
    private static readonly Vector3 XpHighlight = new Vector3(0.345f, 0.312f, 0.229f);
    private static readonly Vector3 XpShadow    = new Vector3(0.683f, 0.669f, 0.629f);

    private static readonly Color QDefParchment = new Color(0.765f, 0.690f, 0.533f, 0.97f);
    private static readonly Color QDefInk       = new Color(0f, 0f, 0f, 1f);
    private static readonly Color QDefAccent    = new Color(0.541f, 0.392f, 0.071f, 1f);
    private const float QDefBloom        = 0.22f;
    private const float QDefAging        = 0.30f;
    private const float QDefDoneFade     = 0.60f;
    private const float QDefRightPercent = 1.6875f;
    private const float QDefTopOffset    = 216f;
    private const float QDefWidth        = 286f;

    private const float QFarTint  = 0.72f;
    private const float QFarAlpha = 0.36f;

    [Header("Globe surface (waterline highlight)")]
    [Range(0f, 3f)] public float globeSurfaceStrength = 0.9f;
    [Range(0.5f, 24f)] public float globeSurfaceWidth = 3.5f;

    [Header("Rims")]
    public bool globeRimGold = true;
    public bool globeRimBrown = true;
    public bool minimapRimGold = true;
    public bool minimapRimBrown = true;
    public bool barRim = true;

    [Header("Quest tracker")]
    public bool questPlate = true;
    public Color questParchment = QDefParchment;
    public Color questInk = QDefInk;
    public Color questAccent = QDefAccent;
    [Range(0f, 1f)] public float questBloom = QDefBloom;
    [Range(0f, 1f)] public float questAging = QDefAging;
    [Range(0.15f, 1f)] public float questDoneFade = QDefDoneFade;
    [Range(0f, 40f)] public float questRightPercent = QDefRightPercent;
    [Range(0f, 700f)] public float questTopOffset = QDefTopOffset;
    [Range(180f, 520f)] public float questWidth = QDefWidth;

    [Header("Fonts")]
    public FontAsset displayFont;
    public FontAsset bodyFont;
    public FontAsset bindingFont;
    public FontAsset essenceFont;

    [Header("Text")]
    [Range(0f, 50f)] public float labelSpacing = 25f;

    [Header("Action Bar colours (base gradient — live)")]
    public Color barTop = new Color(61f / 255f, 52f / 255f, 34f / 255f, 1f);
    public Color barBottom = new Color(34f / 255f, 28f / 255f, 17f / 255f, 1f);
    public Color slotTop = new Color(34f / 255f, 28f / 255f, 17f / 255f, 1f);
    public Color slotBottom = new Color(61f / 255f, 52f / 255f, 34f / 255f, 1f);

    [Header("Action Bar icons (drag textures — live)")]
    public Texture2D[] slotIcons = new Texture2D[8];

    [Header("XP bar")]
    public bool xpBar = true;
    [Range(0f, 1f)] public float xpFill = 0.64f;
    [Range(2f, 40f)] public float xpBarHeight = 10.69f;
    public bool xpTicks = true;
    [Range(0f, 1f)] public float xpBackfillOpacity = 0.5f;
    public Color xpColor = XpDefColor;

    [Header("Potion belt")]
    public bool potionBelt = true;
    [Range(-400f, 400f)] public float beltVerticalOffset;
    public int[] beltCounts = { 5, 3, 2, 1 };
    public float[] beltCooldowns = new float[4];
    public Texture2D[] beltIcons = new Texture2D[4];
    public bool beltTintFromIcon = true;
    [Range(0f, 2f)] public float beltPoolStrength = 1.35f;

    [Header("Debug")]
    public bool screenCentreX;

    [Header("Minimap")]
    public bool zoneTextShadow = true;
    public string zoneName = "ASHEN HOLLOW";
    public Camera minimapCamera;
    public bool minimapFollowsPlayer = true;
    [Range(10f, 400f)] public float minimapHeight = 120f;
    [Range(1f, 60f)] public float minimapRefreshHz = 15f;
    public bool minimapZoom = true;
    [Range(2f, 200f)] public float minimapZoomMin = 10f;
    [Range(2f, 400f)] public float minimapZoomMax = 90f;
    [Range(1.01f, 1.5f)] public float minimapZoomStep = 1.12f;
    public bool minimapStartZoomedOut = true;
    public bool minimapProfile;

    private UIDocument doc;
    private VisualElement root;
    private CssBox hLiquid, mLiquid, hTrail, mTrail;
    private CssBox xpBarBox, xpFillBox;
    private VisualElement xpTicksBox;
    private VisualElement belt;
    private VisualElement essence;
    private static readonly HashSet<int> warnedUnreadable = new HashSet<int>();
    private readonly List<CssBox> swirlBoxes = new List<CssBox>();
    private float swirlClock;
    private Label hNum;
    private Label mNum;
    private float findCooldown;
    private float findManaCooldown;
    private bool justBound;
    private bool justBoundMana;
    private bool treeRebuilt;
    private bool minimapZoomStarted;
    private MinimapView minimap;
    private VisualElement screenX;
    private const int ProfBlock = 120;
    private const int ProfWarmup = 10;
    private int profFrame, profOnCount, profOffCount;
    private double profOnSum, profOffSum;
    private readonly List<VisualElement> globeEdges = new();
    private Trail trailH = new Trail(0.78f), trailM = new Trail(0.55f);
    private float prevH = 78f, prevM = 55f;

    private struct Trail
    {
        public float Value;
        public float From;
        public float Elapsed;
        public float Hold;

        public Trail(float level) { Value = From = level; Elapsed = 0f; Hold = 0f; }

        public void Snap(float level) { Value = From = level; Elapsed = 0f; Hold = 0f; }

        public void Restart(float delay) { From = Value; Elapsed = 0f; Hold = delay; }
    }

    private void OnEnable()
    {
        trailH.Snap(health / 100f); trailM.Snap(mana / 100f);
        prevH = health; prevM = mana;
        Cache(); Apply();
    }

    private void OnValidate() { Cache(); Apply(); }

    private void OnDisable() { root = null; }

    private void Update()
    {
        if (!Application.isPlaying) return;
        Cache();
        if (root == null) return;

        if (treeRebuilt) { treeRebuilt = false; Apply(); }

        if (!minimapZoomStarted && minimapCamera != null)
        {
            minimapZoomStarted = true;
            if (minimapStartZoomedOut)
                minimapCamera.orthographicSize = Mathf.Max(minimapZoomMin, minimapZoomMax);
        }

        float dt = Time.deltaTime;

        ReadPlayerHealth(dt);
        ReadPlayerMana(dt);

        if (health < prevH - 0.0001f) trailH.Restart(trailHoldDelay);
        if (mana < prevM - 0.0001f) trailM.Restart(trailHoldDelay);
        prevH = health; prevM = mana;

        Step(ref trailH, health / 100f, dt);
        Step(ref trailM, mana / 100f, dt);

        SetLevel(hTrail, trailH.Value); SetLevel(mTrail, trailM.Value);
        SetLevel(hLiquid, health / 100f); SetLevel(mLiquid, mana / 100f);
        SetGlobeNum(hNum, HealthValue);
        SetGlobeNum(mNum, ManaValue);

        swirlClock += dt * swirlSpeed;
        ApplySwirl();
        foreach (CssBox liquid in swirlBoxes)
        {
            if (liquid == null) continue;
            liquid.SetSwirlTime(swirlClock);
        }

        PushMinimap();

        if (minimapProfile) ProfileMinimap();
        else if (minimap != null) minimap.SkipRender = false;
    }

    private void PushMinimap()
    {
        if (minimap == null) return;
        minimap.Cam = minimapCamera;
        minimap.Follow = minimapFollowsPlayer;
        minimap.FollowHeight = minimapHeight;
        minimap.RefreshHz = minimapRefreshHz;
        minimap.ZoomMin = minimapZoomMin;
        minimap.ZoomMax = minimapZoomMax;
        minimap.ZoomStep = minimapZoomStep;

        minimap.pickingMode = minimapZoom ? PickingMode.Position : PickingMode.Ignore;
    }

    private void ProfileMinimap()
    {
        if (minimap == null) return;

        bool on = (profFrame / ProfBlock) % 2 == 0;
        minimap.SkipRender = !on;

        if (profFrame % ProfBlock >= ProfWarmup)
        {
            double ms = Time.unscaledDeltaTime * 1000.0;
            if (on) { profOnSum += ms; profOnCount++; } else { profOffSum += ms; profOffCount++; }
        }

        if (++profFrame % (ProfBlock * 2) != 0) return;

        double avgOn = profOnCount > 0 ? profOnSum / profOnCount : 0.0;
        double avgOff = profOffCount > 0 ? profOffSum / profOffCount : 0.0;
        double cost = avgOn - avgOff;
        double pct = avgOff > 0.0001 ? cost / avgOff * 100.0 : 0.0;

        Debug.Log($"[Minimap] with {avgOn:F2}ms ({1000.0 / System.Math.Max(avgOn, 0.0001):F0} fps) | " +
                  $"without {avgOff:F2}ms ({1000.0 / System.Math.Max(avgOff, 0.0001):F0} fps) | " +
                  $"cost {cost:F2}ms ({pct:F0}%) | Render() CPU {minimap.LastRenderMs:F2}ms | " +
                  $"{minimap.RenderCount} renders/{ProfBlock}f" +
                  (minimap.CameraSelfRenders ? "  [!] camera is ENABLED, so it renders regardless — disable it or this reads 0" : ""));

        profOnSum = profOffSum = 0.0; profOnCount = profOffCount = 0;
        minimap.ResetRenderCount();
    }

    private void Step(ref Trail t, float target, float dt)
    {
        if (target >= t.Value) { t.Snap(target); return; }

        if (t.Hold > 0f) { t.Hold -= dt; return; }

        t.Elapsed += dt;
        float d = Mathf.Max(trailDuration, 0.0001f);
        if (t.Elapsed >= d) { t.Snap(target); return; }
        t.Value = Mathf.Lerp(t.From, target, Ease(t.Elapsed / d));
    }

    private static float Ease(float t)
    {
        const float x1 = 0.4f, y1 = 0f, x2 = 0.2f, y2 = 1f;
        t = Mathf.Clamp01(t);

        float u = t;
        for (int i = 0; i < 6; i++)
        {
            float slope = BezierSlope(u, x1, x2);
            if (Mathf.Abs(slope) < 1e-6f) break;
            u = Mathf.Clamp01(u - (Bezier(u, x1, x2) - t) / slope);
        }
        return Bezier(u, y1, y2);
    }

    private static float Bezier(float u, float a, float b)
    {
        float v = 1f - u;
        return 3f * v * v * u * a + 3f * v * u * u * b + u * u * u;
    }

    private static float BezierSlope(float u, float a, float b)
    {
        float v = 1f - u;
        return 3f * v * v * a + 6f * v * u * (b - a) + 3f * u * u * (1f - b);
    }

    private void ReadPlayerHealth(float dt)
    {
        if (!bindPlayerHealth) return;

        if (playerHealth == null)
        {
            findCooldown -= dt;
            if (findCooldown > 0f) return;
            findCooldown = 1f;
            playerHealth = FindPlayerHealth();
            if (playerHealth == null) return;
            justBound = true;
        }

        health = playerHealth.MaxHealth > 0f
            ? Mathf.Clamp01(playerHealth.CurrentHealth / playerHealth.MaxHealth) * 100f
            : 0f;

        if (justBound)
        {
            justBound = false;
            prevH = health;
            trailH.Snap(health / 100f);
        }
    }

    private static Health FindPlayerHealth() => LocalPlayer.Get<Health>();

    private float HealthValue => (bindPlayerHealth && playerHealth != null)
        ? playerHealth.CurrentHealth
        : health;

    private void ReadPlayerMana(float dt)
    {
        if (!bindPlayerMana) return;

        if (playerMana == null)
        {
            findManaCooldown -= dt;
            if (findManaCooldown > 0f) return;
            findManaCooldown = 1f;
            playerMana = LocalPlayer.Get<ResourcePool>();
            if (playerMana == null) return;
            justBoundMana = true;
        }

        mana = playerMana.MaxValue > 0f
            ? Mathf.Clamp01(playerMana.CurrentValue / playerMana.MaxValue) * 100f
            : 0f;

        if (justBoundMana)
        {
            justBoundMana = false;
            prevM = mana;
            trailM.Snap(mana / 100f);
        }
    }

    private float ManaValue => (bindPlayerMana && playerMana != null)
        ? playerMana.CurrentValue
        : mana;

    private static void SetGlobeNum(Label label, float value)
    {
        if (label == null) return;
        string text = Mathf.RoundToInt(value).ToString("N0");
        if (label.text != text) label.text = text;
    }

    private static void SetGlobeNum(Label label, int value)
    {
        if (label == null) return;
        string text = value.ToString("N0");
        if (label.text != text) label.text = text;
    }

    [ContextMenu("Reset quest tracker")]
    public void ResetQuestTracker()
    {
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Reset quest tracker");
#endif
        questPlate = true;
        questParchment = QDefParchment;
        questInk = QDefInk;
        questAccent = QDefAccent;
        questBloom = QDefBloom;
        questAging = QDefAging;
        questDoneFade = QDefDoneFade;
        questRightPercent = QDefRightPercent;
        questTopOffset = QDefTopOffset;
        questWidth = QDefWidth;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        Apply();
    }

    private void ApplyRims()
    {
        SetRimVisible(globeRimGold, "globe-ring-edge");
        SetRimVisible(globeRimBrown, "globe-ring-dark");
        SetRimVisible(minimapRimGold, "mm-ring-edge");
        SetRimVisible(minimapRimBrown, "mm-ring-mid");

        float w = barRim ? 1.1f : 0f;
        root.Query<VisualElement>(className: "actionbar").ForEach(e =>
        {
            e.style.borderTopWidth = w;
            e.style.borderRightWidth = w;
            e.style.borderBottomWidth = w;
            e.style.borderLeftWidth = w;
        });

        ApplyCastShadows();
    }

    private void ApplyCastShadows()
    {
        float globeEdge = globeRimGold ? 69.3f : globeRimBrown ? 68.2f : 63.36f;

        root.Query<CssBox>(className: "globe-shadow")
            .ForEach(s => s.SetShadowShape(globeEdge, 103.4f - globeEdge));

        float mapEdge = minimapRimGold ? 93.005f : minimapRimBrown ? 92.895f : 87.065f;
        root.Query<CssBox>(className: "mm-shadow")
            .ForEach(s => s.SetShadowShape(mapEdge, 137.005f - mapEdge));

        float barHeight = 15.84f + (barRim ? 2.2f : 0f) + 50.6f;
        root.Query<VisualElement>(className: "actionbar-shadow")
            .ForEach(s => s.style.height = barHeight + 48.4f);
    }

    private void SetRimVisible(bool on, string cls)
    {
        DisplayStyle d = on ? DisplayStyle.Flex : DisplayStyle.None;
        root.Query<VisualElement>(className: cls).ForEach(e => e.style.display = d);
    }

    private void ApplyQuestPlate()
    {
        CssBox plate = root.Q<CssBox>("quests");
        if (plate == null) return;

        Color near = questPlate ? questParchment : Color.clear;
        Color far = questPlate
            ? new Color(questParchment.r * QFarTint, questParchment.g * QFarTint,
                        questParchment.b * QFarTint, questParchment.a * QFarAlpha)
            : Color.clear;

        plate.SetBaseGradient(near, far);

        Color aged = questPlate ? new Color(far.r, far.g, far.b, questAging) : Color.clear;
        Color lit = questPlate ? new Color(0.992f, 0.980f, 0.941f, questBloom) : Color.clear;
        plate.SetOverlay1Colors(new Color(aged.r, aged.g, aged.b, 0f), aged);
        plate.SetOverlay2Colors(lit, new Color(lit.r, lit.g, lit.b, 0f));

        plate.style.borderTopWidth = 0f;
        plate.style.borderRightWidth = 0f;
        plate.style.borderBottomWidth = 0f;
        plate.style.borderLeftWidth = 0f;
        plate.style.borderTopLeftRadius = 0f;
        plate.style.borderTopRightRadius = 0f;
        plate.style.borderBottomLeftRadius = 0f;
        plate.style.borderBottomRightRadius = 0f;

        VisualElement tracker = plate.parent;
        if (tracker != null)
        {
            tracker.style.left = StyleKeyword.Auto;
            tracker.style.right = Length.Percent(questRightPercent);
            tracker.style.marginTop = questTopOffset;
            tracker.style.width = questWidth;

            DisplayStyle chrome = questPlate ? DisplayStyle.Flex : DisplayStyle.None;
            foreach (string cls in new[] { "plate-shadow", "q-spine", "q-rule" })
                tracker.Query<VisualElement>(className: cls).ForEach(e => e.style.display = chrome);

            tracker.Query<CssBox>(className: "q-spine")
                   .ForEach(s => s.SetBaseGradient(questAccent, questAccent));

            Color rule = new Color(questInk.r, questInk.g, questInk.b, questInk.a * 0.75f);
            tracker.Query<CssBox>(className: "q-rule")
                   .ForEach(r => r.SetBaseGradient(rule, new Color(rule.r, rule.g, rule.b, 0f)));

            tracker.Query<CssBox>(className: "q-mark").ForEach(m =>
            {
                Color c = IsDone(m) ? questAccent : questInk;
                m.SetBaseGradient(c, c);
            });
        }

        ApplyQuestText();
    }

    private void ApplyQuestText()
    {
        root.Query<Label>(className: "q-title").ForEach(l =>
        {
            l.style.color = questInk;
            QuestTextFx(l);
        });

        root.Query<Label>(className: "q-text").ForEach(l =>
        {
            Color c = questInk;
            if (IsDone(l)) c.a *= questDoneFade;
            l.style.color = c;
            QuestTextFx(l);
        });

        root.Query<Label>(className: "q-count").ForEach(l =>
        {
            l.style.color = questInk;
            QuestTextFx(l);
        });

        Color strike = questInk;
        strike.a *= questDoneFade;
        root.Query<VisualElement>(className: "q-strike").ForEach(s => s.style.backgroundColor = strike);
    }

    private static bool IsDone(VisualElement e)
    {
        for (VisualElement v = e.parent; v != null; v = v.parent)
        {
            if (v.ClassListContains("q-done")) return true;
            if (v.ClassListContains("q-obj")) return false;
        }
        return false;
    }

    private void QuestTextFx(Label l)
    {
        l.style.textShadow = new TextShadow
        {
            offset = new Vector2(0f, 1.1f),
            blurRadius = questPlate ? 0f : 3.3f,
            color = questPlate ? new Color(0f, 0f, 0f, 0f) : new Color(0f, 0f, 0f, 0.95f)
        };
        l.style.unityTextOutlineWidth = questPlate ? 0f : 0.6f;
    }

    private void Cache()
    {
        if (doc == null) doc = GetComponent<UIDocument>();
        VisualElement r = doc != null ? doc.rootVisualElement : null;
        if (r == null) { root = null; return; }
        if (r != root || hLiquid == null)
        {
            treeRebuilt = true;
            root = r;
            hLiquid = root.Q<CssBox>("health-liquid");
            mLiquid = root.Q<CssBox>("mana-liquid");
            hTrail = root.Q<CssBox>("health-trail");
            mTrail = root.Q<CssBox>("mana-trail");
            hNum = root.Q<Label>("health-num");
            mNum = root.Q<Label>("mana-num");
            minimap = root.Q<MinimapView>("minimap-surface");
            screenX = root.Q<VisualElement>("screen-x");
            belt = root.Q<VisualElement>("belt");
            essence = root.Q<VisualElement>("essence");
            xpBarBox = root.Q<CssBox>("xpbar");
            xpFillBox = root.Q<CssBox>("xp-fill");
            xpTicksBox = root.Q<VisualElement>("xp-ticks");

            globeEdges.Clear();
            root.Query<VisualElement>(className: "globe-ring-edge").ForEach(e => globeEdges.Add(e));

            swirlBoxes.Clear();
            root.Query<CssBox>(className: "liquid").ForEach(b => swirlBoxes.Add(b));

            root.pickingMode = PickingMode.Ignore;
            root.Query<VisualElement>().ForEach(e => e.pickingMode = PickingMode.Ignore);

            PushMinimap();
        }
    }

    private void Apply()
    {
        if (root == null) return;

        SetLevel(hLiquid, health / 100f);
        SetLevel(mLiquid, mana / 100f);

        if (health / 100f > trailH.Value) trailH.Snap(health / 100f);
        if (mana / 100f > trailM.Value) trailM.Snap(mana / 100f);
        SetLevel(hTrail, trailH.Value);
        SetLevel(mTrail, trailM.Value);
        SetGlobeNum(hNum, HealthValue);
        SetGlobeNum(mNum, ManaValue);
        ApplySwirl();

        foreach (CssBox liquid in swirlBoxes)
            liquid?.SetSurface(globeSurfaceStrength, globeSurfaceWidth);

        root.Query<CssBox>(className: "globe-clip").ForEach(b => b.SetBaseGradient(globeWell, globeWell));

        if (screenX != null)
            screenX.style.display = screenCentreX ? DisplayStyle.Flex : DisplayStyle.None;

        PushMinimap();

        ApplyFont(displayFont, "globe-num");
        ApplyFont(bodyFont, "globe-label");
        ApplyFont(bindingFont, "slot-key");
        ApplyFont(essenceFont, "ess-amount");
        root.Query<Label>(className: "globe-label").ForEach(l => l.style.letterSpacing = labelSpacing);
        root.Query<Label>(className: "ess-amount").ForEach(l => SetGlobeNum(l, essAmount));
        root.Query<Label>(className: "ess-amount").ForEach(l => l.style.color = essColor);
        ApplyEssenceOrb();
        ApplyMenuIcons();
        ApplyQuestPlate();
        ApplyXpBar();
        ApplyPotionBelt();
        ApplyRims();
        DisplayStyle tracery = barTracery ? DisplayStyle.Flex : DisplayStyle.None;
        foreach (string cls in new[] { "bar-tracery", "bar-inlay", "bar-inlay-frame" })
            root.Query<VisualElement>(className: cls).ForEach(e => e.style.display = tracery);
        if (!string.IsNullOrEmpty(zoneName))
            root.Query<Label>(className: "mm-zone").ForEach(l => l.text = zoneName.ToUpperInvariant());
        StyleTextShadow zoneTs = zoneTextShadow && shadows
            ? new StyleTextShadow(StyleKeyword.Null)
            : new StyleTextShadow(new TextShadow { offset = Vector2.zero, blurRadius = 0f, color = new Color(0f, 0f, 0f, 0f) });
        root.Query<Label>(className: "mm-zone").ForEach(l => l.style.textShadow = zoneTs);
        root.Query<CssBox>(className: "actionbar").ForEach(b => b.SetBaseGradient(barTop, barBottom));
        root.Query<CssBox>(className: "slot-fill").ForEach(s => s.SetBaseGradient(slotTop, slotBottom));

        List<VisualElement> slotList = root.Query<VisualElement>(className: "slot").ToList();
        for (int i = 0; i < slotList.Count; i++)
        {
            Texture2D tex = (slotIcons != null && i < slotIcons.Length) ? slotIcons[i] : null;
            VisualElement art = slotList[i].Q<VisualElement>(className: "slot-art");
            Label plus = slotList[i].Q<Label>(className: "slot-icon");
            if (art != null)
            {
                if (tex != null)
                {
                    art.style.backgroundImage = new StyleBackground(tex);
                    art.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
                }
                art.style.display = tex != null ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (plus != null) plus.style.display = tex != null ? DisplayStyle.None : DisplayStyle.Flex;
        }

        DisplayStyle sd = shadows ? DisplayStyle.Flex : DisplayStyle.None;
        foreach (string cls in new[] { "globe-shadow", "mm-shadow", "actionbar-shadow", "ess-orb-shadow", "mbtn-art-shadow", "belt-shadow" })
            root.Query<VisualElement>(className: cls).ForEach(e => e.style.display = sd);
        foreach (string cls in new[] { "globe-shadow", "mm-shadow", "actionbar-shadow", "belt-shadow" })
            root.Query<VisualElement>(className: cls).ForEach(e => e.style.opacity = shadowOpacity);
        StyleTextShadow ts = shadows
            ? new StyleTextShadow(StyleKeyword.Null)
            : new StyleTextShadow(new TextShadow { offset = Vector2.zero, blurRadius = 0f, color = new Color(0f, 0f, 0f, 0f) });
        root.Query<Label>(className: "ess-amount").ForEach(l => l.style.textShadow = ts);

        if (doc.panelSettings != null) doc.panelSettings.scale = uiScale * UiScaleBaseline;

        foreach (VisualElement rim in globeEdges)
        {
            if (rim == null) continue;
            rim.style.borderTopWidth = globeRimWidth;
            rim.style.borderRightWidth = globeRimWidth;
            rim.style.borderBottomWidth = globeRimWidth;
            rim.style.borderLeftWidth = globeRimWidth;
            rim.style.borderTopColor = globeRimColor;
            rim.style.borderRightColor = globeRimColor;
            rim.style.borderBottomColor = globeRimColor;
            rim.style.borderLeftColor = globeRimColor;
        }
    }

    private void ApplySwirl()
    {
        foreach (CssBox liquid in swirlBoxes)
        {
            if (liquid == null) continue;
            liquid.SetSwirlOn(liquidSwirl);
            liquid.SetSwirlStrength(swirlStrength);
            liquid.SetSwirlWarp(swirlWarp);
        }
    }

    private static readonly Vector3 EssHighlight = new Vector3(0.949f, 0.896f, 0.831f);
    private static readonly Vector3 EssShadow    = new Vector3(0.662f, 0.643f, 0.578f);

    private void ApplyEssenceOrb()
    {
        if (essence != null) essence.style.display = essenceOrb ? DisplayStyle.Flex : DisplayStyle.None;

        Color hi = new Color(essColor.r + EssHighlight.x * (1f - essColor.r),
                             essColor.g + EssHighlight.y * (1f - essColor.g),
                             essColor.b + EssHighlight.z * (1f - essColor.b), 1f);
        Color lo = new Color(essColor.r * EssShadow.x,
                             essColor.g * EssShadow.y,
                             essColor.b * EssShadow.z, 1f);
        Color loClear = new Color(lo.r, lo.g, lo.b, 0f);
        root.Query<CssBox>(className: "ess-orb-face").ForEach(b =>
        {
            b.SetBaseGradient(hi, essColor);
            b.SetOverlay1Colors(loClear, lo);
        });

        Color halo = new Color(essColor.r, essColor.g, essColor.b, essGlow);
        root.Query<CssBox>(className: "ess-orb-glow").ForEach(b => b.SetShadowColor(halo));

        root.Query<VisualElement>(className: "ess-art")
            .ForEach(e => e.style.unityBackgroundImageTintColor = lo);
    }

    private static readonly Color[] BeltFallbackTints =
    {
        new Color(176f / 255f,  24f / 255f,  32f / 255f),
        new Color( 31f / 255f, 111f / 255f, 196f / 255f),
        new Color(103f / 255f, 196f / 255f, 111f / 255f),
        new Color(216f / 255f, 207f / 255f, 166f / 255f),
    };

    private void ApplyPotionBelt()
    {
        if (belt == null) return;
        belt.style.display = potionBelt ? DisplayStyle.Flex : DisplayStyle.None;
        belt.style.marginTop = beltVerticalOffset;

        List<VisualElement> slots = belt.Query<VisualElement>(className: "belt-slot").ToList();
        for (int i = 0; i < slots.Count; i++)
        {
            VisualElement slot = slots[i];

            if (beltCounts != null && i < beltCounts.Length)
            {
                int n = beltCounts[i];
                Label count = slot.Q<Label>(className: "belt-count");
                if (count != null) count.text = n.ToString();
                slot.EnableInClassList("belt-out", n <= 0);
            }

            if (beltCooldowns != null && i < beltCooldowns.Length)
            {
                CssBox cd = slot.Q<CssBox>(className: "belt-cooldown");
                if (cd != null) cd.SetSweep(beltCooldowns[i]);
            }

            Texture2D tex = (beltIcons != null && i < beltIcons.Length) ? beltIcons[i] : null;

            CssBox pool = slot.Q<CssBox>(className: "belt-pool");
            if (pool != null)
            {
                Color tint = BeltFallbackTints[i % BeltFallbackTints.Length];
                if (beltTintFromIcon && tex != null && !TryIconColor(tex, out tint))
                    tint = BeltFallbackTints[i % BeltFallbackTints.Length];

                float lum = 0.2126f * tint.r + 0.7152f * tint.g + 0.0722f * tint.b;
                float a = Mathf.Clamp01(beltPoolStrength * (0.756f - 0.342f * lum));
                pool.SetBaseGradient(new Color(tint.r, tint.g, tint.b, a),
                                     new Color(tint.r, tint.g, tint.b, 0f));
            }

            VisualElement art = slot.Q<VisualElement>(className: "belt-art");
            Label glyph = slot.Q<Label>(className: "belt-icon");
            if (art != null)
            {
                if (tex != null)
                {
                    art.style.backgroundImage = new StyleBackground(tex);
                    art.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
                }
                art.style.display = tex != null ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (glyph != null) glyph.style.display = tex != null ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }

    private static bool TryIconColor(Texture2D tex, out Color color)
    {
        color = Color.white;
        if (tex == null) return false;
        if (!tex.isReadable)
        {
            if (warnedUnreadable.Add(tex.GetInstanceID()))
                Debug.LogWarning($"[HudController] Potion belt: '{tex.name}' has Read/Write Enabled off, " +
                                 "so its colour can't be sampled — that socket is using its palette " +
                                 "colour. Enable it on the texture's importer, or switch off " +
                                 "Belt Tint From Icon.", tex);
            return false;
        }

        Color32[] px;
        try { px = tex.GetPixels32(); }
        catch { return false; }
        if (px == null || px.Length == 0) return false;

        int stride = Mathf.Max(1, px.Length / 4096);
        double r = 0d, g = 0d, b = 0d, wsum = 0d;
        for (int i = 0; i < px.Length; i += stride)
        {
            Color32 c = px[i];
            float a = c.a / 255f;
            if (a < 0.1f) continue;
            float mx = Mathf.Max(c.r, Mathf.Max(c.g, c.b)) / 255f;
            float mn = Mathf.Min(c.r, Mathf.Min(c.g, c.b)) / 255f;
            float sat = mx > 0f ? (mx - mn) / mx : 0f;
            float w = a * (0.15f + sat);
            r += c.r * w; g += c.g * w; b += c.b * w; wsum += w;
        }
        if (wsum <= 0d) return false;

        color = new Color((float)(r / wsum) / 255f, (float)(g / wsum) / 255f, (float)(b / wsum) / 255f, 1f);

        Color.RGBToHSV(color, out float h, out float s, out float v);
        color = Color.HSVToRGB(h, Mathf.Max(s, 0.45f), Mathf.Max(v, 0.75f));
        return true;
    }

    private void ApplyXpBar()
    {
        if (xpBarBox != null)
        {
            xpBarBox.style.display = xpBar ? DisplayStyle.Flex : DisplayStyle.None;
            xpBarBox.style.height = xpBarHeight;
            xpBarBox.SetBaseGradient(
                new Color(XpPlateTop.r, XpPlateTop.g, XpPlateTop.b, XpPlateTop.a * xpBackfillOpacity),
                new Color(XpPlateBottom.r, XpPlateBottom.g, XpPlateBottom.b, XpPlateBottom.a * xpBackfillOpacity));
        }
        SetLevel(xpFillBox, xpFill);
        if (xpFillBox != null)
        {
            Color hi = new Color(xpColor.r + XpHighlight.x * (1f - xpColor.r),
                                 xpColor.g + XpHighlight.y * (1f - xpColor.g),
                                 xpColor.b + XpHighlight.z * (1f - xpColor.b), 1f);
            Color lo = new Color(xpColor.r * XpShadow.x,
                                 xpColor.g * XpShadow.y,
                                 xpColor.b * XpShadow.z, 1f);
            Color loClear = new Color(lo.r, lo.g, lo.b, 0f);
            xpFillBox.SetBaseGradient(hi, new Color(xpColor.r, xpColor.g, xpColor.b, 1f));
            xpFillBox.SetOverlay1Colors(loClear, lo);
        }
        if (xpTicksBox != null)
            xpTicksBox.style.display = xpTicks ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private static void SetLevel(CssBox box, float fill01)
    {
        if (box != null) box.SetLevel(fill01);
    }

    private void ApplyMenuIcons()
    {
        root.Query<VisualElement>(className: "mbtn").ForEach(btn =>
        {
            VisualElement art = btn.Q<VisualElement>(className: "mbtn-art");
            if (art == null) return;
            art.style.unityBackgroundImageTintColor = menuIconColor;

            if (btn.userData is bool) return;
            btn.userData = true;
            btn.RegisterCallback<PointerEnterEvent>(_ =>
            {
                VisualElement a = btn.Q<VisualElement>(className: "mbtn-art");
                if (a != null) a.style.unityBackgroundImageTintColor = menuIconHoverColor;
            });
            btn.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                VisualElement a = btn.Q<VisualElement>(className: "mbtn-art");
                if (a != null) a.style.unityBackgroundImageTintColor = menuIconColor;
            });
        });
    }

    private void ApplyFont(FontAsset font, string className)
    {
        if (font == null || root == null) return;
        StyleFontDefinition def = new StyleFontDefinition(FontDefinition.FromSDFFont(font));
        root.Query<Label>(className: className).ForEach(l => l.style.unityFontDefinition = def);
    }
}
