using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

public static class Fonts
{
    public static void Apply(VisualElement element, FontAsset font)
    {
        if (element == null || font == null) return;
        element.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromSDFFont(font));
    }
}
