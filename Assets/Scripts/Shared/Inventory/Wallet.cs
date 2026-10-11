using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.Events;

public class Wallet : NetworkBehaviour
{
    public UnityEvent<int> onEssenceChanged;

    [SerializeField] private Health health;

    private static readonly List<Wallet> All = new List<Wallet>();

    private readonly SyncVar<int> essence = new SyncVar<int>();

    public int Essence => essence.Value;

    public bool CanTakeEssence => health == null || !health.IsDead;

    private void Awake()
    {
        essence.OnChange += HandleEssenceSynced;
    }

    private void OnDestroy()
    {
        essence.OnChange -= HandleEssenceSynced;
    }

    private void OnEnable()
    {
        All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    public override void OnStartClient()
    {
        onEssenceChanged?.Invoke(essence.Value);
    }

    public static Wallet Nearest(Vector3 point, float maxDistance)
    {
        Wallet best = null;
        float bestSqr = maxDistance * maxDistance;

        foreach (Wallet wallet in All)
        {
            if (!wallet.CanTakeEssence) continue;

            float sqr = (wallet.transform.position - point).sqrMagnitude;
            if (sqr > bestSqr) continue;

            bestSqr = sqr;
            best = wallet;
        }

        return best;
    }

    public void SetEssence(int amount)
    {
        if (!IsServerInitialized)
        {
            Debug.LogWarning($"Wallet.SetEssence on a client for {name}. The total is the server's; the call did nothing.", this);
            return;
        }

        essence.Value = Mathf.Max(0, amount);
    }

    public void AddEssence(int amount)
    {
        if (amount <= 0) return;

        if (!IsServerInitialized)
        {
            Debug.LogWarning($"Wallet.AddEssence on a client for {name}. The total is the server's; the call did nothing.", this);
            return;
        }

        essence.Value += amount;
        Debug.Log($"{name} picked up {amount} essence ({essence.Value} total)");
    }

    private void HandleEssenceSynced(int previous, int next, bool asServer)
    {
        if (asServer && IsHostInitialized) return;

        onEssenceChanged?.Invoke(next);
    }
}
