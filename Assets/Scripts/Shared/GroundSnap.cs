using System;
using UnityEngine;

public static class GroundSnap
{
    private const float RayHeight = 2.0f;
    private const float RayDistance = 10.0f;

    public static Vector3 Find(Vector3 position, Transform ignoreRoot)
    {
        Vector3 rayOrigin = position + Vector3.up * RayHeight;
        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, RayHeight + RayDistance,
            ~0, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (ignoreRoot != null && hit.collider.transform.IsChildOf(ignoreRoot)) continue;
            return hit.point;
        }

        return position;
    }
}
