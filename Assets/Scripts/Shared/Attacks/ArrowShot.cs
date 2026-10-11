using System.Collections.Generic;
using UnityEngine;

public class ArrowShot : Ability
{
    private const float CursorSurfaceTolerance = 0.1f;

    private const float MinimumLedgeRise = 0.05f;

    private readonly ArrowShotAttack definition;

    private float drawDuration;
    private float drawn;
    private float charge;

    public ArrowShot(ArrowShotAttack definition, GameObject caster) : base(definition, caster)
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
        charge = drawDuration > 0.0f ? Mathf.Clamp01(drawn / drawDuration) : 1.0f;

        indicator?.Show(BuildAimShot());

        if (drawn < drawDuration) return;

        Loose();

        if (Definition.repeatsWhileHeld) StartDraw();
        else Stop();
    }

    protected override void EndAttack()
    {
        indicator?.Hide();
        charge = 0.0f;
    }

    private void StartDraw()
    {
        drawDuration = ScaleDuration(definition.attackDuration);
        drawn = 0.0f;
        charge = 0.0f;
    }

    private void Loose()
    {
        charge = 1.0f;
        indicator?.Hide();

        float spread = Mathf.Max(0.0f, definition.spreadDegrees);
        BallisticCurve curve = BuildCurve();
        Damage damage = ComposeDamage(definition.damageMultiplier);
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

    private Vector3 ClearObstruction(Vector3 cursorPoint)
    {
        Vector3 launch = LaunchPosition;
        Vector3 toward = cursorPoint - launch;
        float distance = toward.magnitude;

        if (distance <= 0.001f) return cursorPoint;

        Vector3 direction = toward / distance;
        int layers = aim.LineOfSightLayers;

        float sightDistance = distance - CursorSurfaceTolerance;
        if (sightDistance <= 0.0f) return cursorPoint;

        if (!Physics.Raycast(launch, direction, out RaycastHit blocker, sightDistance, layers,
                             QueryTriggerInteraction.Ignore))
        {
            return cursorPoint;
        }

        if (!TryFindLedgeTop(blocker.point, direction, layers, out Vector3 ledgeTop)) return cursorPoint;

        return ledgeTop + Vector3.up * definition.ledgeClearance;
    }

    private bool TryFindLedgeTop(Vector3 blockedAt, Vector3 direction, int layers, out Vector3 ledgeTop)
    {
        ledgeTop = blockedAt;

        Vector3 heading = new Vector3(direction.x, 0.0f, direction.z);
        if (heading.sqrMagnitude <= 0.0001f) return false;

        Vector3 beyondFace = blockedAt + heading.normalized * definition.ledgeEdgeInset;
        float searchHeight = definition.maximumLedgeSearchHeight;

        if (!Physics.Raycast(beyondFace + Vector3.up * searchHeight, Vector3.down,
                             out RaycastHit surface, searchHeight, layers,
                             QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        if (surface.point.y - blockedAt.y < MinimumLedgeRise) return false;

        ledgeTop = surface.point;
        return true;
    }

    private BallisticCurve BuildCurve()
    {
        Vector3 launch = LaunchPosition;
        Vector3 aimPoint = ClearObstruction(aim.Point);

        return Ballistics.CurveThatLandsOn(launch, aimPoint, definition.launchSpeed, definition.projectile.gravity)
                         .WithLaunchAngleBetween(definition.minimumLaunchAngle, definition.maximumLaunchAngle);
    }

    private BallisticCurve Fanned(in BallisticCurve curve, float yawDegrees)
    {
        Vector3 turned = Quaternion.AngleAxis(yawDegrees, Vector3.up) * curve.LaunchVelocity;

        return Ballistics.CurveFiredAlong(curve.LaunchPosition, turned, definition.launchSpeed,
                                          definition.projectile.gravity);
    }

    private AimShot BuildAimShot()
    {
        BallisticCurve curve = BuildCurve();

        return new AimShot
        {
            Curve = curve,
            SpreadDegrees = Mathf.Max(0.0f, definition.spreadDegrees),
            SweepRadius = Mathf.Max(definition.projectile.size.x, definition.projectile.size.y) * 0.5f,
            SweepLayers = aim.SweepLayers(definition.hittableLayers),
            Duration = GetPreviewDuration(curve),
            Charge = charge
        };
    }

    private float GetPreviewDuration(in BallisticCurve curve)
    {
        Vector3 launch = LaunchPosition;
        Vector3 groundPoint = aim.Point;
        float gravity = definition.projectile.gravity;
        float verticalSpeed = curve.LaunchVelocity.y;
        float dropToFloor = launch.y - groundPoint.y;

        if (gravity <= 0.0001f)
        {
            float horizontalSpeed = new Vector2(curve.LaunchVelocity.x, curve.LaunchVelocity.z).magnitude;
            if (horizontalSpeed <= 0.01f) return definition.maximumPreviewDuration;

            Vector3 toward = groundPoint - launch;
            return Mathf.Min(definition.maximumPreviewDuration,
                             new Vector2(toward.x, toward.z).magnitude / horizontalSpeed);
        }

        float discriminant = verticalSpeed * verticalSpeed + 2.0f * gravity * dropToFloor;
        if (discriminant < 0.0f) return definition.maximumPreviewDuration;

        float timeToFloorHeight = (verticalSpeed + Mathf.Sqrt(discriminant)) / gravity;

        return Mathf.Clamp(timeToFloorHeight, 0.0f, definition.maximumPreviewDuration);
    }
}
