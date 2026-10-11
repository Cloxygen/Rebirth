using System.Collections.Generic;
using UnityEngine;

public static class BloodEffect
{
    private const int MaxDecalsPerHit = 10;

    private static Mesh sphereMesh;
    private static Material sphereMaterial;

    public static void Spawn(Vector3 position, int sourceLayer = -1)
    {
        GameObject effectObject = new GameObject("BloodEffect");
        effectObject.transform.position = position;

        ParticleSystem particleSystem = effectObject.AddComponent<ParticleSystem>();
        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particleSystem.main;
        main.duration = 1.0f;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
        main.startColor = new Color(0.55f, 0.0f, 0.0f);
        main.gravityModifier = 1.0f;
        main.maxParticles = 32;

        ParticleSystem.EmissionModule emission = particleSystem.emission;
        emission.rateOverTime = 0.0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 10, 16) });

        ParticleSystem.ShapeModule shape = particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        ParticleSystem.CollisionModule collision = particleSystem.collision;
        collision.enabled = true;
        collision.type = ParticleSystemCollisionType.World;
        collision.mode = ParticleSystemCollisionMode.Collision3D;
        collision.dampen = 0.5f;
        collision.bounce = 0.3f;
        collision.lifetimeLoss = 0.25f;
        collision.radiusScale = 1.0f;
        collision.sendCollisionMessages = true;

        if (sourceLayer >= 0)
        {
            collision.collidesWith = ~(1 << sourceLayer);
        }

        effectObject.AddComponent<BloodSplatterStamper>().Initialize(particleSystem, MaxDecalsPerHit);

        ParticleSystemRenderer particleRenderer = effectObject.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Mesh;
        particleRenderer.mesh = GetSphereMesh();
        particleRenderer.material = GetSphereMaterial();

        particleSystem.Play();

        Object.Destroy(effectObject, main.duration + main.startLifetime.constantMax + 0.1f);
    }

    private static Mesh GetSphereMesh()
    {
        if (sphereMesh == null)
        {
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphereMesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(temp);
        }

        return sphereMesh;
    }

    private static Material GetSphereMaterial()
    {
        if (sphereMaterial == null)
        {
            sphereMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                color = new Color(0.5f, 0.0f, 0.0f)
            };
        }

        return sphereMaterial;
    }
}

public class BloodSplatterStamper : MonoBehaviour
{
    private ParticleSystem sourceParticles;
    private List<ParticleCollisionEvent> collisionEvents;
    private int remainingDecals;

    public void Initialize(ParticleSystem particles, int maxDecals)
    {
        sourceParticles = particles;
        collisionEvents = new List<ParticleCollisionEvent>();
        remainingDecals = maxDecals;
    }

    private void OnParticleCollision(GameObject other)
    {
        if (remainingDecals <= 0) return;

        int eventCount = sourceParticles.GetCollisionEvents(other, collisionEvents);

        for (int i = 0; i < eventCount && remainingDecals > 0; i++)
        {
            ParticleCollisionEvent collisionEvent = collisionEvents[i];

            if (collisionEvent.velocity.magnitude < BloodDecal.MinimumImpactSpeed) continue;

            remainingDecals--;

            Transform surface = collisionEvent.colliderComponent != null
                ? collisionEvent.colliderComponent.transform
                : null;

            BloodDecal.Spawn(collisionEvent.intersection, collisionEvent.normal, collisionEvent.velocity, surface);
        }
    }
}
