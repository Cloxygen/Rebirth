using System;
using System.Collections.Generic;
using FishNet.Serializing;
using UnityEngine;

[Serializable]
public struct DamageAmount
{
    public DamageType type;
    public float amount;

    public DamageAmount(DamageType type, float amount)
    {
        this.type = type;
        this.amount = amount;
    }
}

[Serializable]
public struct Damage
{
    [SerializeField] private DamageAmount[] amounts;

    public Damage(params DamageAmount[] amounts)
    {
        this.amounts = amounts;
    }

    public Damage(DamageType type, float amount) : this(new DamageAmount(type, amount))
    {
    }

    public IReadOnlyList<DamageAmount> Amounts => amounts ?? Array.Empty<DamageAmount>();

    public float Total
    {
        get
        {
            float total = 0.0f;
            foreach (DamageAmount entry in Amounts) total += entry.amount;
            return total;
        }
    }

    public bool IsNothing => Total <= 0.0f;

    public float Of(DamageType category)
    {
        float total = 0.0f;
        foreach (DamageAmount entry in Amounts)
        {
            if (entry.type.IsUnder(category)) total += entry.amount;
        }
        return total;
    }

    public Damage Scaled(float multiplier)
    {
        List<DamageAmount> scaled = new List<DamageAmount>();
        foreach (DamageAmount entry in Amounts)
        {
            scaled.Add(new DamageAmount(entry.type, entry.amount * multiplier));
        }
        return new Damage(scaled.ToArray());
    }

    public Damage Plus(Damage other)
    {
        List<DamageAmount> combined = new List<DamageAmount>();
        AddTo(combined);
        other.AddTo(combined);
        return new Damage(combined.ToArray());
    }

    public Damage WithNothingBelowZero()
    {
        List<DamageAmount> kept = new List<DamageAmount>();
        foreach (DamageAmount entry in Amounts)
        {
            if (entry.amount > 0.0f) kept.Add(entry);
        }
        return new Damage(kept.ToArray());
    }

    private void AddTo(List<DamageAmount> combined)
    {
        foreach (DamageAmount entry in Amounts)
        {
            int existing = IndexOfType(combined, entry.type);
            if (existing < 0) combined.Add(entry);
            else combined[existing] = new DamageAmount(entry.type, combined[existing].amount + entry.amount);
        }
    }

    private static int IndexOfType(List<DamageAmount> entries, DamageType type)
    {
        for (int index = 0; index < entries.Count; index++)
        {
            if (entries[index].type == type) return index;
        }
        return -1;
    }
}

public static class DamageSerializer
{
    public static void WriteDamage(this Writer writer, Damage value)
    {
        IReadOnlyList<DamageAmount> amounts = value.Amounts;

        writer.WriteInt32(amounts.Count);
        foreach (DamageAmount entry in amounts)
        {
            writer.WriteInt32((int)entry.type);
            writer.WriteSingle(entry.amount);
        }
    }

    public static Damage ReadDamage(this Reader reader)
    {
        DamageAmount[] amounts = new DamageAmount[reader.ReadInt32()];

        for (int index = 0; index < amounts.Length; index++)
        {
            DamageType type = (DamageType)reader.ReadInt32();
            amounts[index] = new DamageAmount(type, reader.ReadSingle());
        }
        return new Damage(amounts);
    }
}
