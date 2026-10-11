using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class TooltipBox : MonoBehaviour
{
    public TooltipStyle style = TooltipStyle.Default;

    private VisualElement box;
    private Label title, rarity, type, subtype, description;
    private StatRows lines;
    private TooltipStyle applied;
    private bool styleValid;
    private UIDocument document;

    private VisualElement Root => document.rootVisualElement;

    public void Show(TooltipContent content)
    {
        Attach();
        ApplyStyleIfChanged();
        bool reopening = IsHidden();
        bool changed = WriteContent(content);
        Open();
        RevealOnceMeasured(changed || reopening);
    }

    private bool IsHidden() => box.style.display.value == DisplayStyle.None;

    private void Open()
    {
        box.style.display = DisplayStyle.Flex;
        box.BringToFront();
    }

    public void Hide()
    {
        if (box != null) box.style.display = DisplayStyle.None;
    }

    private void OnDisable()
    {
        box?.RemoveFromHierarchy();
        box = null;
        styleValid = false;
    }

    private void Attach()
    {
        if (document == null) document = GetComponent<UIDocument>();
        if (box == null) Build();
        if (box.panel == null) Root.Add(box);
    }

    private void Build()
    {
        box = new VisualElement { name = "tooltip", pickingMode = PickingMode.Ignore };
        box.AddToClassList("tooltip");
        title = AddRow("tooltip-title");
        rarity = AddRow("tooltip-rarity");
        AddTypeRow();
        lines = new StatRows(AddHost());
        description = AddRow("tooltip-description");
    }

    private Label AddRow(string className) => AddRow(box, className);

    private void AddTypeRow()
    {
        VisualElement row = new VisualElement();
        row.AddToClassList("tooltip-type-row");
        type = AddRow(row, "tooltip-type");
        subtype = AddRow(row, "tooltip-subtype");
        box.Add(row);
    }

    private static Label AddRow(VisualElement parent, string className)
    {
        Label row = new Label();
        row.AddToClassList(className);
        parent.Add(row);
        return row;
    }

    private VisualElement AddHost()
    {
        VisualElement host = new VisualElement();
        box.Add(host);
        return host;
    }

    private bool WriteContent(TooltipContent content)
    {
        bool changed = WriteRow(title, content.title);
        title.style.color = content.titleColor;
        changed |= WriteRow(rarity, content.rarity);
        changed |= WriteRow(type, content.type);
        changed |= WriteRow(subtype, content.subtype);
        changed |= lines.Show(content.lines);
        changed |= WriteRow(description, content.description);
        return changed;
    }

    private static bool WriteRow(Label row, string text)
    {
        string next = text ?? string.Empty;
        row.style.display = next.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        if (row.text == next) return false;
        row.text = next;
        return true;
    }

    private void RevealOnceMeasured(bool needsMeasuring)
    {
        if (needsMeasuring) box.style.visibility = Visibility.Hidden;
        else Place();
    }

    private void Place()
    {
        Vector2? pointer = PanelPointer.Read(Root);
        Vector2? size = Layout.Measure(box);
        Vector2? panel = Layout.Measure(Root);
        if (!pointer.HasValue || !size.HasValue || !panel.HasValue) return;
        MoveTo(TooltipPosition.Find(pointer.Value, size.Value, panel.Value));
    }

    private void MoveTo(Vector2 spot)
    {
        box.style.left = spot.x;
        box.style.top = spot.y;
        box.style.visibility = Visibility.Visible;
    }

    private void ApplyStyleIfChanged()
    {
        if (styleValid && applied.Equals(style)) return;
        applied = style;
        styleValid = true;
        StyleBox(style);
        StyleRows(style);
    }

    private void StyleBox(TooltipStyle s)
    {
        box.style.maxWidth = s.width;
        box.style.paddingLeft = s.padding;
        box.style.paddingRight = s.padding;
        box.style.paddingTop = s.padding * 0.75f;
        box.style.paddingBottom = s.padding * 0.85f;
    }

    private void StyleRows(TooltipStyle s)
    {
        StyleRow(title, s.titleFont, s.titleSize);
        StyleRow(rarity, s.rarityFont, s.raritySize);
        StyleRow(type, s.typeFont, s.typeSize);
        StyleRow(subtype, s.typeFont, s.typeSize);
        StyleRow(description, s.descriptionFont, s.descriptionSize);
        lines.UseStyle(s.lineFont, s.lineSize, s.lineColor);
        SpaceRows(s);
    }

    private void SpaceRows(TooltipStyle s)
    {
        rarity.style.color = s.rarityColor;
        type.style.color = s.typeColor;
        subtype.style.color = s.typeColor;
        type.style.letterSpacing = s.typeTracking;
        subtype.style.letterSpacing = s.typeTracking;
        type.parent.style.marginBottom = s.typeGap;
        description.style.color = s.descriptionColor;
        description.style.marginTop = s.descriptionGap;
    }

    private static void StyleRow(Label row, FontAsset font, float size)
    {
        Fonts.Apply(row, font);
        row.style.fontSize = size;
    }
}
