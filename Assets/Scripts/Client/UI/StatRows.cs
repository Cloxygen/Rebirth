using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

public class StatRows
{
    private const string RowClass = "tooltip-line";

    private readonly List<Label> rows = new List<Label>();
    private int shownCount;
    private readonly VisualElement parent;
    private FontAsset font;
    private float size = 14f;
    private Color color = Color.white;

    public StatRows(VisualElement parent)
    {
        this.parent = parent;
    }

    public void UseStyle(FontAsset font, float size, Color color)
    {
        this.font = font;
        this.size = size;
        this.color = color;
        foreach (Label row in rows) ApplyStyle(row);
    }

    public bool Show(IReadOnlyList<string> lines)
    {
        int lineCount = lines?.Count ?? 0;
        bool changed = lineCount != shownCount;
        for (int i = 0; i < lineCount; i++) changed |= ShowRow(EnsureRow(i), lines[i]);
        HideFrom(lineCount);
        shownCount = lineCount;
        return changed;
    }

    public void Hide()
    {
        HideFrom(0);
        shownCount = 0;
    }

    private static bool ShowRow(Label row, string text)
    {
        row.style.display = DisplayStyle.Flex;
        if (row.text == text) return false;
        row.text = text;
        return true;
    }

    private void HideFrom(int firstToHide)
    {
        for (int i = firstToHide; i < rows.Count; i++) rows[i].style.display = DisplayStyle.None;
    }

    private Label EnsureRow(int index)
    {
        while (rows.Count <= index) rows.Add(CreateRow());
        return rows[index];
    }

    private Label CreateRow()
    {
        Label row = new Label();
        row.AddToClassList(RowClass);
        ApplyStyle(row);
        parent.Add(row);
        return row;
    }

    private void ApplyStyle(Label row)
    {
        Fonts.Apply(row, font);
        row.style.fontSize = size;
        row.style.color = color;
    }
}
