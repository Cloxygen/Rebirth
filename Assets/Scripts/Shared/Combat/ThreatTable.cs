using System.Collections.Generic;
using UnityEngine;

public class ThreatTable
{
    private class Entry
    {
        public float Threat;
        public float LastSeenTime;
    }

    private readonly Dictionary<Transform, Entry> entries = new Dictionary<Transform, Entry>();
    private readonly List<Transform> forgotten = new List<Transform>();

    public Transform Target { get; private set; }

    public bool Contains(Transform who) => who != null && entries.ContainsKey(who);

    public void Add(Transform who, float amount, float now)
    {
        if (who == null || amount <= 0.0f) return;

        if (!entries.TryGetValue(who, out Entry entry))
        {
            entry = new Entry();
            entries[who] = entry;
        }

        entry.Threat += amount;
        entry.LastSeenTime = now;
    }

    public void Update(Vector3 from, float sightRadius, float forgetAfter, float now)
    {
        forgotten.Clear();

        foreach (KeyValuePair<Transform, Entry> pair in entries)
        {
            Transform who = pair.Key;
            Entry entry = pair.Value;

            if (who == null || IsDead(who))
            {
                forgotten.Add(who);
                continue;
            }

            if (HorizontalDistance(from, who.position) <= sightRadius) entry.LastSeenTime = now;
            else if (now - entry.LastSeenTime > forgetAfter) forgotten.Add(who);
        }

        foreach (Transform who in forgotten) entries.Remove(who);

        if (Target != null && !entries.ContainsKey(Target)) Target = null;
    }

    public Transform PickTarget(float switchMargin)
    {
        Transform top = null;
        float topThreat = 0.0f;

        foreach (KeyValuePair<Transform, Entry> pair in entries)
        {
            if (pair.Value.Threat <= topThreat) continue;
            top = pair.Key;
            topThreat = pair.Value.Threat;
        }

        if (Target != null && entries.TryGetValue(Target, out Entry current))
        {
            if (topThreat <= current.Threat * (1.0f + switchMargin)) return Target;
        }

        Target = top;
        return Target;
    }

    public void Clear()
    {
        entries.Clear();
        Target = null;
    }

    public static bool IsDead(Transform who)
    {
        return who.TryGetComponent(out Health health) && health.IsDead;
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        Vector3 offset = b - a;
        offset.y = 0.0f;
        return offset.magnitude;
    }
}
