using System.Collections.Generic;
using UnityEngine;

public class FlatShot : Ability
{
    private const float MinimumAimRun = 0.5f;

    private readonly FlatShotAttack definition;

    private float drawDuration;
    private float drawn;

    public FlatShot(FlatShotAttack definition, GameObject caster) : base(definition, caster)
    {
        this.definition = definition;
    }

    protected override void BeginAttack()
    {
        StartDraw();
    }

    protected override void AdvanceAttack(float seconds, AbilityButton button)
    {
        if (!button.Held)
        {
            Stop();
            return;
        }

        drawn += seconds;
        if (drawn < drawDuration) return;

        Loose();

        if (Definition.repeatsWhileHeld) StartDraw();
        else Stop();
    }

    private void StartDraw()
    {
        drawDuration = ScaleDuration(definition.attackDuration);
        drawn = 0.0f;
    }

    private void Loose()
    {
        float spread = Mathf.Max(0.0f, definition.spreadDegrees);
        BallisticCurve curve = BuildCurve();

        Damage damage = ComposeDamage(1.0f);
        IReadOnlyList<HitEffect> effects = CollectOnHitEffects();

        int arrows = 1 + ExtraProjectiles;
        for (int index = 0; index < arrows; index++)
        {
            float fan = (index - (arrows - 1) * 0.5f) * definition.degreesBetweenProjectiles;
            float yaw = fan + RollScatter(spread);

            Shot.Launch(Fanned(curve, yaw), body, definition.hittableLayers, damage,
                        AddedBlastRadius, definition.projectile, effects, PierceCount,
                        definition.knockbackDistance);
        }
    }

    private static float RollScatter(float spread)
    {
        return spread > 0.0f ? Random.Range(-spread, spread) : 0.0f;
    }

    private Vector3 LaunchPosition =>
        aim.GetLaunchPosition(definition.launchHeightAboveGround, definition.launchForwardOffset);

    private BallisticCurve BuildCurve()
    {
        float climb = GetLaunchPitch() * Mathf.Deg2Rad;
        Vector3 aimed = body.forward * Mathf.Cos(climb) + Vector3.up * Mathf.Sin(climb);

        return Ballistics.CurveFiredAlong(LaunchPosition, aimed, definition.launchSpeed,
                                          definition.projectile.gravity);
    }

    private float GetLaunchPitch()
    {
        Vector3 toward = aim.Point + Vector3.up * definition.launchHeightAboveGround - LaunchPosition;
        float run = new Vector2(toward.x, toward.z).magnitude;

        if (run < MinimumAimRun) return definition.launchPitchDegrees;

        return Mathf.Atan2(toward.y, run) * Mathf.Rad2Deg + definition.launchPitchDegrees;
    }

    private BallisticCurve Fanned(in BallisticCurve curve, float yawDegrees)
    {
        Vector3 turned = Quaternion.AngleAxis(yawDegrees, Vector3.up) * curve.LaunchVelocity;

        return Ballistics.CurveFiredAlong(curve.LaunchPosition, turned, definition.launchSpeed,
                                          definition.projectile.gravity);
    }
}
