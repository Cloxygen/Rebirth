using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
[ExecuteAlways]
[RequireComponent(typeof(UIDocument))]
public class InventoryWidget : MonoBehaviour
{
    [Serializable]
    public class SlotIcon
    {
        public SlotId slot;
        public Texture2D texture;
    }

    private const float IDefCell = 45.4f;
    private const float IDefGap = 0f;
    private const float IDefPadding = 18f;
    private const float IDefBorder = 1.1f;
    private const float IDefEdge = 0f;
    private const float IDefGridLine = 1f;
    private const float IDefEssenceSize = 15f;
    private const float IDefEssenceIcon = 1.45f;
    private const float IDefSlot = 64f;
    private const float IDefSlotGap = 12f;
    private const float IDefSlotIconSize = 0.62f;
    private const float IDefSlotIconOpacity = 0.55f;
    private const float IDefControlHeight = 20f;
    private const float IDefSearchDim = 0.1f;
    private const float CaretBlink = 0.53f;
    private const float IDefBloom = 0f;
    private const float IDefVignette = 0.3f;
    private const float IDefRarityTint = 0.16f;
    private static readonly Color IDefPlateTop = new Color(0.0849f, 0.0849f, 0.0849f, 1f);
    private static readonly Color IDefPlateBottom = new Color(0.1216f, 0.1098f, 0.0902f, 0.784f);
    private static readonly Color IDefBorderColor = Palette.MetalLine;
    private static readonly Color IDefTitle = new Color(0.541f, 0.498f, 0.420f, 1f);
    private static readonly Color IDefLabel = new Color(0.553f, 0.522f, 0.463f, 1f);
    private static readonly Color IDefTile  = new Color(0.106f, 0.086f, 0.063f, 1f);
    private static readonly Color IDefAccent = new Color(0.792f, 0.737f, 0.623f, 1f);
    private static readonly Color IDefWell = Palette.MetalLine;
    private static readonly Color IDefCellFill = new Color(0.1216f, 0.1098f, 0.0902f, 1f);
    private static readonly Color IDefCellLight = new Color(0.1226f, 0.1086f, 0.0885f, 1f);
    private static readonly Color IDefCellLine = new Color(0.2925f, 0.2925f, 0.2925f, 0.0941f);
    private static readonly Color IDefValid = Fade(Palette.Buff, 0.30f);
    private static readonly Color IDefInvalid = Fade(Palette.HpHi, 0.32f);
    private static readonly Color IDefSwap = Fade(Palette.Gold, 0.30f);
    private static readonly Color BloomTint = new Color(0.992f, 0.980f, 0.941f, 1f);

    private const float SocketEdge = 1.1f;

    public bool show = true;

    [Header("Opening")]
    [SerializeField] private InputAction toggleAction;
    public bool openOnStart;
    public bool previewInEditor;
    public Vector2Int previewGrid = new Vector2Int(6, 10);

    [Header("Grid")]
    [Range(16f, 72f)] public float cellSize = IDefCell;
    [Range(0.25f, 3f)] public float gridLineWidth = IDefGridLine;
    [Range(0f, 8f)] public float cellGap = IDefGap;
    public Color cellColor = IDefCellFill;
    public Color cellLightColor = IDefCellLight;
    public Color cellLineColor = IDefCellLine;
    public Color wellColor = IDefWell;

    [Header("Bag controls")]
    public bool showSearch = true;
    public bool showSort = true;
    [Range(14f, 34f)] public float controlHeight = IDefControlHeight;
    [Range(0f, 1f)] public float searchDim = IDefSearchDim;
    public Texture2D sortIcon;

    [Header("Equipment")]
    public bool showEquipment = true;
    [Range(24f, 96f)] public float slotSize = IDefSlot;
    [Range(0f, 32f)] public float slotGap = IDefSlotGap;
    public bool showSlotLabels;
    public SlotIcon[] slotIcons = Array.Empty<SlotIcon>();
    [Range(0.2f, 1f)] public float slotIconSize = IDefSlotIconSize;
    public Color slotIconColor = Color.white;
    [Range(0f, 1f)] public float slotIconOpacity = IDefSlotIconOpacity;

    [Header("Panel")]
    public Color plateTopColor = IDefPlateTop;
    public Color plateBottomColor = IDefPlateBottom;
    public Color borderColor = IDefBorderColor;
    [Range(0f, 4f)] public float borderWidth = IDefBorder;
    [Range(0f, 8f)] public float edgeWidth = IDefEdge;
    [Range(0f, 40f)] public float padding = IDefPadding;
    public Color accentColor = IDefAccent;
    public Color essenceColor = Color.white;
    [Range(9f, 30f)] public float essenceFontSize = IDefEssenceSize;
    [Range(0.5f, 3f)] public float essenceIconScale = IDefEssenceIcon;
    [Range(0f, 1f)] public float bloom = IDefBloom;
    [Range(0f, 1f)] public float vignette = IDefVignette;

    [Header("Placement feedback")]
    public Color validColor = IDefValid;
    public Color invalidColor = IDefInvalid;
    public Color swapColor = IDefSwap;

    [Header("Items")]
    public Color tileColor = IDefTile;
    [Range(0f, 1f)] public float rarityTint = IDefRarityTint;
    public bool showTooltips = true;
    public TooltipBox tooltip;

    [Header("Text")]
    public FontAsset displayFont;
    public FontAsset labelFont;
    [Range(8f, 28f)] public float titleFontSize = 14f;
    [Range(0f, 120f)] public float titleTracking = 30f;
    [Range(0f, 80f)] public float sectionTracking = 14f;
    [Range(7f, 20f)] public float labelFontSize = 10f;
    [Range(6f, 16f)] public float slotLabelFontSize = 8f;
    public Color titleColor = IDefTitle;
    public Color labelColor = IDefLabel;

    [Header("Dropping")]
    public bool dropToGround = true;

    public bool IsOpen => isOpen;

    private Item held => inventory != null ? inventory.OnCursor : null;

    private UIDocument doc;
    private VisualElement root;
    private VisualElement panel;
    private CssBox plate;
    private VisualElement header, body, edge, essenceRow;
    private VisualElement sectEquipped, sectCarried;
    private VisualElement essenceOrb;
    private Label essenceValue, essenceLabel;
    private readonly List<CssBox> sectionRules = new List<CssBox>();
    private readonly List<Label> sectionLabels = new List<Label>();
    private VisualElement bagTools;
    private VisualElement searchBox;
    private TextField searchField;
    private Label searchHint;
    private VisualElement sortButton;
    private Label sortLabel;
    private VisualElement sortArt;
    private readonly List<VisualElement> searchChrome = new List<VisualElement>();
    private VisualElement bagColumn;
    private VisualElement gear;
    private VisualElement bag;
    private CssBox well;
    private VisualElement grid;
    private VisualElement preview;
    private VisualElement catcher;
    private VisualElement ghost;
    private Label titleLabel;
    private VisualElement menuButton;

    private readonly List<VisualElement> tiles = new List<VisualElement>();
    private readonly List<VisualElement> cells = new List<VisualElement>();
    private readonly List<(SlotId Id, CssBox Element)> gearSlots = new List<(SlotId, CssBox)>();
    private readonly List<Inventory.PlacedItem> scratch = new List<Inventory.PlacedItem>();
    private readonly TooltipContent tipContent = new TooltipContent();

    private Inventory inventory;
    private EquipmentLoadout loadout;
    private Wallet wallet;
    private float findCooldown;
    private bool isOpen;
    private bool startApplied;
    private bool skinValid;
    private Skin skin;
    private Vector2Int builtGrid = new Vector2Int(-1, -1);
    private bool itemsDirty = true;
    private Vector2Int? hoverCell;
    private string query = string.Empty;
    private bool typing;
    private int searchKeyFrame = -10;
    private int searchClickFrame = -10;
    private float caretPhase;
    private bool caretOn = true;

    private const int DollColumns = 3;
    private static readonly SlotId[] DollGrid =
    {
        SlotId.None,    SlotId.Head,  SlotId.None,
        SlotId.Cape,    SlotId.Chest, SlotId.Amulet,
        SlotId.Gloves,  SlotId.Belt,  SlotId.Ring1,
        SlotId.Trinket, SlotId.Pants, SlotId.Ring2,
        SlotId.Weapon,  SlotId.Boots, SlotId.Offhand,
    };

    private static readonly int LastRarity = Enum.GetValues(typeof(ItemRarity)).Length - 1;

    private struct Skin : IEquatable<Skin>
    {
        public float Cell, Gap, Pad, Border, TitleSize, LabelSize, SlotLabelSize, Slot, SlotGap;
        public float TitleTrack, SectionTrack;
        public float Bloom, Vignette, RarityTint, Edge, GridLine, EssenceSize, EssenceIcon;
        public float ControlHeight, SearchDim, SlotIconSize, SlotIconOpacity;
        public Color PlateTop, PlateBottom, BorderCol;
        public Color Well, CellFill, CellLight, CellLine, Title, Label, Tile, Accent, Essence;
        public Color SlotIconColor;
        public bool Gear, SlotLabels, Search, Sort;
        public int Cols, Rows;
        public FontAsset DisplayFont, LabelFont;
        public Texture2D SortIcon;

        public bool Equals(Skin o) =>
            Cell == o.Cell && Gap == o.Gap && Pad == o.Pad && Border == o.Border &&
            TitleSize == o.TitleSize && LabelSize == o.LabelSize &&
            SlotLabelSize == o.SlotLabelSize &&
            TitleTrack == o.TitleTrack && SectionTrack == o.SectionTrack &&
            Slot == o.Slot && SlotGap == o.SlotGap &&
            Bloom == o.Bloom && Vignette == o.Vignette && RarityTint == o.RarityTint &&
            Edge == o.Edge && GridLine == o.GridLine && EssenceSize == o.EssenceSize &&
            EssenceIcon == o.EssenceIcon &&
            PlateTop == o.PlateTop && PlateBottom == o.PlateBottom && BorderCol == o.BorderCol &&
            Accent == o.Accent && Essence == o.Essence &&
            Well == o.Well && CellFill == o.CellFill && CellLight == o.CellLight &&
            CellLine == o.CellLine &&
            Title == o.Title && Label == o.Label && Tile == o.Tile &&
            Gear == o.Gear && SlotLabels == o.SlotLabels &&
            Search == o.Search && Sort == o.Sort &&
            ControlHeight == o.ControlHeight &&
            SearchDim == o.SearchDim &&
            SlotIconSize == o.SlotIconSize && SlotIconOpacity == o.SlotIconOpacity &&
            SlotIconColor == o.SlotIconColor &&
            Cols == o.Cols && Rows == o.Rows &&
            DisplayFont == o.DisplayFont && LabelFont == o.LabelFont && SortIcon == o.SortIcon;

        public override bool Equals(object o) => o is Skin s && Equals(s);
        public override int GetHashCode() => Cols * 397 ^ Rows;
    }

    private void Awake()
    {
        if (toggleAction.bindings.Count == 0) toggleAction.AddBinding("<Keyboard>/i");
    }

    private void OnEnable()
    {
        doc = GetComponent<UIDocument>();
        if (Application.isPlaying) toggleAction.Enable();
    }

    private void OnDisable()
    {
        if (Application.isPlaying)
        {
            toggleAction.Disable();
            Release();
        }

        if (typing) { typing = false; UITextEntry.Active = false; }

        BindInventory(null);
        SetOpen(false);

        panel?.RemoveFromHierarchy();
        catcher?.RemoveFromHierarchy();
        ghost?.RemoveFromHierarchy();

        if (menuButton != null)
        {
            menuButton.UnregisterCallback<PointerDownEvent>(OnMenuButtonDown);
            menuButton.pickingMode = PickingMode.Ignore;
            menuButton = null;
        }

        cells.Clear();
        tiles.Clear();
        gearSlots.Clear();
        sectionRules.Clear();
        sectionLabels.Clear();
        searchChrome.Clear();
        root = null;
        panel = null;
        catcher = null;
        ghost = null;
    }

    private void OnValidate()
    {
        skinValid = false;
        itemsDirty = true;
    }

    private void LateUpdate()
    {
        ResolveInventory();

        if (!CacheTree()) return;

        if (!show)
        {
            if (isOpen) Close();
            ApplyPicking();
            return;
        }

        if (Application.isPlaying) ReadInput();
        else SetOpen(previewInEditor);

        ApplyStyle();

        if (isOpen)
        {
            if (itemsDirty) RebuildItems();
            UpdateEssence();
            UpdateCaret();
            UpdateHover();
            UpdateGhost();
            UpdateTooltip();
        }

        ApplyPicking();
    }

    private void ReadInput()
    {
        if (!startApplied)
        {
            startApplied = true;
            if (openOnStart) Open();
        }

        if (UITextEntry.Active) return;

        if (toggleAction.WasPressedThisFrame()) Toggle();

        if (isOpen && Time.frameCount != searchKeyFrame
            && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
    }

    public void Toggle()
    {
        if (isOpen) Close(); else Open();
    }

    private void OnMenuButtonDown(PointerDownEvent e)
    {
        Toggle();
        UIPointer.ConsumeClick();
        e.StopPropagation();
    }

    public void Open()
    {
        if (isOpen || !show) return;
        SetOpen(true);
        itemsDirty = true;
    }

    public void Close()
    {
        if (!isOpen) return;
        Release();
        SetOpen(false);
    }

    private void SetOpen(bool open)
    {
        isOpen = open;
        if (panel != null) panel.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
        if (!open)
        {
            Blur();
            ClearQuery();
            hoverCell = null;
            HideTooltip();
            if (preview != null) preview.style.display = DisplayStyle.None;
        }
    }

    private void ResolveInventory()
    {
        if (!Application.isPlaying) return;
        if (inventory != null) return;

        findCooldown -= Time.unscaledDeltaTime;
        if (findCooldown > 0f) return;
        findCooldown = 1f;

        Inventory found = LocalPlayer.Get<Inventory>();
        if (found != null) BindInventory(found);
    }

    private void BindInventory(Inventory next)
    {
        if (inventory == next) return;
        if (inventory != null) inventory.onChanged.RemoveListener(OnInventoryChanged);
        if (loadout != null) loadout.onChanged.RemoveListener(OnInventoryChanged);
        inventory = next;
        loadout = inventory != null ? inventory.GetComponent<EquipmentLoadout>() : null;
        if (inventory != null) inventory.onChanged.AddListener(OnInventoryChanged);
        if (loadout != null) loadout.onChanged.AddListener(OnInventoryChanged);
        wallet = null;
        itemsDirty = true;
        builtGrid = new Vector2Int(-1, -1);
    }

    private void OnInventoryChanged() => itemsDirty = true;

    private int Columns => inventory != null ? inventory.Columns : Mathf.Max(1, previewGrid.x);
    private int Rows => inventory != null ? inventory.Rows : Mathf.Max(1, previewGrid.y);

    private float Pitch => cellSize + cellGap;

    private float Span(int count) => count * cellSize + Mathf.Max(0, count - 1) * cellGap;

    private bool CacheTree()
    {
        if (doc == null) doc = GetComponent<UIDocument>();
        VisualElement r = doc != null ? doc.rootVisualElement : null;
        if (r == null) { root = null; panel = null; return false; }
        if (r == root && panel != null) return true;

        root = r;
        skinValid = false;
        builtGrid = new Vector2Int(-1, -1);
        itemsDirty = true;
        cells.Clear();
        tiles.Clear();

        catcher = BuildCatcher();
        panel = BuildPanel();
        Mount(catcher, panel);

        ghost = BuildGhost();
        root.Add(ghost);

        menuButton = root.Q<VisualElement>(className: "mbtn-inventory");
        menuButton?.RegisterCallback<PointerDownEvent>(OnMenuButtonDown);

        SetOpen(isOpen);
        return true;
    }

    private static readonly string[] AlwaysOnTop = { "xpbar", "menu" };

    private void Mount(VisualElement under, VisualElement over)
    {
        VisualElement hud = root.Q<VisualElement>(className: "hud");

        if (hud == null)
        {
            root.Add(under);
            root.Add(over);
            return;
        }

        hud.Add(under);
        hud.Add(over);

        foreach (string cls in AlwaysOnTop)
        {
            VisualElement e = hud.Q<VisualElement>(className: cls);
            if (e != null && e.parent == hud) e.PlaceInFront(over);
        }
    }

    private VisualElement BuildPanel()
    {
        VisualElement wrapper = new VisualElement { name = "inventory" };
        wrapper.AddToClassList("inv");
        wrapper.pickingMode = PickingMode.Ignore;

        gearSlots.Clear();

        CssBox shadow = new CssBox();
        shadow.AddToClassList("inv-shadow");
        shadow.pickingMode = PickingMode.Ignore;
        wrapper.Add(shadow);

        plate = new CssBox { name = "inv-plate" };
        plate.AddToClassList("inv-plate");
        wrapper.Add(plate);

        edge = new VisualElement { name = "inv-edge" };
        edge.AddToClassList("inv-edge");
        edge.pickingMode = PickingMode.Ignore;

        header = new VisualElement { name = "inv-header" };
        header.AddToClassList("inv-header");
        titleLabel = new Label("INVENTORY");
        titleLabel.AddToClassList("inv-title");
        header.Add(titleLabel);
        plate.Add(header);

        body = new VisualElement { name = "inv-body" };
        body.AddToClassList("inv-body");
        plate.Add(body);

        gear = new VisualElement { name = "inv-gear" };
        gear.AddToClassList("inv-gear");

        foreach (SlotId id in DollGrid)
        {
            if (id == SlotId.None) continue;
            gear.Add(BuildSlot(id));
        }
        body.Add(sectEquipped = NewSection("EQUIPPED"));
        body.Add(gear);

        bag = new VisualElement { name = "inv-bag" };
        bag.AddToClassList("inv-bag");
        bag.pickingMode = PickingMode.Ignore;

        bagColumn = new VisualElement { name = "inv-bag-column" };
        bagColumn.AddToClassList("inv-bag-column");
        bagColumn.pickingMode = PickingMode.Ignore;
        bag.Add(bagColumn);

        bagColumn.Add(sectCarried = NewSection("CARRIED"));

        bagTools = new VisualElement { name = "inv-bag-tools" };
        bagTools.AddToClassList("inv-bag-tools");
        bagTools.pickingMode = PickingMode.Ignore;
        bagTools.Add(BuildSearch());
        bagTools.Add(BuildSortButton());
        bagColumn.Add(bagTools);

        well = new CssBox { name = "inv-well" };
        well.AddToClassList("inv-well");
        grid = new VisualElement { name = "inv-grid" };
        grid.AddToClassList("inv-grid");
        well.Add(grid);
        bagColumn.Add(well);
        body.Add(bag);

        preview = new VisualElement { name = "inv-preview" };
        preview.AddToClassList("inv-preview");
        preview.pickingMode = PickingMode.Ignore;
        preview.style.display = DisplayStyle.None;
        grid.Add(preview);

        essenceRow = new VisualElement { name = "inv-essence-row" };
        essenceRow.AddToClassList("inv-essence-row");
        essenceRow.pickingMode = PickingMode.Ignore;

        VisualElement essence = new VisualElement();
        essence.AddToClassList("inv-essence");
        essenceOrb = new VisualElement();
        essenceOrb.AddToClassList("inv-orb");
        essenceValue = new Label("0");
        essenceValue.AddToClassList("inv-essence-value");
        essence.Add(essenceOrb);
        essence.Add(essenceValue);

        essenceLabel = new Label("ESSENCE");
        essenceLabel.AddToClassList("inv-essence-label");

        essenceRow.Add(essence);
        essenceRow.Add(essenceLabel);

        bagColumn.Add(essenceRow);

        grid.RegisterCallback<PointerDownEvent>(OnGridDown);

        plate.RegisterCallback<PointerDownEvent>(_ => UIPointer.ConsumeClick());

        plate.RegisterCallback<NavigationMoveEvent>(e => { if (!typing) e.StopPropagation(); }, TrickleDown.TrickleDown);

        wrapper.Add(edge);
        return wrapper;
    }

    private VisualElement NewSection(string text)
    {
        VisualElement row = new VisualElement();
        row.AddToClassList("inv-sect");
        row.pickingMode = PickingMode.Ignore;

        Label label = new Label(text);
        label.AddToClassList("inv-sect-label");
        label.pickingMode = PickingMode.Ignore;

        CssBox rule = new CssBox();
        rule.AddToClassList("inv-sect-rule");
        rule.pickingMode = PickingMode.Ignore;

        row.Add(label);
        row.Add(rule);
        sectionRules.Add(rule);
        sectionLabels.Add(label);
        return row;
    }

    private CssBox BuildSlot(SlotId id)
    {
        CssBox socket = new CssBox();
        socket.AddToClassList("inv-slot");
        socket.userData = id;

        VisualElement icon = new VisualElement();
        icon.AddToClassList("inv-slot-icon");
        icon.pickingMode = PickingMode.Ignore;
        socket.Add(icon);

        Label label = new Label(SlotName(id));
        label.AddToClassList("inv-slot-label");
        label.pickingMode = PickingMode.Ignore;
        socket.Add(label);

        socket.RegisterCallback<PointerDownEvent>(e =>
        {
            OnSlotDown(id, e.button);
            UIPointer.ConsumeClick();
            e.StopPropagation();
        });

        gearSlots.Add((id, socket));
        return socket;
    }

    private VisualElement BuildSearch()
    {
        searchBox = new VisualElement { name = "inv-search" };
        searchBox.AddToClassList("inv-search");

        searchField = new TextField { name = "inv-search-input" };
        searchField.AddToClassList("inv-search-input");
        searchField.RegisterValueChangedCallback(e => { caretPhase = Time.unscaledTime; SetQuery(e.newValue); });
        searchBox.RegisterCallback<PointerDownEvent>(_ => searchClickFrame = Time.frameCount,
                                                    TrickleDown.TrickleDown);

        searchField.RegisterCallback<FocusInEvent>(_ =>
        {
            if (Time.frameCount != searchClickFrame)
            {
                searchField.schedule.Execute(() => { if (!typing) searchField.Blur(); });
                return;
            }

            typing = true;
            UITextEntry.Active = true;
            caretPhase = Time.unscaledTime;
            ApplySearch();
        });
        searchField.RegisterCallback<FocusOutEvent>(_ =>
        {
            typing = false;
            UITextEntry.Active = false;
            ApplySearch();
        });
        searchField.RegisterCallback<KeyDownEvent>(OnSearchKeyDown, TrickleDown.TrickleDown);
        searchBox.Add(searchField);

        searchHint = new Label("SEARCH");
        searchHint.AddToClassList("inv-search-hint");
        searchHint.pickingMode = PickingMode.Ignore;
        searchBox.Add(searchHint);

        searchChrome.Clear();
        searchChrome.Add(searchBox);
        searchChrome.Add(searchField);
        VisualElement inner = searchField.Q("unity-text-input");
        if (inner != null) searchChrome.Add(inner);

        return searchBox;
    }

    private VisualElement BuildSortButton()
    {
        sortButton = new VisualElement { name = "inv-sort" };
        sortButton.AddToClassList("inv-sort");

        sortArt = new VisualElement();
        sortArt.AddToClassList("inv-sort-art");
        sortArt.pickingMode = PickingMode.Ignore;
        sortLabel = new Label("SORT");
        sortLabel.AddToClassList("inv-sort-label");
        sortLabel.pickingMode = PickingMode.Ignore;
        sortButton.Add(sortArt);
        sortButton.Add(sortLabel);

        sortButton.RegisterCallback<PointerDownEvent>(e =>
        {
            SortBag();
            UIPointer.ConsumeClick();
            e.StopPropagation();
        });

        return sortButton;
    }

    private VisualElement BuildCatcher()
    {
        VisualElement c = new VisualElement { name = "inv-catcher" };
        c.AddToClassList("inv-catcher");
        c.style.display = DisplayStyle.None;
        c.RegisterCallback<PointerDownEvent>(e =>
        {
            ThrowHeld();
            UIPointer.ConsumeClick();
            e.StopPropagation();
        });
        return c;
    }

    private VisualElement BuildGhost()
    {
        VisualElement g = new VisualElement { name = "inv-ghost" };
        g.AddToClassList("inv-ghost");
        g.pickingMode = PickingMode.Ignore;
        g.style.display = DisplayStyle.None;
        return g;
    }

    private void ApplyStyle()
    {
        Skin next = new Skin
        {
            Cell = cellSize, Gap = cellGap, Pad = padding, Border = borderWidth,
            Slot = slotSize, SlotGap = slotGap,
            TitleSize = titleFontSize, LabelSize = labelFontSize,
            SlotLabelSize = slotLabelFontSize,
            TitleTrack = titleTracking, SectionTrack = sectionTracking,
            PlateTop = plateTopColor, PlateBottom = plateBottomColor, BorderCol = borderColor,
            Accent = accentColor, Essence = essenceColor,
            Well = wellColor, CellFill = cellColor, CellLight = cellLightColor,
            CellLine = cellLineColor,
            Bloom = bloom, Vignette = vignette, RarityTint = rarityTint,
            Edge = edgeWidth, GridLine = gridLineWidth, EssenceSize = essenceFontSize,
            EssenceIcon = essenceIconScale,
            Title = titleColor, Label = labelColor, Tile = tileColor,
            Gear = showEquipment, SlotLabels = showSlotLabels,
            SlotIconSize = slotIconSize, SlotIconOpacity = slotIconOpacity,
            SlotIconColor = slotIconColor,
            Search = showSearch, Sort = showSort,
            ControlHeight = controlHeight, SearchDim = searchDim,
            Cols = Columns, Rows = Rows,
            DisplayFont = displayFont, LabelFont = labelFont, SortIcon = sortIcon
        };

        if (skinValid && skin.Equals(next)) return;
        skin = next;
        skinValid = true;

        float gridW = Span(next.Cols);
        float gridH = Span(next.Rows);
        float wellW = gridW + 2f * SocketEdge;

        plate.style.width = wellW + next.Pad * 2f + next.Border * 2f;
        SetBorder(plate, next.BorderCol, next.Border);
        plate.SetBaseGradient(next.PlateTop, next.PlateBottom);

        Color dark = new Color(0f, 0f, 0f, next.Vignette);
        plate.SetOverlay1Colors(new Color(0f, 0f, 0f, 0f), dark);
        Color lit = new Color(BloomTint.r, BloomTint.g, BloomTint.b, next.Bloom);
        plate.SetOverlay2Colors(lit, new Color(lit.r, lit.g, lit.b, 0f));

        header.style.paddingLeft = next.Pad;
        header.style.paddingRight = next.Pad;
        titleLabel.style.fontSize = next.TitleSize;
        titleLabel.style.color = next.Title;
        titleLabel.style.letterSpacing = next.TitleTrack;
        ApplyFont(titleLabel, next.DisplayFont);

        edge.style.width = next.Edge;
        edge.style.backgroundColor = next.Accent;

        foreach (CssBox rule in sectionRules)
        {
            Color faint = new Color(next.Accent.r, next.Accent.g, next.Accent.b, next.Accent.a * 0.55f);
            rule.SetBaseGradient(faint, new Color(faint.r, faint.g, faint.b, 0f));
        }
        foreach (Label label in sectionLabels)
        {
            label.style.color = next.Accent;
            label.style.fontSize = next.LabelSize;
            label.style.letterSpacing = next.SectionTrack;
            ApplyFont(label, next.LabelFont);
        }

        ApplyBagControls(next);

        bagColumn.style.width = wellW;

        essenceOrb.style.unityBackgroundImageTintColor = next.Essence;
        essenceValue.style.color = next.Essence;
        essenceValue.style.fontSize = next.EssenceSize;
        essenceLabel.style.color = next.Label;
        ApplyFont(essenceValue, next.DisplayFont);
        ApplyFont(essenceLabel, next.LabelFont);
        float orb = Mathf.Round(next.EssenceSize * next.EssenceIcon);
        essenceOrb.style.width = orb;
        essenceOrb.style.height = orb;

        body.style.paddingTop = next.Pad;
        body.style.paddingRight = next.Pad;
        body.style.paddingBottom = next.Pad;
        body.style.paddingLeft = next.Pad;

        gear.style.display = next.Gear ? DisplayStyle.Flex : DisplayStyle.None;
        sectEquipped.style.display = next.Gear ? DisplayStyle.Flex : DisplayStyle.None;

        LayoutSlots(next);

        well.style.width = wellW;
        well.style.height = gridH + 2f * SocketEdge;
        SetBorder(well, next.Well, SocketEdge);
        well.SetBaseGradient(next.CellFill, next.CellLight);

        grid.style.width = gridW;
        grid.style.height = gridH;

        RebuildCells(next);
        itemsDirty = true;
    }

    private void ApplyBagControls(Skin s)
    {
        bool anyTool = s.Search || s.Sort;
        bagTools.style.display = anyTool ? DisplayStyle.Flex : DisplayStyle.None;
        sectCarried.style.marginBottom = anyTool ? 7f : StyleKeyword.Null;

        searchBox.style.display = s.Search ? DisplayStyle.Flex : DisplayStyle.None;
        searchBox.style.height = s.ControlHeight;
        searchBox.style.backgroundColor = s.CellFill;
        SetBorder(searchBox, s.Well, 1f);

        float text = Mathf.Min(s.LabelSize, s.ControlHeight * 0.55f);
        VisualElement input = searchField.Q("unity-text-input");
        if (input != null)
        {
            input.style.fontSize = text;
            input.style.color = s.Accent;
            ApplyFont(input, s.LabelFont);
        }
        searchHint.style.fontSize = text;
        searchHint.style.color = s.Label;
        ApplyFont(searchHint, s.LabelFont);

        caretOn = true;
        searchField.EnableInClassList("inv-caret-off", false);

        sortButton.style.display = s.Sort ? DisplayStyle.Flex : DisplayStyle.None;
        sortButton.style.width = s.ControlHeight;
        sortButton.style.height = s.ControlHeight;
        sortButton.style.backgroundColor = s.CellFill;
        SetBorder(sortButton, s.Well, 1f);

        bool art = s.SortIcon != null;
        sortArt.style.display = art ? DisplayStyle.Flex : DisplayStyle.None;
        sortArt.style.backgroundImage = art ? new StyleBackground(s.SortIcon) : new StyleBackground(StyleKeyword.None);
        sortArt.style.unityBackgroundImageTintColor = s.Accent;
        sortLabel.style.display = art ? DisplayStyle.None : DisplayStyle.Flex;
        sortLabel.style.fontSize = Mathf.Min(s.SlotLabelSize, s.ControlHeight / 3.2f);
        sortLabel.style.color = s.Accent;
        ApplyFont(sortLabel, s.LabelFont);
    }

    private void LayoutSlots(Skin s)
    {
        float pitch = s.Slot + s.SlotGap;
        int built = 0;

        for (int cell = 0; cell < DollGrid.Length; cell++)
        {
            if (DollGrid[cell] == SlotId.None) continue;

            CssBox socket = gearSlots[built].Element;
            built++;

            socket.style.left = cell % DollColumns * pitch;
            socket.style.top = cell / DollColumns * pitch;
            socket.style.width = s.Slot;
            socket.style.height = s.Slot;
            SetBorder(socket, s.Well, SocketEdge);
            socket.SetBaseGradient(s.CellFill, s.CellLight);

            VisualElement icon = socket.Q<VisualElement>(className: "inv-slot-icon");
            if (icon != null)
            {
                float art = Mathf.Round(s.Slot * s.SlotIconSize);
                float inset = Mathf.Round((s.Slot - 2f * SocketEdge - art) * 0.5f);
                icon.style.width = art;
                icon.style.height = art;
                icon.style.left = inset;
                icon.style.top = inset;
                icon.style.unityBackgroundImageTintColor =
                    new Color(s.SlotIconColor.r, s.SlotIconColor.g, s.SlotIconColor.b, 1f);
                icon.style.opacity = s.SlotIconOpacity;
            }

            Label label = socket.Q<Label>(className: "inv-slot-label");
            if (label == null) continue;

            label.style.display = s.SlotLabels ? DisplayStyle.Flex : DisplayStyle.None;
            label.style.fontSize = Mathf.Min(s.SlotLabelSize, s.Slot / 5.4f);
            label.style.color = s.Label;
            ApplyFont(label, s.LabelFont);
        }

        gear.style.width = DollColumns * s.Slot + (DollColumns - 1) * s.SlotGap;
        gear.style.height = DollRows * s.Slot + (DollRows - 1) * s.SlotGap;
    }

    private static int DollRows => (DollGrid.Length + DollColumns - 1) / DollColumns;

    private void RebuildCells(Skin s)
    {
        Vector2Int dims = new Vector2Int(s.Cols, s.Rows);

        if (dims != builtGrid)
        {
            foreach (VisualElement old in cells) old.RemoveFromHierarchy();
            cells.Clear();

            int at = 0;
            for (int x = 1; x < s.Cols; x++) cells.Add(NewRule(at++));
            for (int y = 1; y < s.Rows; y++) cells.Add(NewRule(at++));

            builtGrid = dims;
        }

        float pitch = s.Cell + s.Gap;
        int i = 0;

        for (int x = 1; x < s.Cols; x++, i++)
        {
            VisualElement line = cells[i];
            line.style.left = x * pitch - s.Gap;
            line.style.top = 0f;
            line.style.width = s.GridLine;
            line.style.height = Span(s.Rows);
            line.style.backgroundColor = s.CellLine;
        }
        for (int y = 1; y < s.Rows; y++, i++)
        {
            VisualElement line = cells[i];
            line.style.left = 0f;
            line.style.top = y * pitch - s.Gap;
            line.style.width = Span(s.Cols);
            line.style.height = s.GridLine;
            line.style.backgroundColor = s.CellLine;
        }
    }

    private VisualElement NewRule(int index)
    {
        VisualElement line = new VisualElement();
        line.AddToClassList("inv-rule-line");
        line.pickingMode = PickingMode.Ignore;
        grid.Insert(index, line);
        return line;
    }

    private void RebuildItems()
    {
        itemsDirty = false;

        foreach (VisualElement tile in tiles) tile.RemoveFromHierarchy();
        tiles.Clear();

        if (inventory != null)
        {
            foreach (Inventory.PlacedItem placed in inventory.Placed)
            {
                Vector2Int size = placed.Item.Size;
                VisualElement tile = BuildTile(placed.Item, Span(size.x), Span(size.y));
                tile.style.left = placed.Origin.x * Pitch;
                tile.style.top = placed.Origin.y * Pitch;
                tile.userData = placed.Item;
                grid.Add(tile);
                tiles.Add(tile);
            }
        }

        foreach ((SlotId id, CssBox socket) in gearSlots)
        {
            VisualElement old = socket.Q<VisualElement>(className: "inv-tile");
            old?.RemoveFromHierarchy();

            EquipSlot slot = loadout != null ? loadout.SlotWithId(id) : null;
            Item worn = slot != null ? slot.Item : null;

            Texture2D art = IconForSlot(id);
            VisualElement icon = socket.Q<VisualElement>(className: "inv-slot-icon");
            if (icon != null)
            {
                icon.style.display = worn == null && art != null ? DisplayStyle.Flex : DisplayStyle.None;
                if (art != null) icon.style.backgroundImage = new StyleBackground(art);
            }

            Label label = socket.Q<Label>(className: "inv-slot-label");
            if (label != null)
                label.style.display = worn == null && showSlotLabels && art == null
                    ? DisplayStyle.Flex : DisplayStyle.None;

            if (worn == null) continue;

            VisualElement tile = BuildTile(worn, slotSize, slotSize);
            tile.style.left = 0f;
            tile.style.top = 0f;
            socket.Add(tile);
        }

        ApplySearch();
    }

    private VisualElement BuildTile(Item item, float width, float height)
    {
        VisualElement tile = new VisualElement();
        tile.AddToClassList("inv-tile");
        tile.pickingMode = PickingMode.Ignore;
        tile.style.width = width;
        tile.style.height = height;

        Color rarity = item.Definition.rarity.ToColor();

        float weight = (float)item.Definition.rarity / (LastRarity == 0 ? 1 : LastRarity);
        tile.style.backgroundColor = Color.Lerp(tileColor, rarity, rarityTint * weight);
        SetBorder(tile, rarity, 1f);

        VisualElement art = new VisualElement();
        art.AddToClassList("inv-tile-art");
        art.pickingMode = PickingMode.Ignore;

        Texture2D icon = item.Definition.icon;
        if (icon != null)
        {
            art.style.backgroundImage = Background.FromTexture2D(icon);
        }
        else
        {
            art.style.backgroundColor = item.Definition.tint;
        }
        tile.Add(art);

        return tile;
    }

    private Texture2D IconForSlot(SlotId id)
    {
        if (slotIcons == null) return null;
        foreach (SlotIcon entry in slotIcons)
        {
            if (entry != null && entry.texture != null && entry.slot == id) return entry.texture;
        }
        return null;
    }

    private void SetQuery(string text)
    {
        string next = text == null ? string.Empty : text.Trim();
        if (next == query) return;
        query = next;
        ApplySearch();
    }

    private void ClearQuery()
    {
        query = string.Empty;
        searchField?.SetValueWithoutNotify(string.Empty);
        ApplySearch();
    }

    private void ApplySearch()
    {
        bool filtering = showSearch && query.Length > 0;

        foreach (VisualElement tile in tiles)
        {
            Item item = tile.userData as Item;
            tile.style.opacity = !filtering || Matches(item) ? 1f : searchDim;
        }

        if (searchHint != null)
            searchHint.style.display = typing || query.Length > 0 ? DisplayStyle.None : DisplayStyle.Flex;
    }

    private void UpdateCaret()
    {
        if (searchField == null) return;

        bool on = !typing || Mathf.Repeat(Time.unscaledTime - caretPhase, CaretBlink * 2f) < CaretBlink;
        if (on == caretOn) return;

        caretOn = on;
        searchField.EnableInClassList("inv-caret-off", !on);
    }

    private bool Matches(Item item)
    {
        if (item == null) return false;
        ItemDefinition def = item.Definition;
        return Contains(def.displayName) || Contains(def.rarity.ToString()) || Contains(EquipmentTypes.Label(def.equipmentType));
    }

    private bool Contains(string text) =>
        !string.IsNullOrEmpty(text) && text.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;

    private void OnSearchKeyDown(KeyDownEvent e)
    {
        if (e.keyCode != KeyCode.Escape) return;

        searchKeyFrame = Time.frameCount;
        if (query.Length > 0) ClearQuery();
        else Blur();
        e.StopPropagation();
    }

    private void Blur()
    {
        if (searchField == null) return;
        typing = false;
        UITextEntry.Active = false;
        searchField.Blur();
    }

    private void SortBag()
    {
        if (inventory == null || inventory.Placed.Count == 0) return;

        inventory.RequestSort();
    }

    private static int CompareForSort(Item a, Item b)
    {
        int areaA = a.Size.x * a.Size.y;
        int areaB = b.Size.x * b.Size.y;
        if (areaA != areaB) return areaB - areaA;
        if (a.Size.y != b.Size.y) return b.Size.y - a.Size.y;
        int rarity = b.Definition.rarity.CompareTo(a.Definition.rarity);
        if (rarity != 0) return rarity;
        return string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
    }

    private Vector2Int? CellAt(Vector2 panelPos)
    {
        if (grid == null) return null;
        Vector2 local = grid.WorldToLocal(panelPos);
        if (local.x < 0f || local.y < 0f) return null;

        int x = Mathf.FloorToInt(local.x / Pitch);
        int y = Mathf.FloorToInt(local.y / Pitch);
        if (x < 0 || y < 0 || x >= Columns || y >= Rows) return null;
        return new Vector2Int(x, y);
    }

    private Vector2Int PlacementOrigin(Vector2 panelPos, Vector2Int size)
    {
        Vector2 local = grid.WorldToLocal(panelPos);
        Vector2 topLeft = local - new Vector2(Span(size.x), Span(size.y)) * 0.5f;

        int x = Mathf.RoundToInt(topLeft.x / Pitch);
        int y = Mathf.RoundToInt(topLeft.y / Pitch);

        return new Vector2Int(
            Mathf.Clamp(x, 0, Mathf.Max(0, Columns - size.x)),
            Mathf.Clamp(y, 0, Mathf.Max(0, Rows - size.y)));
    }

    private void OnGridDown(PointerDownEvent e)
    {
        UIPointer.ConsumeClick();
        e.StopPropagation();

        if (inventory == null) return;

        Vector2? panelPos = PointerInPanel();
        if (!panelPos.HasValue) return;

        if (held != null)
        {
            DropHeldAt(panelPos.Value);
            return;
        }

        Vector2Int? cell = CellAt(panelPos.Value);
        if (cell == null) return;

        Inventory.PlacedItem placed = inventory.ItemAt(cell.Value);
        if (placed == null) return;

        if (e.button == 1)
        {
            EquipFromBag(placed);
            return;
        }

        inventory.RequestLift(placed.Item.InstanceId);
    }

    private void DropHeldAt(Vector2 panelPos)
    {
        inventory.RequestPlaceFromCursor(PlacementOrigin(panelPos, held.Size));
    }

    private void OnSlotDown(SlotId id, int button)
    {
        if (inventory == null || loadout == null) return;

        EquipSlot slot = loadout.SlotWithId(id);
        if (slot == null) return;

        Item worn = slot.Item;

        if (held != null)
        {
            if (!slot.Accepts(held)) return;

            loadout.RequestEquipFromCursor(id);
            return;
        }

        if (worn == null) return;

        if (button == 1)
        {
            loadout.RequestUnequipToBag(id);
            return;
        }

        loadout.RequestUnequipToCursor(id);
    }

    private void EquipFromBag(Inventory.PlacedItem placed)
    {
        if (loadout == null) return;

        loadout.RequestEquipFromBag(placed.Item.InstanceId);
    }

    private void Release()
    {
        if (held == null) return;

        inventory.RequestReturnCursor();
    }

    private void ThrowHeld()
    {
        if (held == null) return;
        if (!dropToGround) return;

        ChatBox.Instance?.Log(ChatChannel.Loot, $"You drop {held.DisplayName}.");
        inventory.RequestThrowFromCursor();
    }

    private void UpdateEssence()
    {
        if (essenceValue == null) return;

        if (wallet == null && inventory != null) wallet = inventory.GetComponent<Wallet>();

        string text = (wallet != null ? wallet.Essence : 0).ToString("N0");
        if (essenceValue.text != text) essenceValue.text = text;
    }

    private void UpdateHover()
    {
        Vector2? panelPos = PointerInPanel();
        hoverCell = panelPos.HasValue ? CellAt(panelPos.Value) : null;

        if (held == null || !panelPos.HasValue || hoverCell == null)
        {
            preview.style.display = DisplayStyle.None;
            return;
        }

        Vector2Int size = held.Size;
        Vector2Int origin = PlacementOrigin(panelPos.Value, size);

        Color wash;
        if (inventory == null)
        {
            wash = invalidColor;
        }
        else if (inventory.CanPlace(held, origin))
        {
            wash = validColor;
        }
        else
        {
            inventory.DistinctOccupantsUnder(origin, size, scratch);
            wash = scratch.Count == 1 ? swapColor : invalidColor;
        }

        preview.style.display = DisplayStyle.Flex;
        preview.style.left = origin.x * Pitch;
        preview.style.top = origin.y * Pitch;
        preview.style.width = Span(size.x);
        preview.style.height = Span(size.y);
        preview.style.backgroundColor = wash;
    }

    private void UpdateGhost()
    {
        if (held == null)
        {
            ghost.style.display = DisplayStyle.None;
            if (ghost.childCount > 0) ghost.Clear();
            ghost.userData = null;
            return;
        }

        if (!(ghost.userData is Item shown) || shown != held)
        {
            ghost.userData = held;
            ghost.Clear();
            VisualElement tile = BuildTile(held, Span(held.Size.x), Span(held.Size.y));
            tile.style.opacity = 0.9f;
            ghost.Add(tile);
        }

        Vector2? panelPos = PointerInPanel();
        if (!panelPos.HasValue) { ghost.style.display = DisplayStyle.None; return; }

        ghost.style.display = DisplayStyle.Flex;
        ghost.style.left = Mathf.Round(panelPos.Value.x - Span(held.Size.x) * 0.5f);
        ghost.style.top = Mathf.Round(panelPos.Value.y - Span(held.Size.y) * 0.5f);
    }

    private void UpdateTooltip()
    {
        if (tooltip == null) tooltip = GetComponent<TooltipBox>();
        if (tooltip == null) return;
        Vector2? panelPos = PointerInPanel();
        object subject = CanShowTooltip(panelPos) ? FindSubjectUnderPointer(panelPos.Value) : null;
        if (subject == null) tooltip.Hide();
        else tooltip.Show(DescribeSubject(subject));
    }

    private bool CanShowTooltip(Vector2? panelPos) =>
        showTooltips && held == null && inventory != null && panelPos.HasValue;

    private object FindSubjectUnderPointer(Vector2 pointer)
    {
        Inventory.PlacedItem placed = hoverCell != null ? inventory.ItemAt(hoverCell.Value) : null;
        if (placed != null) return placed.Item.Definition;
        SlotId id = showEquipment ? SlotUnder(pointer) : SlotId.None;
        if (id == SlotId.None) return null;
        EquipSlot slot = loadout != null ? loadout.SlotWithId(id) : null;
        return slot != null && slot.Item != null ? slot.Item.Definition : (object)id;
    }

    private TooltipContent DescribeSubject(object subject)
    {
        if (subject is ItemDefinition def) ItemTooltip.Describe(def, tipContent);
        else DescribeSlot((SlotId)subject);
        return tipContent;
    }

    private void DescribeSlot(SlotId id)
    {
        tipContent.Clear();
        tipContent.title = SlotName(id);
        tipContent.titleColor = accentColor;
    }

    private void HideTooltip()
    {
        if (tooltip != null) tooltip.Hide();
    }

    private SlotId SlotUnder(Vector2 panelPos)
    {
        foreach ((SlotId id, CssBox socket) in gearSlots)
        {
            if (socket.worldBound.Contains(panelPos)) return id;
        }
        return SlotId.None;
    }

    private Vector2? PointerInPanel() => root != null ? PanelPointer.Read(root) : null;

    private void ApplyPicking()
    {
        bool live = isOpen && show;

        bool holding = live && held != null;
        catcher.style.display = holding ? DisplayStyle.Flex : DisplayStyle.None;
        catcher.pickingMode = holding ? PickingMode.Position : PickingMode.Ignore;

        panel.pickingMode = PickingMode.Ignore;

        PickingMode mode = live ? PickingMode.Position : PickingMode.Ignore;
        plate.pickingMode = mode;
        grid.pickingMode = mode;
        foreach ((SlotId _, CssBox socket) in gearSlots) socket.pickingMode = mode;

        PickingMode searchMode = live && showSearch ? PickingMode.Position : PickingMode.Ignore;
        foreach (VisualElement e in searchChrome) e.pickingMode = searchMode;
        sortButton.pickingMode = live && showSort ? PickingMode.Position : PickingMode.Ignore;

        if (menuButton != null) menuButton.pickingMode = show ? PickingMode.Position : PickingMode.Ignore;
    }

    private static string SlotName(SlotId id)
    {
        switch (id)
        {
            case SlotId.Offhand: return "OFF HAND";
            case SlotId.Ring1:
            case SlotId.Ring2: return "RING";
            case SlotId.None: return string.Empty;
            default: return id.ToString().ToUpperInvariant();
        }
    }

    private static Color Fade(Color c, float a) => new Color(c.r, c.g, c.b, a);

    private static void ApplyFont(VisualElement element, FontAsset font)
    {
        if (element == null || font == null) return;
        element.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromSDFFont(font));
    }

    private static void SetBorder(VisualElement e, Color c, float w)
    {
        e.style.borderTopWidth = w;
        e.style.borderRightWidth = w;
        e.style.borderBottomWidth = w;
        e.style.borderLeftWidth = w;
        e.style.borderTopColor = c;
        e.style.borderRightColor = c;
        e.style.borderBottomColor = c;
        e.style.borderLeftColor = c;
    }

    private const string SlotIconFolder = "Assets/UI/Sprites/Equipment_Slots/";
    private static readonly (SlotId Id, string File)[] SlotIconFiles =
    {
        (SlotId.Head, "helmet"), (SlotId.Amulet, "amulet"), (SlotId.Trinket, "trinket"),
        (SlotId.Cape, "cloak"),  (SlotId.Chest, "chest"),   (SlotId.Belt, "belt"),
        (SlotId.Pants, "greaves"), (SlotId.Boots, "boot"),  (SlotId.Weapon, "sword"),
        (SlotId.Offhand, "shield"), (SlotId.Gloves, "gloves"),
        (SlotId.Ring1, "ring"),  (SlotId.Ring2, "ring"),
    };

    [ContextMenu("Find Slot Icons")]
    public void FindSlotIcons()
    {
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Find slot icons");

        List<SlotIcon> found = new List<SlotIcon>();
        foreach ((SlotId id, string file) in SlotIconFiles)
        {
            Texture2D tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(SlotIconFolder + file + ".png");
            if (tex != null) found.Add(new SlotIcon { slot = id, texture = tex });
        }

        slotIcons = found.ToArray();
        itemsDirty = true;
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"InventoryWidget: found {found.Count} slot icons in {SlotIconFolder}.", this);
#endif
    }

    [ContextMenu("Reset inventory panel")]
    public void ResetInventoryPanel()
    {
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Reset inventory panel");
#endif
        cellSize = IDefCell;
        cellGap = IDefGap;
        padding = IDefPadding;
        borderWidth = IDefBorder;
        edgeWidth = IDefEdge;
        gridLineWidth = IDefGridLine;
        essenceFontSize = IDefEssenceSize;
        essenceIconScale = IDefEssenceIcon;
        slotSize = IDefSlot;
        slotGap = IDefSlotGap;
        slotIconSize = IDefSlotIconSize;
        slotIconOpacity = IDefSlotIconOpacity;
        slotIconColor = Color.white;
        controlHeight = IDefControlHeight;
        searchDim = IDefSearchDim;
        bloom = IDefBloom;
        vignette = IDefVignette;
        rarityTint = IDefRarityTint;
        accentColor = IDefAccent;
        essenceColor = Color.white;
        plateTopColor = IDefPlateTop;
        plateBottomColor = IDefPlateBottom;
        borderColor = IDefBorderColor;
        wellColor = IDefWell;
        cellColor = IDefCellFill;
        cellLightColor = IDefCellLight;
        cellLineColor = IDefCellLine;
        titleFontSize = 14f;
        titleTracking = 30f;
        sectionTracking = 14f;
        labelFontSize = 10f;
        slotLabelFontSize = 8f;
        titleColor = IDefTitle;
        labelColor = IDefLabel;
        tileColor = IDefTile;
        validColor = IDefValid;
        invalidColor = IDefInvalid;
        swapColor = IDefSwap;
        showEquipment = true;
        showSlotLabels = false;
        showSearch = true;
        showSort = true;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        skinValid = false;
    }
}
