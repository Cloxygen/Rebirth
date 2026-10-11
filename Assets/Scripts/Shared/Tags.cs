using System;
using System.Collections.Generic;
using UnityEngine;

public enum Tag
{
    Weapon,
    Melee,
    Ranged,
    Blunt,
    Slashing,
    Piercing,
    Magic,
    TwoHanded,
    Spell,
    Incantation,
    Projectile,
    Physical,
    Fire,
    Curse,
}

[Serializable]
public struct Tags
{
    [SerializeField] private Tag[] tags;

    public Tags(params Tag[] tags)
    {
        this.tags = tags;
    }

    public bool Has(Tag wanted)
    {
        if (tags == null) return false;

        foreach (Tag tag in tags)
        {
            if (tag == wanted) return true;
        }
        return false;
    }

    public bool HasAll(Tags wanted)
    {
        if (wanted.tags == null) return true;

        foreach (Tag tag in wanted.tags)
        {
            if (!Has(tag)) return false;
        }
        return true;
    }

    public bool HasNone(Tags unwanted)
    {
        if (unwanted.tags == null) return true;

        foreach (Tag tag in unwanted.tags)
        {
            if (Has(tag)) return false;
        }
        return true;
    }

    public Tags Plus(params Tag[] added) => Plus(new Tags(added));

    public Tags Plus(Tags added)
    {
        List<Tag> combined = new List<Tag>();
        AddMissingTo(combined);
        added.AddMissingTo(combined);
        return new Tags(combined.ToArray());
    }

    private void AddMissingTo(List<Tag> combined)
    {
        if (tags == null) return;

        foreach (Tag tag in tags)
        {
            if (!combined.Contains(tag)) combined.Add(tag);
        }
    }
}
