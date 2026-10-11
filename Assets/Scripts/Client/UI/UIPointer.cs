using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public static class UIPointer
{
    private static readonly List<RaycastResult> raycastResults = new List<RaycastResult>();
    private static int consumedFrame = -10;

    public static void ConsumeClick()
    {
        consumedFrame = Time.frameCount;
    }

    public static bool IsOverBlockingUI()
    {
        if (UITextEntry.Active) return true;

        int sinceConsumed = Time.frameCount - consumedFrame;
        if (sinceConsumed >= 0 && sinceConsumed <= 1) return true;
        if (EventSystem.current == null) return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero
        };

        raycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        foreach (RaycastResult result in raycastResults)
        {
            if (result.gameObject.GetComponentInParent<NonBlockingUI>() == null) return true;
        }

        return false;
    }
}
