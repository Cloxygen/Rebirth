using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public static class PanelPointer
{
    public static Vector2? Read(VisualElement root)
    {
        if (Mouse.current == null) return null;
        Vector2? size = Layout.Measure(root);
        if (!size.HasValue) return null;
        Vector2 screen = Mouse.current.position.ReadValue();
        return new Vector2(screen.x / Screen.width * size.Value.x, (1f - screen.y / Screen.height) * size.Value.y);
    }
}
