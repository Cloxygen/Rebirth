using System.Collections.Generic;
using UnityEngine;

public class ChargedBolt : Ability
{
    private readonly ChargedBoltAttack definition;

    private float turnWaited;
    private bool turned;
    private float chargeDuration;
    private float charge;

    public ChargedBolt(ChargedBoltAttack definition, GameObject caster) : base(definition, caster)
    {
        this.definition = definition;
    }

    protected override void BeginAttack()
    {
        chargeDuration = ScaleDuration(definition.chargeDuration);
        turnWaited = 0.0f;
        turned = false;
        charge = 0.0f;
    }

    protected override void AdvanceAttack(float seconds, AbilityButton button)
    {
        if (!turned)
        {
            turnWaited += seconds;
            turned = HasTurnedToAim(turnWaited);
            return;
        }

        if (!button.Held)
        {
            Cast();
            Stop();
            return;
        }

        charge = chargeDuration > 0.0f ? Mathf.Min(1.0f, charge + seconds / chargeDuration) : 1.0f;
        indicator?.Show(BuildAimShot());
    }

    protected override void EndAttack()
    {
        indicator?.Hide();
        charge = 0.0f;
    }

    private void Cast()
    {
        indicator?.Hide();

        Damage damage = ComposeDamage(Mathf.Lerp(definition.damageMultiplierAtZeroCharge,
                                                 definition.damageMultiplierAtFullCharge, charge));
        float blast = BlastRadius;
        BallisticCurve curve = BuildCurve();
        IReadOnlyList<HitEffect> effects = CollectOnHitEffects();

        int bolts = 1 + ExtraProjectiles;
        for (int index = 0; index < bolts; index++)
        {
            float yaw = (index - (bolts - 1) * 0.5f) * definition.degreesBetweenProjectiles;

            Shot.Launch(Fanned(curve, yaw), body, definition.hittableLayers, damage, blast,
                        definition.bolt, effects, PierceCount, definition.knockbackDistance);
        }
    }

    private float BlastRadius =>
        Mathf.Lerp(definition.minimumBlastRadius, definition.maximumBlastRadius, charge) + AddedBlastRadius;

    private Vector3 LaunchPosition =>
        aim.GetLaunchPosition(definition.launchHeightAboveGround, definition.launchForwardOffset);

    private BallisticCurve BuildCurve()
    {
        return Ballistics.CurveThatLandsOn(LaunchPosition, aim.Point, definition.boltSpeed,
                                           definition.bolt.gravity);
    }

    private BallisticCurve Fanned(in BallisticCurve curve, float yawDegrees)
    {
        Vector3 fanned = Quaternion.AngleAxis(yawDegrees, Vector3.up) * curve.LaunchVelocity;

        return Ballistics.CurveFiredAlong(curve.LaunchPosition, fanned, definition.boltSpeed,
                                          definition.bolt.gravity);
    }

    private AimShot BuildAimShot()
    {
        return new AimShot
        {
            Curve = BuildCurve(),
            SpreadDegrees = 0.0f,
            GroundRadius = BlastRadius,
            SweepRadius = Mathf.Max(definition.bolt.size.x, definition.bolt.size.y) * 0.5f,
            SweepLayers = aim.SweepLayers(definition.hittableLayers),
            Duration = definition.maximumCastRange / Mathf.Max(0.01f, definition.boltSpeed),
            Charge = charge
        };
    }
}
