using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public struct DamageIncrease
{
    public DamageType type;
    public float percent;
}

[System.Serializable]
public class ModifierSet
{
    [Header("Offence")]
    [FormerlySerializedAs("damage")]
    public Damage addedDamage;
    public DamageIncrease[] increasedDamage;
    public float addedAttackSpeed;

    [Header("Projectiles")]
    public int extraProjectiles;
    public int pierceCount;

    [Header("Area")]
    public float addedBlastRadius;

    [Header("On Hit")]
    public HitEffect[] onHitEffects;

    public ModifierSet Copy()
    {
        return new ModifierSet
        {
            addedDamage = addedDamage,
            increasedDamage = increasedDamage,
            addedAttackSpeed = addedAttackSpeed,
            extraProjectiles = extraProjectiles,
            pierceCount = pierceCount,
            addedBlastRadius = addedBlastRadius,
            onHitEffects = onHitEffects,
        };
    }
}
