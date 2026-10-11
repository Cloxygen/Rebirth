using UnityEngine;

[RequireComponent(typeof(Health))]
public class DamageNumberEmitter : MonoBehaviour
{
    [SerializeField] private DamageKind kind = DamageKind.Dealt;

    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        health.onDamaged.AddListener(HandleDamaged);
    }

    private void OnDisable()
    {
        health.onDamaged.RemoveListener(HandleDamaged);
    }

    private void HandleDamaged(Damage damage, Vector3 hitPoint)
    {
        DamageNumbers.Spawn(damage.Total, hitPoint, kind);
    }
}
