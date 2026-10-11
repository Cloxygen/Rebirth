using FishNet;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class LootDropper : MonoBehaviour
{
    [SerializeField] private LootTable table;

    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        health.onDied.AddListener(DropLoot);
    }

    private void OnDisable()
    {
        health.onDied.RemoveListener(DropLoot);
    }

    private void DropLoot()
    {
        if (InstanceFinder.NetworkManager == null || !InstanceFinder.NetworkManager.IsServerStarted) return;

        if (table == null)
        {
            Debug.LogWarning($"{name}: no loot table assigned, so nothing drops.", this);
            return;
        }

        table.Drop(transform.position, transform);
    }
}
