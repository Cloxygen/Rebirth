using System;
using System.Collections.Generic;
using UnityEngine;

public class Modifiers : MonoBehaviour
{
    [SerializeField] private ModifierSet baseModifiers = new ModifierSet();

    private readonly List<ModifierSet> sources = new List<ModifierSet>();
    private readonly List<HitEffect> onHitEffects = new List<HitEffect>();
    private readonly List<DamageIncrease> increases = new List<DamageIncrease>();

    public Damage AddedDamage { get; private set; }

    public float AttackSpeed { get; private set; }

    public int ExtraProjectiles { get; private set; }
    public int PierceCount { get; private set; }
    public float AddedBlastRadius { get; private set; }
    public IReadOnlyList<HitEffect> OnHitEffects => onHitEffects;

    public event Action Changed;

    private void Awake()
    {
        Recalculate();
    }

    public void Register(ModifierSet set)
    {
        if (set == null || sources.Contains(set)) return;

        sources.Add(set);
        Recalculate();
    }

    public void Unregister(ModifierSet set)
    {
        if (set == null || !sources.Remove(set)) return;

        Recalculate();
    }

    public void Recalculate()
    {
        AddedDamage = baseModifiers.addedDamage;
        AttackSpeed = 1.0f + baseModifiers.addedAttackSpeed;
        ExtraProjectiles = baseModifiers.extraProjectiles;
        PierceCount = baseModifiers.pierceCount;
        AddedBlastRadius = baseModifiers.addedBlastRadius;

        onHitEffects.Clear();
        increases.Clear();
        Collect(baseModifiers);

        foreach (ModifierSet set in sources)
        {
            if (set == null) continue;

            AddedDamage = AddedDamage.Plus(set.addedDamage);
            AttackSpeed += set.addedAttackSpeed;
            ExtraProjectiles += set.extraProjectiles;
            PierceCount += set.pierceCount;
            AddedBlastRadius += set.addedBlastRadius;
            Collect(set);
        }

        AttackSpeed = Mathf.Max(0.05f, AttackSpeed);

        Changed?.Invoke();
    }

    public Damage ApplyIncreases(Damage damage)
    {
        List<DamageAmount> raised = new List<DamageAmount>();

        foreach (DamageAmount entry in damage.Amounts)
        {
            float multiplier = Mathf.Max(0.0f, 1.0f + PercentFor(entry.type) / 100.0f);
            raised.Add(new DamageAmount(entry.type, entry.amount * multiplier));
        }

        return new Damage(raised.ToArray());
    }

    private float PercentFor(DamageType type)
    {
        float percent = 0.0f;

        foreach (DamageIncrease increase in increases)
        {
            if (type.IsUnder(increase.type)) percent += increase.percent;
        }

        return percent;
    }

    private void Collect(ModifierSet set)
    {
        if (set.increasedDamage != null) increases.AddRange(set.increasedDamage);

        if (set.onHitEffects == null) return;

        foreach (HitEffect effect in set.onHitEffects)
        {
            if (effect != null && !onHitEffects.Contains(effect)) onHitEffects.Add(effect);
        }
    }
}
