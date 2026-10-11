using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.Events;

public class ResourcePool : NetworkBehaviour, IDepletable
{
    [SerializeField] private float maxValue = 100.0f;
    [SerializeField] private float startValue = 100.0f;
    [SerializeField] private float regenPerSecond = 5.0f;

    public UnityEvent<float, float> onValueChanged = new UnityEvent<float, float>();

    private readonly SyncVar<float> currentValue = new SyncVar<float>();

    public float CurrentValue => currentValue.Value;
    public float MaxValue => maxValue;

    public bool CanAfford(float amount) => amount <= 0.0f || currentValue.Value >= amount;

    float IDepletable.Current => currentValue.Value;
    float IDepletable.Max => maxValue;
    UnityEvent<float, float> IDepletable.Changed => onValueChanged;

    private void Awake()
    {
        currentValue.OnChange += HandleValueSynced;
    }

    private void OnDestroy()
    {
        currentValue.OnChange -= HandleValueSynced;
    }

    public override void OnStartServer()
    {
        currentValue.Value = Mathf.Clamp(startValue, 0.0f, maxValue);
    }

    public override void OnStartClient()
    {
        onValueChanged?.Invoke(currentValue.Value, maxValue);
    }

    private void Update()
    {
        if (!IsServerInitialized || regenPerSecond <= 0.0f) return;

        Restore(regenPerSecond * Time.deltaTime);
    }

    public void Spend(float amount)
    {
        if (amount <= 0.0f) return;
        if (!ServerOwnsThis("spend")) return;

        SetValue(currentValue.Value - amount);
    }

    public void Restore(float amount)
    {
        if (amount <= 0.0f) return;
        if (!ServerOwnsThis("restore")) return;

        SetValue(currentValue.Value + amount);
    }

    public void SetCurrent(float value)
    {
        if (!ServerOwnsThis("set")) return;

        SetValue(value);
    }

    public void SetMax(float newMax, bool refill = false)
    {
        if (!ServerOwnsThis("set the maximum of")) return;

        maxValue = Mathf.Max(0.0f, newMax);
        SetValue(refill ? maxValue : currentValue.Value);
    }

    private void SetValue(float value)
    {
        currentValue.Value = Mathf.Clamp(value, 0.0f, maxValue);
    }

    private bool IsDuplicateHostCallback(bool asServer) => asServer && IsHostInitialized;

    private void HandleValueSynced(float previous, float next, bool asServer)
    {
        if (IsDuplicateHostCallback(asServer)) return;

        onValueChanged?.Invoke(next, maxValue);
    }

    private bool ServerOwnsThis(string what)
    {
        if (IsServerInitialized) return true;

        Debug.LogWarning($"ResourcePool: a client cannot {what} {name}'s pool; the call did nothing.", this);
        return false;
    }
}
