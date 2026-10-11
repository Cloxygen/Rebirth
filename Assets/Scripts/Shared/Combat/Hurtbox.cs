using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class Hurtbox : MonoBehaviour
{
    [SerializeField] private float padding;

    private static readonly Dictionary<Collider, Hurtbox> hurtboxesByVolume = new Dictionary<Collider, Hurtbox>();

    private readonly List<Collider> volumes = new List<Collider>();

    public Health Health { get; private set; }

    public static Hurtbox FindByCollider(Collider collider)
    {
        return collider != null && hurtboxesByVolume.TryGetValue(collider, out Hurtbox hurtbox) ? hurtbox : null;
    }

    private void Awake()
    {
        Health = GetComponent<Health>();

        RegisterStandingVolumes();

        if (TryGetComponent(out CharacterController controller)) Register(GrowTriggerAround(controller));
    }

    private void OnDestroy()
    {
        foreach (Collider volume in volumes) hurtboxesByVolume.Remove(volume);

        volumes.Clear();
    }

    private void RegisterStandingVolumes()
    {
        foreach (Collider volume in GetComponents<Collider>())
        {
            if (volume is CharacterController) continue;

            Register(volume);
        }
    }

    private void Register(Collider volume)
    {
        hurtboxesByVolume[volume] = this;
        volumes.Add(volume);
    }

    private Collider GrowTriggerAround(CharacterController controller)
    {
        CapsuleCollider volume = gameObject.AddComponent<CapsuleCollider>();
        volume.isTrigger = true;
        volume.center = controller.center;
        volume.height = controller.height + padding * 2.0f;
        volume.radius = controller.radius + padding;

        return volume;
    }
}
