using UnityEngine;

public class LightFollow : MonoBehaviour
{
    [SerializeField] private Transform targetToFollow;
    [SerializeField] private Vector3 positionOffset = new Vector3(0.0f, 1.8f, 0.8f);
    [SerializeField] private LayerMask obstacleLayerMask;
    [SerializeField] private float lightSafetyRadius = 0.2f;

    private void LateUpdate()
    {
        if (targetToFollow == null) return;

        Vector3 desiredPosition = targetToFollow.position + positionOffset;
        Vector3 directionToTarget = desiredPosition - targetToFollow.position;
        float distanceToTarget = directionToTarget.magnitude;

        if (distanceToTarget > 0.01f)
        {
            Vector3 directionNormalized = directionToTarget / distanceToTarget;

            if (Physics.SphereCast(targetToFollow.position, lightSafetyRadius, directionNormalized, out RaycastHit wallHitInformation, distanceToTarget, obstacleLayerMask))
            {
                transform.position = targetToFollow.position + directionNormalized * Mathf.Max(0.1f, wallHitInformation.distance - lightSafetyRadius);
                return;
            }
        }

        transform.position = desiredPosition;
    }
}
