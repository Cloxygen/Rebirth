using FishNet.Connection;
using FishNet.Object;
using FishNet.Observing;
using UnityEngine;

[CreateAssetMenu(fileName = "VisionCondition", menuName = "Rebirth/Vision Condition")]
public class VisionCondition : ObserverCondition
{
    public override bool ConditionMet(NetworkConnection connection, bool currentlyAdded, out bool notProcessed)
    {
        notProcessed = false;

        Vector3 position = NetworkObject.transform.position;
        Projectile shot = NetworkObject.GetComponent<Projectile>();

        foreach (NetworkObject watcher in connection.Objects)
        {
            VisionField vision = watcher.GetComponent<VisionField>();
            if (vision == null) continue;

            bool visible = shot != null
                ? shot.FlightComesWithin(vision.transform.position, vision.SendRadius)
                : vision.CanBeSent(position, currentlyAdded);

            if (visible) return true;
        }

        return false;
    }

    public override ObserverConditionType GetConditionType() => ObserverConditionType.Normal;
}
