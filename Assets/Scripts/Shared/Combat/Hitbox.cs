using System;
using System.Collections.Generic;
using UnityEngine;

public class Hitbox
{
    private readonly Transform attacker;
    private readonly Vector3 halfExtents;
    private readonly LayerMask hittableLayers;
    private readonly Damage damage;
    private readonly float knockbackDistance;
    private readonly HitEffect[] onHitEffects;
    private readonly HashSet<Hurtbox> alreadyHit = new HashSet<Hurtbox>();

    private Vector3 lastSweptTo;
    private bool hasSweptOnce;

    public event Action<Hurtbox, Vector3> Struck;

    public Hitbox(Transform attacker, Vector3 worldSize, LayerMask hittableLayers, Damage damage,
                  float knockbackDistance, IReadOnlyList<HitEffect> onHitEffects)
    {
        this.attacker = attacker;
        this.hittableLayers = hittableLayers;
        this.damage = damage;
        this.knockbackDistance = knockbackDistance;
        this.onHitEffects = CopyOf(onHitEffects);

        halfExtents = worldSize * 0.5f;
    }

    public void SweepTo(Vector3 position, Quaternion rotation)
    {
        Physics.SyncTransforms();

        Vector3 startedAt = hasSweptOnce ? lastSweptTo : position;
        lastSweptTo = position;
        hasSweptOnce = true;

        ResolveBodiesTouchingAt(position, rotation);
        ResolveBodiesSweptBetween(startedAt, position, rotation);
    }

    private static HitEffect[] CopyOf(IReadOnlyList<HitEffect> effects)
    {
        if (effects == null || effects.Count == 0) return Array.Empty<HitEffect>();

        HitEffect[] copy = new HitEffect[effects.Count];
        for (int index = 0; index < effects.Count; index++) copy[index] = effects[index];

        return copy;
    }

    private void ResolveBodiesTouchingAt(Vector3 position, Quaternion rotation)
    {
        Collider[] touching = Physics.OverlapBox(position, halfExtents, rotation,
                                                 hittableLayers, QueryTriggerInteraction.Collide);

        foreach (Collider collider in touching) ResolveBodyBehind(collider, position);
    }

    private void ResolveBodiesSweptBetween(Vector3 startedAt, Vector3 arrivedAt, Quaternion rotation)
    {
        Vector3 travelled = arrivedAt - startedAt;
        float distance = travelled.magnitude;
        if (distance <= 0.0001f) return;

        RaycastHit[] sweptThrough = Physics.BoxCastAll(startedAt, halfExtents, travelled / distance,
                                                       rotation, distance, hittableLayers,
                                                       QueryTriggerInteraction.Collide);

        foreach (RaycastHit hit in sweptThrough) ResolveBodyBehind(hit.collider, arrivedAt);
    }

    private void ResolveBodyBehind(Collider collider, Vector3 boxPosition)
    {
        if (attacker != null && collider.transform.IsChildOf(attacker)) return;

        Hurtbox target = Hurtbox.FindByCollider(collider);
        if (target == null || !alreadyHit.Add(target)) return;

        Vector3 contactPoint = collider.ClosestPoint(boxPosition);

        Struck?.Invoke(target, contactPoint);

        if (!Health.DamageIsDecidedHere || damage.IsNothing) return;

        target.Health.TakeDamage(damage, contactPoint, knockbackDistance, attacker);

        foreach (HitEffect effect in onHitEffects)
        {
            if (effect != null) effect.OnHit(target.Health, contactPoint, attacker);
        }
    }
}
