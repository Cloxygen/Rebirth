using UnityEngine;

public static class DebugPanel
{
    public const float TitleBarHeight = 30.0f;

    private const float Inset = 12.0f;
    private const float Gap = 6.0f;

    private static float claimedHeight;
    private static int lastFrame = -1;
    private static EventType lastEvent = EventType.Ignore;

    public static Rect Claim(float width, float height)
    {
        StartNewPassIfNeeded();

        float top = Screen.height - Inset - claimedHeight - height;
        claimedHeight += height + Gap;

        return new Rect(Inset, top, width, height);
    }

    public static bool DrawTitleBar(string title, ref bool collapsed)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(title);
        if (GUILayout.Button(collapsed ? "+" : "–", GUILayout.Width(26.0f))) collapsed = !collapsed;
        GUILayout.EndHorizontal();

        return !collapsed;
    }

    private static void StartNewPassIfNeeded()
    {
        if (Time.frameCount == lastFrame && Event.current.type == lastEvent) return;

        lastFrame = Time.frameCount;
        lastEvent = Event.current.type;
        claimedHeight = 0.0f;
    }
}
