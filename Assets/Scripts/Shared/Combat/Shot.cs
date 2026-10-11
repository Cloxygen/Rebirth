using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public static class Shot
{
    public static void Launch(in BallisticCurve curve, Transform shooter, LayerMask hittableLayers,
                              Damage damage, float blastRadius, Projectile.Settings settings,
                              IReadOnlyList<HitEffect> onHitEffects, int pierceCount,
                              float knockbackDistance)
    {
        if (Health.DamageIsDecidedHere)
        {
            Projectile.Spawn(curve, shooter, hittableLayers, damage, blastRadius, settings,
                             onHitEffects, pierceCount, knockbackDistance);
            return;
        }

        if (WasLoosedByThisPlayer(shooter)) PredictedShot.Spawn(curve, shooter, hittableLayers, settings);
    }

    private static bool WasLoosedByThisPlayer(Transform shooter)
    {
        NetworkObject shooterObject = shooter != null ? shooter.GetComponentInParent<NetworkObject>() : null;

        return shooterObject != null && shooterObject.IsOwner;
    }
}
