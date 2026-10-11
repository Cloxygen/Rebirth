using System;
using UnityEngine;

public enum DamageType
{
    Physical,
    Blunt,
    Slashing,
    Piercing,
    Fire,
    Curse,
}

public static class DamageTypes
{
    public static bool IsUnder(this DamageType type, DamageType category)
    {
        if (type == category) return true;

        return type switch
        {
            DamageType.Blunt or DamageType.Slashing or DamageType.Piercing => category == DamageType.Physical,
            DamageType.Physical or DamageType.Fire or DamageType.Curse => false,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Damage type is not placed in the hierarchy."),
        };
    }

    public static Tag ToTag(this DamageType type)
    {
        return type switch
        {
            DamageType.Physical => Tag.Physical,
            DamageType.Blunt => Tag.Blunt,
            DamageType.Slashing => Tag.Slashing,
            DamageType.Piercing => Tag.Piercing,
            DamageType.Fire => Tag.Fire,
            DamageType.Curse => Tag.Curse,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Damage type has no tag."),
        };
    }

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
#endif
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CheckEveryTypeIsPlaced()
    {
        foreach (DamageType type in Enum.GetValues(typeof(DamageType)))
        {
            type.ToTag();
            foreach (DamageType category in Enum.GetValues(typeof(DamageType))) type.IsUnder(category);
        }
    }
}
