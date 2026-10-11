using UnityEngine;

public class PredictedShot : MonoBehaviour
{
    private BallisticCurve curve;
    private Transform owner;
    private LayerMask stoppedBy;
    private Vector3 halfExtents;
    private float remainingLifetime;
    private float elapsedFlightTime;
    private Vector3 velocity;
    private TrailRenderer trail;

    public static void Spawn(in BallisticCurve curve, Transform owner, LayerMask stoppedBy,
                             Projectile.Settings settings)
    {
        GameObject shotObject = BoxMesh.CreateDetached("PredictedShot", settings.size, settings.color);
        shotObject.transform.position = curve.LaunchPosition;

        PredictedShot predicted = shotObject.AddComponent<PredictedShot>();
        predicted.curve = curve;
        predicted.owner = owner;
        predicted.stoppedBy = stoppedBy;
        predicted.remainingLifetime = settings.maximumLifetime;
        predicted.halfExtents = new Vector3(
            settings.size.x * 0.5f,
            settings.size.y * 0.5f + settings.verticalHitTolerance,
            settings.size.z * 0.5f);
        predicted.trail = Projectile.AttachTrail(shotObject.transform, settings.trailDuration,
                                             settings.trailWidth, settings.trailColor);

        predicted.velocity = curve.LaunchVelocity;
        shotObject.transform.rotation = Quaternion.LookRotation(predicted.velocity, Vector3.up);
    }

    private void Update()
    {
        remainingLifetime -= Time.deltaTime;
        if (remainingLifetime <= 0.0f)
        {
            Vanish();
            return;
        }

        elapsedFlightTime += Time.deltaTime;

        Vector3 nextPosition = curve.PositionAfterSeconds(elapsedFlightTime);
        velocity = curve.VelocityAfterSeconds(elapsedFlightTime);

        Vector3 step = nextPosition - transform.position;
        float stepDistance = step.magnitude;
        if (stepDistance <= 0.0f) return;

        if (StopsOnSomething(step / stepDistance, stepDistance, out Vector3 stopPoint))
        {
            transform.position = stopPoint;
            Vanish();
            return;
        }

        transform.position = nextPosition;
        if (velocity.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(velocity, Vector3.up);
        }
    }

    private bool StopsOnSomething(Vector3 direction, float stepDistance, out Vector3 stopPoint)
    {
        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, stepDistance,
                stoppedBy, QueryTriggerInteraction.Ignore)
            && !IsTheShooter(hit.collider))
        {
            stopPoint = hit.point;
            return true;
        }

        Vector3 centre = transform.position + direction * (stepDistance * 0.5f);
        Vector3 extents = new Vector3(halfExtents.x, halfExtents.y, halfExtents.z + stepDistance * 0.5f);

        Collider[] caught = Physics.OverlapBox(centre, extents, transform.rotation, stoppedBy,
                                               QueryTriggerInteraction.Collide);

        foreach (Collider collider in caught)
        {
            if (IsTheShooter(collider)) continue;
            if (Hurtbox.FindByCollider(collider) == null) continue;

            stopPoint = collider.ClosestPoint(transform.position);
            return true;
        }

        stopPoint = Vector3.zero;
        return false;
    }

    private bool IsTheShooter(Collider collider)
    {
        return owner != null && collider.transform.IsChildOf(owner);
    }

    private void Vanish()
    {
        if (trail != null)
        {
            trail.transform.SetParent(null, true);
            trail.emitting = false;
            trail.gameObject.AddComponent<AutoDestroy>().lifetime = trail.time;
        }

        Destroy(gameObject);
    }
}
