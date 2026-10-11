using UnityEngine;

public class FootstepEventReceiver : MonoBehaviour
{
    [SerializeField] private WalkingParticles walkingParticles;
    [SerializeField] private Transform leftFootBone;
    [SerializeField] private Transform rightFootBone;

    public void OnLeftFootstep()
    {
        if (walkingParticles != null && leftFootBone != null)
        {
            walkingParticles.SpawnFootstepDecal(leftFootBone.position);
        }
    }

    public void OnRightFootstep()
    {
        if (walkingParticles != null && rightFootBone != null)
        {
            walkingParticles.SpawnFootstepDecal(rightFootBone.position);
        }
    }
}
