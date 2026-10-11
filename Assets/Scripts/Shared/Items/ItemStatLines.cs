using System.Collections.Generic;

public static class ItemStatLines
{
    private const string WeaponDamageLine = "{0:0.##} {1} damage";
    private const string AddedDamageLine = "+{0:0.##} {1} damage";
    private const string IncreasedDamageLine = "{0:+0.#;-0.#}% increased {1} damage";
    private const string AttackSpeedLine = "{0:+0.#;-0.#}% attack speed";
    private const string ExtraProjectilesLine = "+{0} projectiles";
    private const string PierceLine = "Pierce {0}";
    private const string BlastLine = "Blast {0:0.##}";
    private const string GrantsAbilityLine = "Grants {0}";
    private const string ManaCostLine = "{0:0.##} mana";
    private const string CooldownLine = "{0:0.##}s cooldown";

    public static void Describe(ItemDefinition item, List<string> lines)
    {
        lines.Clear();
        DescribeWeaponDamage(item as Weapon, lines);
        DescribeModifiers(item.modifiers, lines);
        DescribeAbility(item.ability, lines);
    }

    private static void DescribeWeaponDamage(Weapon weapon, List<string> lines)
    {
        if (weapon == null) return;

        foreach (DamageAmount damage in weapon.damage.Amounts)
        {
            if (damage.amount > 0.0f) lines.Add(string.Format(WeaponDamageLine, damage.amount, damage.type));
        }
    }

    private static void DescribeModifiers(ModifierSet modifiers, List<string> lines)
    {
        if (modifiers == null) return;

        DescribeAddedDamage(modifiers.addedDamage, lines);
        DescribeIncreasedDamage(modifiers.increasedDamage, lines);
        DescribeAttackSpeed(modifiers.addedAttackSpeed, lines);
        DescribeProjectiles(modifiers, lines);
        DescribeBlast(modifiers.addedBlastRadius, lines);
    }

    private static void DescribeAddedDamage(Damage addedDamage, List<string> lines)
    {
        foreach (DamageAmount damage in addedDamage.Amounts)
        {
            if (damage.amount > 0.0f) lines.Add(string.Format(AddedDamageLine, damage.amount, damage.type));
        }
    }

    private static void DescribeIncreasedDamage(DamageIncrease[] increases, List<string> lines)
    {
        if (increases == null) return;

        foreach (DamageIncrease increase in increases)
        {
            if (increase.percent != 0.0f) lines.Add(string.Format(IncreasedDamageLine, increase.percent, increase.type));
        }
    }

    private static void DescribeAttackSpeed(float addedAttackSpeed, List<string> lines)
    {
        if (addedAttackSpeed != 0.0f) lines.Add(string.Format(AttackSpeedLine, addedAttackSpeed * 100.0f));
    }

    private static void DescribeProjectiles(ModifierSet modifiers, List<string> lines)
    {
        if (modifiers.extraProjectiles > 0) lines.Add(string.Format(ExtraProjectilesLine, modifiers.extraProjectiles));
        if (modifiers.pierceCount > 0) lines.Add(string.Format(PierceLine, modifiers.pierceCount));
    }

    private static void DescribeBlast(float addedBlastRadius, List<string> lines)
    {
        if (addedBlastRadius > 0.0f) lines.Add(string.Format(BlastLine, addedBlastRadius));
    }

    private static void DescribeAbility(AbilityDefinition ability, List<string> lines)
    {
        if (ability == null) return;

        lines.Add(string.Format(GrantsAbilityLine, ability.name));
        DescribeAbilityCost(ability, lines);
    }

    private static void DescribeAbilityCost(AbilityDefinition ability, List<string> lines)
    {
        if (ability.manaCost > 0.0f) lines.Add(string.Format(ManaCostLine, ability.manaCost));
        if (ability.cooldown > 0.0f) lines.Add(string.Format(CooldownLine, ability.cooldown));
    }
}
