using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Rebirth/Effects/Bleed", fileName = "Bleed")]
public class BleedEffect : HitEffect
{
    public Damage damagePerTick = new Damage(DamageType.Piercing, 2.0f);
    public float tickInterval = 0.5f;
    public int tickCount = 4;

    public override void OnHit(Health target, Vector3 hitPoint, Transform attacker)
    {
        if (target == null || target.IsDead) return;

        BleedTicker ticker = target.gameObject.GetComponent<BleedTicker>();
        if (ticker == null) ticker = target.gameObject.AddComponent<BleedTicker>();

        ticker.Apply(target, damagePerTick, tickInterval, tickCount, attacker);
    }
}

public class BleedTicker : MonoBehaviour
{
    private Health target;
    private Transform attacker;
    private Damage damagePerTick;
    private float tickInterval;
    private int ticksRemaining;
    private Coroutine running;

    public void Apply(Health health, Damage damage, float interval, int ticks, Transform source)
    {
        target = health;
        attacker = source;
        if (damage.Total > damagePerTick.Total) damagePerTick = damage;
        tickInterval = interval;
        ticksRemaining = Mathf.Max(ticksRemaining, ticks);

        if (running == null) running = StartCoroutine(Tick());
    }

    private IEnumerator Tick()
    {
        while (ticksRemaining > 0 && target != null && !target.IsDead)
        {
            yield return new WaitForSeconds(tickInterval);

            ticksRemaining--;
            if (target != null && !target.IsDead)
            {
                target.TakeDamage(damagePerTick, target.transform.position, 0.0f, attacker);
            }
        }

        running = null;
        damagePerTick = default;
    }
}
