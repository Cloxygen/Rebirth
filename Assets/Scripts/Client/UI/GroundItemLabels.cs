using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public class GroundItemLabels : MonoBehaviour
{
    [Header("Labels")]
    public bool show = true;
    public float maxDistance = 16f;
    public float screenOffset = 14f;
    [Range(1, 60)] public int maxLabels = 24;

    [Header("Type")]
    public FontAsset font;
    public float fontSize = 13f;
    public float outlineWidth = 0.55f;
    public Color outlineColor = new Color(0f, 0f, 0f, 0.85f);

    [Header("Colour")]
    [Range(0.2f, 2.0f)] public float inReachBrightness = 1.0f;
    [Range(0.2f, 2.0f)] public float tooFarBrightness = 0.55f;
    [Range(0.2f, 2.0f)] public float hoveredBrightness = 1.45f;

    private static readonly List<GroundItem> Items = new List<GroundItem>();

    public static void Register(GroundItem item)
    {
        if (item != null && !Items.Contains(item)) Items.Add(item);
    }

    public static void Unregister(GroundItem item) => Items.Remove(item);

    private UIDocument doc;
    private VisualElement root;
    private VisualElement layer;
    private Camera cam;
    private PlayerInteract pickup;

    private readonly List<Entry> pool = new List<Entry>();

    private class Entry
    {
        public Label Label;
        public GroundItem Owner;
        public string Text;
        public Color Color;
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

        ResolvePickup();

        for (int i = 0; i < pool.Count; i++) pool[i].Claimed = false;

        if (show && pickup != null)
        {
            IInteractable hovered = pickup.TargetedItem;
            Transform player = pickup.transform;
            Vector3 playerPosition = player.position;
            float cullSqr = maxDistance * maxDistance;

            int used = 0;
            for (int i = 0; i < Items.Count && used < maxLabels; i++)
            {
                GroundItem item = Items[i];
                if (item == null) continue;

                float distanceSqr = (item.transform.position - playerPosition).sqrMagnitude;
                if (distanceSqr > cullSqr) continue;

                Vector3 screenPoint = cam.WorldToScreenPoint(item.transform.position);
                if (screenPoint.z <= 0f) continue;

                string text = item.DisplayName;
                if (string.IsNullOrEmpty(text)) continue;

                Entry entry = Claim(item);
                if (entry == null) break;

                float brightness = ReferenceEquals(hovered, item) ? hoveredBrightness
                                 : item.IsInRange(player) ? inReachBrightness
                                 : tooFarBrightness;

                Color color = Shade(item.Rarity.ToColor(), brightness);

                Apply(entry, text, color);
                PlaceLabel(entry, screenPoint);
                used++;
            }
        }

        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i].Claimed) continue;
            pool[i].Owner = null;
            pool[i].Label.style.display = DisplayStyle.None;
        }
    }

    private void ResolvePickup()
    {
        pickup = LocalPlayer.Get<PlayerInteract>();
    }

    private Entry Claim(GroundItem owner)
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i].Owner != owner) continue;
            pool[i].Claimed = true;
            pool[i].Label.style.display = DisplayStyle.Flex;
            return pool[i];
        }

        Entry free = null;
        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i].Claimed || pool[i].Owner != null) continue;
            free = pool[i];
            break;
        }

        if (free == null)
        {
            if (pool.Count >= maxLabels) return null;
            free = Build();
            pool.Add(free);
        }

        free.Owner = owner;
        free.Claimed = true;
        free.Text = null;
        free.Label.style.display = DisplayStyle.Flex;
        return free;
    }

    private static Color Shade(Color rarity, float brightness)
    {
        return new Color(
            Mathf.Clamp01(rarity.r * brightness),
            Mathf.Clamp01(rarity.g * brightness),
            Mathf.Clamp01(rarity.b * brightness),
            rarity.a);
    }

    private Entry Build()
    {
        Label label = new Label { pickingMode = PickingMode.Ignore };
        label.style.position = Position.Absolute;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        label.style.whiteSpace = WhiteSpace.NoWrap;
        layer.Add(label);
        return new Entry { Label = label };
    }

    private void Apply(Entry entry, string text, Color color)
    {
        if (entry.Text != text)
        {
            entry.Text = text;
            entry.Label.text = text;
        }

        if (entry.Color != color)
        {
            entry.Color = color;
            entry.Label.style.color = color;
        }

        entry.Label.style.fontSize = fontSize;
        entry.Label.style.unityTextOutlineWidth = outlineWidth;
        entry.Label.style.unityTextOutlineColor = outlineColor;
        if (font != null)
        {
            entry.Label.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromSDFFont(font));
        }
    }

    private void PlaceLabel(Entry entry, Vector3 screenPoint)
    {
        float panelW = root.resolvedStyle.width;
        float panelH = root.resolvedStyle.height;
        if (float.IsNaN(panelW) || panelW < 1f || float.IsNaN(panelH) || panelH < 1f) return;

        float x = screenPoint.x / Screen.width * panelW;
        float y = (1f - screenPoint.y / Screen.height) * panelH;

        float width = entry.Label.resolvedStyle.width;
        if (float.IsNaN(width)) width = 0f;
        float height = entry.Label.resolvedStyle.height;
        if (float.IsNaN(height)) height = fontSize;

        entry.Label.style.left = Mathf.Round(x - width * 0.5f);
        entry.Label.style.top = Mathf.Round(y - height - screenOffset);
    }

    private bool CacheTree()
    {
        if (doc == null) doc = GetComponent<UIDocument>();
        VisualElement r = doc != null ? doc.rootVisualElement : null;
        if (r == null) return false;
        if (r == root && layer != null) return true;

        root = r;
        layer = new VisualElement { name = "ground-item-labels" };
        layer.pickingMode = PickingMode.Ignore;

        root.Insert(0, layer);

        pool.Clear();
        layer.Clear();
        return true;
    }
}
