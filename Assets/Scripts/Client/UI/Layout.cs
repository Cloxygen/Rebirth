using UnityEngine;
using UnityEngine.UIElements;

public static class Layout
{
    public static Vector2? Measure(VisualElement element)
    {
        if (element == null) return null;

        float width = element.resolvedStyle.width;
        float height = element.resolvedStyle.height;
        if (float.IsNaN(width) || width < 1f || float.IsNaN(height) || height < 1f) return null;

        return new Vector2(width, height);
    }
}
