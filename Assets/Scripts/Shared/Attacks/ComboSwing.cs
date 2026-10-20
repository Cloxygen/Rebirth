using UnityEngine;

public class ComboSwing : Ability
{
    private enum Stage
    {
        Turning,
        Swinging,
        Recovering
    }

    private readonly ComboSwingAttack definition;

    private int comboIndex;
    private float comboResetTimer;

    private Stage stage;
    private float swingYaw;
    private float stageElapsed;
    private ComboSwingAttack.Swing swing;
    private MeleeSwing blade;
    private float bladeDuration;
    private float recoveryDuration;

    public ComboSwing(ComboSwingAttack definition, GameObject caster) : base(definition, caster)
    {
        this.definition = definition;
    }

    protected override void BeginAttack()
    {
        StartTurning();
    }

    protected override void AdvanceAttack(float seconds, AbilityButton button)
    {
        Stage before = stage;

        AdvanceStage(seconds, button);

        if (IsRunning && stage != before) AdvanceStage(0.0f, button);
    }

    private void AdvanceStage(float seconds, AbilityButton button)
    {
        switch (stage)
        {
            case Stage.Turning: AdvanceTurn(seconds); break;
            case Stage.Swinging: AdvanceBlade(seconds); break;
            case Stage.Recovering: AdvanceRecovery(seconds, button); break;
        }
    }

    protected override void SteerFacing()
    {
        movement.FaceYaw(swingYaw);
    }

    protected override void DrawAttack(float seconds)
    {
        blade?.Draw(seconds);
    }

    protected override void EndAttack()
    {
        blade?.Stop();
        blade = null;
    }

    protected override void AdvanceWhileIdle(float seconds)
    {
        if (comboResetTimer <= 0.0f) return;

        comboResetTimer -= seconds;
        if (comboResetTimer <= 0.0f) comboIndex = 0;
    }

    private void StartTurning()
    {
        swing = SelectSwing();
        swingYaw = aim != null ? YawToward(aim.Point) : body.eulerAngles.y;
        stage = Stage.Turning;
        stageElapsed = 0.0f;
    }

    private void AdvanceTurn(float seconds)
    {
        stageElapsed += seconds;

        if (stageElapsed < Definition.turnDuration && !IsFacingYaw(swingYaw)) return;

        ThrowSwing();
    }

    private void ThrowSwing()
    {
        Hitbox hitbox = new Hitbox(body, definition.hitboxSize, aim.HittableLayers,
                                   ComposeDamage(swing.damageMultiplier), definition.knockbackDistance,
                                   CollectOnHitEffects());
        hitbox.Struck += NoteContact;

        MeleeSwing.Arc arc = new MeleeSwing.Arc(definition.hitboxSize, definition.hitboxForwardOffset,
                                                swing.swingArcDegrees, swing.swingDirection,
                                                definition.windupColor, definition.hitboxColor);

        bladeDuration = ScaleDuration(swing.swingDuration);
        blade = new MeleeSwing(body, hitbox, arc, ScaleDuration(swing.windupDuration), bladeDuration);

        ItemSpawner.NoteSwing();

        stage = Stage.Swinging;
    }

    private void AdvanceBlade(float seconds)
    {
        blade.Advance(seconds);

        if (blade.BladeIsOut) Lunge();

        if (!blade.IsFinished) return;

        blade = null;
        recoveryDuration = ScaleDuration(swing.recoveryDuration);
        stage = Stage.Recovering;
        stageElapsed = 0.0f;
    }

    private void Lunge()
    {
        if (movement == null || swing.lungeDistance == 0.0f || bladeDuration <= 0.0f) return;

        float frontLoaded = 2.0f * (1.0f - blade.BladeProgress);
        movement.Lunge(swing.lungeDistance * frontLoaded / bladeDuration);
    }

    private void AdvanceRecovery(float seconds, AbilityButton button)
    {
        stageElapsed += seconds;

        bool wantsAnother = button.PressedThisTick || (Definition.repeatsWhileHeld && button.Held);
        bool recovered = stageElapsed >= recoveryDuration;

        if (!recovered && (!wantsAnother || IsFinalSwing)) return;

        AdvanceCombo();

        if (wantsAnother) StartTurning();
        else Stop();
    }

    private ComboSwingAttack.Swing SelectSwing()
    {
        if (definition.swings == null || definition.swings.Length == 0) return new ComboSwingAttack.Swing();

        return definition.swings[Mathf.Clamp(comboIndex, 0, definition.swings.Length - 1)];
    }

    private bool IsFinalSwing => definition.swings == null || comboIndex >= definition.swings.Length - 1;

    private void AdvanceCombo()
    {
        if (definition.swings == null || definition.swings.Length == 0) return;

        comboIndex = (comboIndex + 1) % definition.swings.Length;
        comboResetTimer = definition.comboWindow;
    }

    private static void NoteContact(Hurtbox target, Vector3 contactPoint)
    {
        ItemSpawner.NoteContact(target.Health);
    }
}
