using UnityEngine;

public class WalkingParticles : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private ParticleSystem particleSystemToEmit;
    [SerializeField] private float distanceBetweenPuffs = 0.5f;
    [SerializeField] private int particlesPerPuff = 5;
    [SerializeField] private int particlesPerLandingPuff = 15;
    [SerializeField] private bool spawnFootprintDecals = true;
    [SerializeField] private FootprintEffect.Settings footprintSettings = new FootprintEffect.Settings();

    private CharacterController characterController;
    private Vector3 lastPosition;
    private float accumulatedDistance;
    private bool wasGroundedLastFrame = true;

    private void Start()
    {
        InitializeComponents();
    }

    private void Update()
    {
        UpdateMovementParticles();
    }

    private void InitializeComponents()
    {
        lastPosition = transform.position;
        characterController = GetComponent<CharacterController>();

        if (particleSystemToEmit == null)
        {
            particleSystemToEmit = GetComponentInChildren<ParticleSystem>();
        }
    }

    private void UpdateMovementParticles()
    {
        if (particleSystemToEmit == null) return;

        bool isGrounded = characterController == null || characterController.isGrounded;
        float movedThisFrame = Vector3.Distance(transform.position, lastPosition);
        lastPosition = transform.position;

        if (isGrounded && !wasGroundedLastFrame)
        {
            particleSystemToEmit.Emit(particlesPerLandingPuff);
        }

        wasGroundedLastFrame = isGrounded;

        if (!isGrounded)
        {
            accumulatedDistance = 0.0f;
            return;
        }

        accumulatedDistance += movedThisFrame;

        if (accumulatedDistance >= distanceBetweenPuffs)
        {
            particleSystemToEmit.Emit(particlesPerPuff);
            accumulatedDistance = 0.0f;
        }
    }

    public void SpawnFootstepDecal(Vector3 footWorldPosition)
    {
        if (!spawnFootprintDecals) return;

        bool isGrounded = characterController == null || characterController.isGrounded;
        if (!isGrounded) return;

        FootprintEffect.Spawn(footWorldPosition, transform.rotation, 0.0f, transform, footprintSettings);
    }
}
