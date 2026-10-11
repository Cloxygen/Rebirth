using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.Events;

public class Health : NetworkBehaviour, IDepletable
{
    [SerializeField] private float maxHealth = 100.0f;

    public UnityEvent<float, float> onHealthChanged;

    public UnityEvent<Damage, Vector3> onDamaged;

    public UnityEvent onDied;

    private readonly SyncVar<float> currentHealth = new SyncVar<float>();
    private readonly SyncVar<bool> dead = new SyncVar<bool>();
    private readonly SyncVar<bool> invulnerable = new SyncVar<bool>();

    private IDamageAbsorber damageAbsorber;

    public float CurrentHealth => currentHealth.Value;
    public float MaxHealth => maxHealth;
    public bool IsDead => dead.Value;
    public bool IsInvulnerable => invulnerable.Value;

    public static bool DamageIsDecidedHere =>
        InstanceFinder.NetworkManager != null && InstanceFinder.NetworkManager.IsServerStarted;

    public void SetInvulnerable(bool value)
    {
        if (!IsServerInitialized) return;

        invulnerable.Value = value;
    }

    public void Revive()
    {
        if (!IsServerInitialized) return;

        currentHealth.Value = maxHealth;
        dead.Value = false;
    }

    float IDepletable.Current => currentHealth.Value;
    float IDepletable.Max => maxHealth;
    UnityEvent<float, float> IDepletable.Changed => onHealthChanged;

    public float LastHitKnockback { get; private set; }

    public Transform LastHitAttacker { get; private set; }

    private void Awake()
    {
        damageAbsorber = GetComponent<IDamageAbsorber>();

        currentHealth.OnChange += HandleHealthSynced;
        dead.OnChange += HandleDeathSynced;
    }

    private void OnDestroy()
    {
        currentHealth.OnChange -= HandleHealthSynced;
        dead.OnChange -= HandleDeathSynced;
    }

    public override void OnStartServer()
    {
        currentHealth.Value = maxHealth;
    }

    public override void OnStartClient()
    {
        onHealthChanged?.Invoke(currentHealth.Value, maxHealth);
    }

    private bool IsDuplicateHostCallback(bool asServer) => asServer && IsHostInitialized;

    private void HandleHealthSynced(float previous, float next, bool asServer)
    {
        if (IsDuplicateHostCallback(asServer)) return;

        onHealthChanged?.Invoke(next, maxHealth);
    }

    private void HandleDeathSynced(bool previous, bool next, bool asServer)
    {
        if (IsDuplicateHostCallback(asServer) || !next) return;

        onDied?.Invoke();
        Debug.Log($"{name} died");
    }

    public void TakeDamage(Damage damage)
    {
        TakeDamage(damage, transform.position);
    }

    public void TakeDamage(Damage damage, Vector3 hitPoint)
    {
        TakeDamage(damage, hitPoint, 0.0f);
    }

    public void TakeDamage(Damage damage, Vector3 hitPoint, float knockback, Transform attacker = null)
    {
        if (!IsServerInitialized)
        {
            Debug.LogWarning($"Health.TakeDamage on a client for {name}. Damage is decided by the server; the call did nothing.", this);
            return;
        }

        if (IsDead || damage.IsNothing || invulnerable.Value) return;

        if (damageAbsorber != null)
        {
            damage = damageAbsorber.Absorb(damage, hitPoint);
            if (damage.IsNothing) return;
        }

        float amount = damage.Total;
        currentHealth.Value = Mathf.Max(0.0f, currentHealth.Value - amount);
        Debug.Log($"{name} took {amount} damage ({currentHealth.Value}/{maxHealth} HP)");

        ReportHit(damage, hitPoint, knockback, attacker);
        ObserveHit(damage, hitPoint, knockback);

        if (currentHealth.Value <= 0.0f) dead.Value = true;
    }

    public void Heal(float amount)
    {
        if (!IsServerInitialized)
        {
            Debug.LogWarning($"Health.Heal on a client for {name}. Healing is decided by the server; the call did nothing.", this);
            return;
        }

        if (IsDead || amount <= 0.0f) return;

        currentHealth.Value = Mathf.Min(maxHealth, currentHealth.Value + amount);
        Debug.Log($"{name} healed {amount} ({currentHealth.Value}/{maxHealth} HP)");
    }

    [ObserversRpc]
    private void ObserveHit(Damage damage, Vector3 hitPoint, float knockback)
    {
        if (IsServerInitialized) return;

        ReportHit(damage, hitPoint, knockback, null);
    }

    private void ReportHit(Damage damage, Vector3 hitPoint, float knockback, Transform attacker)
    {
        LastHitKnockback = knockback;
        LastHitAttacker = attacker;
        onDamaged?.Invoke(damage, hitPoint);

        if (IsClientInitialized) BloodEffect.Spawn(hitPoint, gameObject.layer);
    }
}
