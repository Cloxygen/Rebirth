using UnityEngine;

public static class TooltipPosition
{
    private const float Gap = 14f;

    public static Vector2 Find(Vector2 pointer, Vector2 tooltipSize, Vector2 panelSize)
    {
        return new Vector2(
            ClampInsidePanel(pointer.x - tooltipSize.x - Gap, tooltipSize.x, panelSize.x),
            ClampInsidePanel(pointer.y + Gap, tooltipSize.y, panelSize.y));
    }

    private static float ClampInsidePanel(float wantedEdge, float tooltipSize, float panelSize) =>
        Mathf.Round(Mathf.Clamp(wantedEdge, 0f, Mathf.Max(0f, panelSize - tooltipSize)));
}
