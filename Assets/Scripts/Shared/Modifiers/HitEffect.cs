using UnityEngine;

public abstract class HitEffect : ScriptableObject
{
    public abstract void OnHit(Health target, Vector3 hitPoint, Transform attacker);
}
