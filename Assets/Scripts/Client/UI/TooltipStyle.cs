using System;
using UnityEngine;
using UnityEngine.TextCore.Text;

[Serializable]
public struct TooltipStyle : IEquatable<TooltipStyle>
{
     public FontAsset titleFont;
    [Range(8f, 32f)] public float titleSize;
     public FontAsset rarityFont;
    [Range(6f, 24f)] public float raritySize;
    public Color rarityColor;
     public FontAsset typeFont;
    [Range(6f, 24f)] public float typeSize;
    [Range(0f, 80f)] public float typeTracking;
    public Color typeColor;
    [Range(0f, 30f)] public float typeGap;
     public FontAsset lineFont;
    [Range(6f, 24f)] public float lineSize;
    public Color lineColor;
     public FontAsset descriptionFont;
    [Range(6f, 24f)] public float descriptionSize;
    public Color descriptionColor;
    [Range(0f, 30f)] public float descriptionGap;
    [Range(120f, 600f)] public float width;
    [Range(0f, 30f)] public float padding;

    public static TooltipStyle Default => new TooltipStyle
    {
        titleSize = 16f,
        raritySize = 11f, rarityColor = new Color(0.553f, 0.522f, 0.463f, 1f),
        typeSize = 11f, typeTracking = 14f, typeColor = new Color(0.792f, 0.737f, 0.623f, 1f), typeGap = 4f,
        lineSize = 14f, lineColor = new Color(0.93f, 0.91f, 0.86f, 1f),
        descriptionSize = 13f, descriptionColor = new Color(0.553f, 0.522f, 0.463f, 1f), descriptionGap = 8f,
        width = 320f, padding = 12f,
    };

    public bool Equals(TooltipStyle o) =>
        titleFont == o.titleFont && titleSize == o.titleSize &&
        rarityFont == o.rarityFont && raritySize == o.raritySize && rarityColor == o.rarityColor &&
        typeFont == o.typeFont && typeSize == o.typeSize && typeTracking == o.typeTracking &&
        typeColor == o.typeColor && typeGap == o.typeGap &&
        lineFont == o.lineFont && lineSize == o.lineSize && lineColor == o.lineColor &&
        descriptionFont == o.descriptionFont && descriptionSize == o.descriptionSize &&
        descriptionColor == o.descriptionColor && descriptionGap == o.descriptionGap &&
        width == o.width && padding == o.padding;

    public override bool Equals(object o) => o is TooltipStyle s && Equals(s);
    public override int GetHashCode() => 0;
}
