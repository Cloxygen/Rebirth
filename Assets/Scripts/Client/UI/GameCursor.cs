using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

using Cursor = UnityEngine.Cursor;

[DisallowMultipleComponent]
public class GameCursor : MonoBehaviour
{
    [Header("Cursor art")]
    public Texture2D arrow;
    public Texture2D hand;

    [Header("Colour")]
    public Color tint = Palette.MetalHi;
    public Color tooFarTint = new Color(0.55f, 0.54f, 0.58f, 1f);
    public Vector2 hotspot = new Vector2(8f, 8f);

    [Header("Hold ring")]
    public bool showRing = true;
    public float ringDiameter = 30f;
    public float ringThickness = 6f;
    public Vector2 ringOffset = Vector2.zero;
    public Color ringFill = Palette.Gold;
    public Color ringTrack = new Color(0x10 / 255f, 0x0c / 255f, 0x17 / 255f, 1f);

    private PlayerInteract pickup;

    private UIDocument doc;
    private VisualElement root;
    private VisualElement ringRoot;
    private CssBox ringTrackBox;
    private CssBox ringFillBox;
    private bool ringStyled;
    private bool cursorHiddenByRing;

    private RingSignature styledRing;

    private const float PlateBorder = 1f;

    private struct RingSignature
    {
        public float Diameter, Thickness;
        public Color TrackColor, FillColor;
    }

    private enum Look { None, Arrow, Hand, HandFar }
    private Look appliedLook = Look.None;
    private Texture2D appliedTexture;
    private Vector2 appliedHotspot;

    private readonly Tinted tintedArrow = new Tinted();
    private readonly Tinted tintedHand = new Tinted();
    private readonly Tinted tintedHandFar = new Tinted();

    private class Tinted
    {
        public Texture2D Texture;
        public bool Changed;

        private Texture2D source;
        private Color color;
        private Color32[] buffer;

        public Texture2D Get(Texture2D src, Color c)
        {
            Changed = false;
            if (src == null) return null;
            if (Texture != null && src == source && c == color) return Texture;

            source = src;
            color = c;
            Changed = true;

            if (!src.isReadable)
            {
                Debug.LogError($"GameCursor: '{src.name}' needs Read/Write Enabled in its " +
                               "import settings to be tinted. Using it untinted.");
                Texture = src;
                return Texture;
            }

            if (Texture == null || Texture == src || Texture.width != src.width || Texture.height != src.height)
            {
                Texture = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.DontSave,
                    filterMode = src.filterMode,
                    name = src.name + " (tinted)"
                };
                buffer = null;
            }

            Color32[] pixels = src.GetPixels32();
            if (buffer == null || buffer.Length != pixels.Length) buffer = new Color32[pixels.Length];

            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 p = pixels[i];
                buffer[i] = new Color32(
                    (byte)Mathf.Clamp(p.r * c.r, 0f, 255f),
                    (byte)Mathf.Clamp(p.g * c.g, 0f, 255f),
                    (byte)Mathf.Clamp(p.b * c.b, 0f, 255f),
                    (byte)Mathf.Clamp(p.a * c.a, 0f, 255f));
            }

            Texture.SetPixels32(buffer);
            Texture.Apply(false);
            return Texture;
        }

        public void Dispose()
        {
            if (Texture != null && Texture != source) UnityEngine.Object.Destroy(Texture);
            Texture = null;
            source = null;
            buffer = null;
        }
    }

    private void OnEnable()
    {
        doc = GetComponent<UIDocument>();
    }

    private void OnDisable()
    {
        SetCursorHidden(false);
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        appliedLook = Look.None;
        appliedTexture = null;

        tintedArrow.Dispose();
        tintedHand.Dispose();
        tintedHandFar.Dispose();
        if (ringRoot != null) ringRoot.style.display = DisplayStyle.None;
    }

    private void LateUpdate()
    {
        ResolvePickup();

        PlayerInteract.Hover hover = pickup != null ? pickup.HoverState : PlayerInteract.Hover.None;

        bool pointerLive = Cursor.lockState == CursorLockMode.None;

        ApplyLook(!pointerLive ? Look.Arrow
                : hover == PlayerInteract.Hover.InReach ? Look.Hand
                : hover == PlayerInteract.Hover.TooFar ? Look.HandFar
                : Look.Arrow);

        UpdateRing(pointerLive);
    }

    private void ResolvePickup()
    {
        pickup = LocalPlayer.Get<PlayerInteract>();
    }

    private void ApplyLook(Look look)
    {
        Texture2D source = look == Look.Arrow ? arrow : (hand != null ? hand : arrow);
        Color color = look == Look.HandFar ? tooFarTint : tint;

        Tinted slot = look == Look.Hand ? tintedHand
                    : look == Look.HandFar ? tintedHandFar
                    : tintedArrow;
        Texture2D texture = slot.Get(source, color);

        if (!slot.Changed && look == appliedLook && texture == appliedTexture
                          && hotspot == appliedHotspot) return;

        appliedLook = look;
        appliedTexture = texture;
        appliedHotspot = hotspot;

        Cursor.SetCursor(texture, texture != null ? hotspot : Vector2.zero, CursorMode.Auto);
    }

    private void UpdateRing(bool pointerLive)
    {
        float progress = pickup != null ? pickup.HoldProgress : 0f;

        bool visible = showRing && pointerLive && progress > 0f && Mouse.current != null;

        SetCursorHidden(visible);

        if (!visible)
        {
            if (ringRoot != null) ringRoot.style.display = DisplayStyle.None;
            return;
        }

        if (!CacheTree()) return;

        StyleRing();
        ringRoot.style.display = DisplayStyle.Flex;
        ringFillBox.SetSweep(progress);

        float panelW = root.resolvedStyle.width;
        float panelH = root.resolvedStyle.height;
        if (float.IsNaN(panelW) || panelW < 1f || float.IsNaN(panelH) || panelH < 1f) return;

        Vector2 screen = Mouse.current.position.ReadValue();
        float x = screen.x / Screen.width * panelW + ringOffset.x;
        float y = (1f - screen.y / Screen.height) * panelH + ringOffset.y;

        ringRoot.style.left = Mathf.Round(x - styledRing.Diameter * 0.5f);
        ringRoot.style.top = Mathf.Round(y - styledRing.Diameter * 0.5f);
    }

    private void SetCursorHidden(bool hidden)
    {
        if (hidden != cursorHiddenByRing)
        {
            cursorHiddenByRing = hidden;
            Cursor.visible = !hidden;
            return;
        }

        if (hidden && Cursor.visible) Cursor.visible = false;
    }

    private bool CacheTree()
    {
        if (doc == null) doc = GetComponent<UIDocument>();
        VisualElement r = doc != null ? doc.rootVisualElement : null;
        if (r == null) return false;
        if (r == root && ringRoot != null) return true;

        root = r;

        ringRoot = new VisualElement { name = "cursor-ring" };
        ringRoot.pickingMode = PickingMode.Ignore;
        ringRoot.style.position = Position.Absolute;

        ringTrackBox = NewRingBox("plate-track");
        ringFillBox = NewRingBox("plate-fill");
        ringRoot.Add(ringTrackBox);
        ringRoot.Add(ringFillBox);

        root.Add(ringRoot);
        ringStyled = false;
        return true;
    }

    private CssBox NewRingBox(string plateClass)
    {
        CssBox box = new CssBox();
        box.pickingMode = PickingMode.Ignore;
        box.AddToClassList(plateClass);
        box.style.position = Position.Absolute;

        box.style.right = StyleKeyword.Auto;
        box.style.bottom = StyleKeyword.Auto;
        return box;
    }

    private void StyleRing()
    {
        RingSignature sig = new RingSignature
        {
            Diameter = Mathf.Max(6f, ringDiameter),
            Thickness = Mathf.Max(1f, ringThickness),
            TrackColor = ringTrack,
            FillColor = ringFill
        };

        if (ringStyled && sig.Equals(styledRing)) return;
        ringStyled = true;
        styledRing = sig;

        float radius = sig.Diameter * 0.5f;
        ringRoot.style.width = sig.Diameter;
        ringRoot.style.height = sig.Diameter;

        Band(ringTrackBox, radius, radius, sig.Thickness);

        Band(ringFillBox, radius, radius - PlateBorder, sig.Thickness - PlateBorder * 2f);

        ringTrackBox.SetBaseGradient(sig.TrackColor, sig.TrackColor);
        ringFillBox.SetBaseGradient(sig.FillColor, sig.FillColor);

        ringTrackBox.SetSweep(1f);
    }

    private void Band(CssBox box, float widgetRadius, float outerRadius, float width)
    {
        if (width <= 0f || outerRadius <= 0f)
        {
            box.style.display = DisplayStyle.None;
            return;
        }

        box.style.display = DisplayStyle.Flex;

        float inset = widgetRadius - outerRadius;
        float size = outerRadius * 2f;

        box.style.left = inset;
        box.style.top = inset;
        box.style.width = size;
        box.style.height = size;
        box.style.borderTopLeftRadius = outerRadius;
        box.style.borderTopRightRadius = outerRadius;
        box.style.borderBottomLeftRadius = outerRadius;
        box.style.borderBottomRightRadius = outerRadius;
        box.SetHole(Mathf.Max(0f, outerRadius - width));
    }

#if UNITY_EDITOR
    private void Reset()
    {
        arrow = LoadCursor("cursor_arrow");
        hand = LoadCursor("cursor_hand");
    }

    private static Texture2D LoadCursor(string file)
    {
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/Textures/UI/cursor/" + file + ".png");
    }
#endif
}
