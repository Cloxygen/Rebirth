using System.Collections.Generic;
using UnityEngine;

public readonly struct AbilityButton
{
    public readonly bool Held;
    public readonly bool PressedThisTick;

    public AbilityButton(bool held, bool pressedThisTick)
    {
        Held = held;
        PressedThisTick = pressedThisTick;
    }
}

public abstract class Ability
{
    private const float FacingTolerance = 8.0f;

    protected readonly Transform body;
    protected readonly Aim aim;
    protected readonly Modifiers modifiers;
    protected readonly EquipmentLoadout equipment;
    protected readonly PlayerMovement movement;
    protected readonly AimIndicator indicator;

    private readonly GameObject indicatorObject;
    private readonly List<HitEffect> onHitEffects = new List<HitEffect>();

    private float cooldownRemaining;
    private float cooldownDuration;

    public AbilityDefinition Definition { get; }

    public bool IsRunning { get; private set; }

    public bool IsReady => cooldownRemaining <= 0.0f;

    public float CooldownFraction => cooldownDuration > 0.0f
        ? Mathf.Clamp01(cooldownRemaining / cooldownDuration)
        : 0.0f;

    protected Ability(AbilityDefinition definition, GameObject caster)
    {
        Definition = definition;

        body = caster.transform;
        aim = caster.GetComponent<Aim>();
        modifiers = caster.GetComponent<Modifiers>();
        equipment = caster.GetComponent<EquipmentLoadout>();
        movement = caster.GetComponent<PlayerMovement>();

        if (definition.indicatorPrefab == null) return;

        indicatorObject = Object.Instantiate(definition.indicatorPrefab, caster.transform);
        indicatorObject.name = definition.indicatorPrefab.name;
        indicator = indicatorObject.GetComponentInChildren<AimIndicator>(true);
        indicator?.Hide();
    }

    public void Begin()
    {
        cooldownDuration = ScaleDuration(Definition.cooldown);
        cooldownRemaining = cooldownDuration;
        IsRunning = true;

        BeginAttack();
    }

    public void Advance(float seconds, AbilityButton button)
    {
        if (cooldownRemaining > 0.0f) cooldownRemaining -= seconds;

        if (!IsRunning)
        {
            AdvanceWhileIdle(seconds);
            return;
        }

        RestrictMovement();
        AdvanceAttack(seconds, button);
    }

    public void Draw(float seconds)
    {
        if (IsRunning) DrawAttack(seconds);
    }

    public void Stop()
    {
        if (!IsRunning) return;

        IsRunning = false;
        EndAttack();
    }

    public void Discard()
    {
        Stop();

        if (indicatorObject != null) Object.Destroy(indicatorObject);
    }

    protected abstract void BeginAttack();

    protected abstract void AdvanceAttack(float seconds, AbilityButton button);

    protected virtual void DrawAttack(float seconds)
    {
    }

    protected virtual void EndAttack()
    {
    }

    protected virtual void AdvanceWhileIdle(float seconds)
    {
    }

    private void RestrictMovement()
    {
        if (movement == null) return;

        movement.BlockDash();
        movement.Slow(Definition.movementSpeedMultiplier);

        SteerFacing();
    }

    protected virtual void SteerFacing()
    {
        if (Definition.aimsAtCursor) movement.FaceAimPoint();
    }

    protected bool HasTurnedToAim(float secondsWaited)
    {
        if (!Definition.aimsAtCursor || aim == null) return true;
        if (secondsWaited >= Definition.turnDuration) return true;

        return IsFacingYaw(YawToward(aim.Point));
    }

    protected bool IsFacingYaw(float yaw)
    {
        return Mathf.Abs(Mathf.DeltaAngle(body.eulerAngles.y, yaw)) <= FacingTolerance;
    }

    protected float YawToward(Vector3 point)
    {
        Vector3 toward = point - body.position;
        if (toward.x * toward.x + toward.z * toward.z <= 0.01f) return body.eulerAngles.y;

        return Mathf.Atan2(toward.x, toward.z) * Mathf.Rad2Deg;
    }

    protected Damage ComposeDamage(float multiplier)
    {
        Damage pool = HeldWeaponDamage.Plus(Definition.AuthoredDamage);
        if (modifiers != null) pool = pool.Plus(modifiers.AddedDamage);

        Damage scaled = pool.Scaled(multiplier);
        if (modifiers != null) scaled = modifiers.ApplyIncreases(scaled);

        return scaled.WithNothingBelowZero();
    }

    private Damage HeldWeaponDamage => equipment != null ? equipment.HeldDamage : default;

    protected float ScaleDuration(float seconds)
    {
        float speed = modifiers != null ? modifiers.AttackSpeed : 1.0f;

        return speed > 0.01f ? seconds / speed : seconds;
    }

    protected IReadOnlyList<HitEffect> CollectOnHitEffects()
    {
        onHitEffects.Clear();

        if (modifiers == null) return onHitEffects;

        foreach (HitEffect effect in modifiers.OnHitEffects)
        {
            if (effect != null && !onHitEffects.Contains(effect)) onHitEffects.Add(effect);
        }

        return onHitEffects;
    }

    protected int ExtraProjectiles => modifiers != null ? Mathf.Max(0, modifiers.ExtraProjectiles) : 0;

    protected int PierceCount => modifiers != null ? Mathf.Max(0, modifiers.PierceCount) : 0;

    protected float AddedBlastRadius => modifiers != null ? Mathf.Max(0.0f, modifiers.AddedBlastRadius) : 0.0f;
}
