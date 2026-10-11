using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Serializing;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    [System.Serializable]
    public class Settings
    {
        public Vector3 size = new Vector3(0.2f, 0.2f, 0.7f);
        public float verticalHitTolerance = 0.0f;
        public Color color = new Color(1.0f, 0.93f, 0.7f, 0.9f);
        public float gravity = 20.0f;
        public float maximumLifetime = 6.0f;
        public float blastEffectDuration = 0.35f;

        public float trailDuration = 0.0f;
        public float trailWidth = 0.1f;
        public Color trailColor = new Color(1.0f, 0.93f, 0.7f, 0.5f);
    }

    public struct Look
    {
        public Vector3 Size;
        public Color Color;
        public float TrailDuration;
        public float TrailWidth;
        public Color TrailColor;
        public float BlastRadius;
        public float BlastEffectDuration;
    }

    private const string PrefabResourcePath = "Projectile";
    private const string EffectMaterialPath = "VisionEffect";

    private static GameObject prefab;
    private static Mesh sphereMesh;
    private static Shader effectShader;
    private static Material blastMaterial;
    private static Material trailMaterial;
    private static readonly Dictionary<Color, Material> bodyMaterialsByColor = new Dictionary<Color, Material>();

    private BallisticCurve flightPath;
    private Look look;
    private uint launchTick;
    private NetworkObject shooter;

    private Transform owner;
    private LayerMask hittableLayers;
    private Damage damage;
    private float knockbackDistance;
    private Hitbox hitbox;
    private int pierceRemaining;
    private HitEffect[] onHitEffects;
    private float remainingLifetime;
    private bool hasBlasted;
    private readonly HashSet<Hurtbox> struckByBlast = new HashSet<Hurtbox>();

    private TrailRenderer trail;

    private float simulatedFlightTime;
    private float drawnFlightTime;
    private Vector3 sweptTo;

    private float flightEndsAt = -1.0f;
    private bool ended;
    private float graceRemaining;

    private const float EndGraceSeconds = 0.5f;
    private Vector3 velocity;
    private bool endedHere;

    public static Projectile Spawn(in BallisticCurve curve, Transform owner,
                                   LayerMask hittableLayers, Damage damage, Settings settings)
    {
        return Spawn(curve, owner, hittableLayers, damage, 0.0f, settings);
    }

    public static Projectile Spawn(in BallisticCurve curve, Transform owner,
                                   LayerMask hittableLayers, Damage damage, float blastRadius,
                                   Settings settings, IReadOnlyList<HitEffect> onHitEffects = null,
                                   int pierceCount = 0, float knockbackDistance = 0.0f)
    {
        NetworkManager networkManager = InstanceFinder.NetworkManager;
        if (networkManager == null || !networkManager.IsServerStarted)
        {
            Debug.LogWarning("Projectile: only the server looses shots; this one was dropped.");
            return null;
        }

        GameObject shotPrefab = LoadPrefab();
        if (shotPrefab == null) return null;

        GameObject shotObject = Instantiate(shotPrefab, curve.LaunchPosition, Quaternion.identity);
        Projectile projectile = shotObject.GetComponent<Projectile>();

        projectile.owner = owner;
        projectile.hittableLayers = hittableLayers;
        projectile.damage = damage;
        projectile.knockbackDistance = knockbackDistance;
        projectile.pierceRemaining = pierceCount;
        projectile.remainingLifetime = settings.maximumLifetime;

        if (onHitEffects != null && onHitEffects.Count > 0)
        {
            projectile.onHitEffects = new HitEffect[onHitEffects.Count];
            for (int index = 0; index < onHitEffects.Count; index++)
            {
                projectile.onHitEffects[index] = onHitEffects[index];
            }
        }

        projectile.BuildHitbox(settings, blastRadius, onHitEffects);

        projectile.flightPath = curve;

        projectile.velocity = curve.LaunchVelocity;
        projectile.PointAlongFlightPath();

        projectile.shooter = owner != null ? owner.GetComponentInParent<NetworkObject>() : null;
        projectile.launchTick = networkManager.TimeManager.LocalTick;
        projectile.look = new Look
        {
            Size = settings.size,
            Color = settings.color,
            TrailDuration = settings.trailDuration,
            TrailWidth = settings.trailWidth,
            TrailColor = settings.trailColor,
            BlastRadius = blastRadius,
            BlastEffectDuration = settings.blastEffectDuration
        };

        projectile.Dress();

        networkManager.ServerManager.Spawn(shotObject);
        return projectile;
    }

    public bool FlightComesWithin(Vector3 point, float radius)
    {
        const float SecondsBetweenSamples = 0.05f;

        float radiusSqr = radius * radius;

        for (float seconds = simulatedFlightTime; seconds < simulatedFlightTime + remainingLifetime;
             seconds += SecondsBetweenSamples)
        {
            Vector3 along = flightPath.PositionAfterSeconds(seconds);
            float dx = along.x - point.x;
            float dz = along.z - point.z;

            if (dx * dx + dz * dz <= radiusSqr) return true;
        }

        return false;
    }

    private static GameObject LoadPrefab()
    {
        if (prefab != null) return prefab;

        prefab = Resources.Load<GameObject>(PrefabResourcePath);
        if (prefab == null)
        {
            Debug.LogError($"Projectile: no prefab at Resources/{PrefabResourcePath}, so no shot could be loosed.");
        }

        return prefab;
    }

    public override void WritePayload(NetworkConnection connection, Writer writer)
    {
        writer.WriteBallisticCurve(flightPath);
        writer.WriteLook(look);
        writer.WriteUInt32(launchTick);
        writer.WriteNetworkObject(shooter);
    }

    public override void ReadPayload(NetworkConnection connection, Reader reader)
    {
        flightPath = reader.ReadBallisticCurve();
        look = reader.ReadLook();
        launchTick = reader.ReadUInt32();
        shooter = reader.ReadNetworkObject();
    }

    public override void OnStartClient()
    {
        if (IsServerInitialized) return;

        drawnFlightTime = 0.0f;
        transform.position = flightPath.LaunchPosition;
        velocity = flightPath.LaunchVelocity;
        PointAlongFlightPath();

        if (shooter != null && shooter.IsOwner)
        {
            MeshRenderer body = GetComponent<MeshRenderer>();
            if (body != null) body.enabled = false;
            return;
        }

        Dress();
    }

    private void BuildHitbox(Settings settings, float blastRadius, IReadOnlyList<HitEffect> effects)
    {
        Vector3 sweptSize = new Vector3(settings.size.x,
                                        settings.size.y + settings.verticalHitTolerance * 2.0f,
                                        settings.size.z);

        hitbox = new Hitbox(owner, sweptSize, hittableLayers,
                            blastRadius > 0.0f ? default : damage, knockbackDistance, effects);
        hitbox.Struck += HandleStruck;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        TimeManager.OnTick += StepFlight;

        sweptTo = flightPath.LaunchPosition;
        hitbox.SweepTo(sweptTo, PointedAlong(flightPath.LaunchVelocity));
    }

    public override void OnStopServer()
    {
        base.OnStopServer();

        if (TimeManager != null) TimeManager.OnTick -= StepFlight;
    }

    private void StepFlight()
    {
        FlyAndResolve((float)TimeManager.TickDelta);
    }

    private void Update()
    {
        DrawFlight();
    }

    private void FlyAndResolve(float seconds)
    {
        if (ended)
        {
            graceRemaining -= seconds;
            if (graceRemaining <= 0.0f) DespawnNow();
            return;
        }

        remainingLifetime -= seconds;
        if (remainingLifetime <= 0.0f)
        {
            EndFlight();
            return;
        }

        Vector3 startedAt = flightPath.PositionAfterSeconds(simulatedFlightTime);
        simulatedFlightTime += seconds;
        Vector3 nextPosition = flightPath.PositionAfterSeconds(simulatedFlightTime);

        Vector3 step = nextPosition - startedAt;
        float stepDistance = step.magnitude;
        if (stepDistance <= 0.0f) return;

        sweptTo = nextPosition;
        hitbox.SweepTo(nextPosition,
                       PointedAlong(flightPath.VelocityAfterSeconds(simulatedFlightTime)));
        if (ended) return;

        if (TryFindWorldImpact(startedAt, step / stepDistance, stepDistance, out Vector3 impactPoint))
        {
            ApplyBlast(impactPoint);
            EndFlight();
        }
    }

    private void HandleStruck(Hurtbox target, Vector3 contactPoint)
    {
        if (look.BlastRadius > 0.0f)
        {
            ApplyBlast(sweptTo);
            EndFlight();
            return;
        }

        if (pierceRemaining > 0)
        {
            pierceRemaining--;
            return;
        }

        EndFlight();
    }

    private void DrawFlight()
    {
        if (ended) return;

        drawnFlightTime += Time.deltaTime;

        bool reachedEnd = flightEndsAt >= 0.0f && drawnFlightTime >= flightEndsAt;
        if (reachedEnd) drawnFlightTime = flightEndsAt;

        transform.position = flightPath.PositionAfterSeconds(drawnFlightTime);
        velocity = flightPath.VelocityAfterSeconds(drawnFlightTime);
        PointAlongFlightPath();

        if (reachedEnd) EndHere();
    }

    private void EndHere()
    {
        ended = true;

        HideBody();
        OrphanTrail();

        if (look.BlastRadius > 0.0f) SpawnBlastParticles(transform.position);
    }

    private void Dress()
    {
        Look drawn = look;

        transform.localScale = drawn.Size;

        MeshRenderer body = GetComponent<MeshRenderer>();
        if (body != null)
        {
            body.sharedMaterial = GetBodyMaterial(drawn.Color);
            body.enabled = true;
        }

        trail = AttachTrail(transform, drawn.TrailDuration, drawn.TrailWidth, drawn.TrailColor);
    }

    [ObserversRpc]
    private void ObserveEnded(float endElapsed)
    {
        endedHere = true;
        flightEndsAt = endElapsed;
    }

    public override void OnStopClient()
    {
        if (IsServerInitialized) return;

        if (ended) return;

        OrphanTrail();

        if (endedHere && look.BlastRadius > 0.0f) SpawnBlastParticles(transform.position);
    }

    public static TrailRenderer AttachTrail(Transform shot, float duration, float width, Color color)
    {
        if (duration <= 0.0f) return null;

        GameObject trailObject = new GameObject("Trail");
        trailObject.layer = shot.gameObject.layer;
        trailObject.transform.SetParent(shot, false);

        TrailRenderer trail = trailObject.AddComponent<TrailRenderer>();
        trail.time = duration;
        trail.autodestruct = false;
        trail.sharedMaterial = GetTrailMaterial();
        trail.alignment = LineAlignment.View;
        trail.numCapVertices = 2;
        trail.numCornerVertices = 2;
        trail.minVertexDistance = 0.05f;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;

        trail.widthCurve = AnimationCurve.Linear(0.0f, width, 1.0f, 0.0f);

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(color, 0.0f), new GradientColorKey(color, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(color.a, 0.0f), new GradientAlphaKey(0.0f, 1.0f) });
        trail.colorGradient = gradient;

        return trail;
    }

    private void EndFlight()
    {
        if (ended) return;

        ended = true;
        graceRemaining = EndGraceSeconds;

        ObserveEnded(simulatedFlightTime);
        OrphanTrail();
        HideBody();
    }

    private void DespawnNow()
    {
        NetworkManager networkManager = InstanceFinder.NetworkManager;
        if (networkManager != null) networkManager.ServerManager.Despawn(gameObject);
        else Destroy(gameObject);
    }

    private void HideBody()
    {
        MeshRenderer body = GetComponent<MeshRenderer>();
        if (body != null) body.enabled = false;
    }

    private void OrphanTrail()
    {
        if (trail == null) return;

        trail.transform.SetParent(null, true);
        trail.emitting = false;
        trail.gameObject.AddComponent<AutoDestroy>().lifetime = trail.time;

        trail = null;
    }

    private void PointAlongFlightPath()
    {
        if (velocity.sqrMagnitude <= 0.0001f) return;

        transform.rotation = PointedAlong(velocity);
    }

    private static Quaternion PointedAlong(Vector3 heading)
    {
        if (heading.sqrMagnitude <= 0.0001f) return Quaternion.identity;

        return Quaternion.LookRotation(heading, Vector3.up);
    }

    private bool TryFindWorldImpact(Vector3 startedAt, Vector3 direction, float stepDistance,
                                    out Vector3 impactPoint)
    {
        impactPoint = Vector3.zero;

        if (!Physics.Raycast(startedAt, direction, out RaycastHit hit, stepDistance,
                hittableLayers, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        if (owner != null && hit.collider.transform.IsChildOf(owner)) return false;
        if (Hurtbox.FindByCollider(hit.collider) != null) return false;

        impactPoint = hit.point;
        return true;
    }

    private void ApplyOnHitEffects(Health target, Vector3 hitPoint)
    {
        if (onHitEffects == null) return;

        foreach (HitEffect effect in onHitEffects)
        {
            if (effect != null) effect.OnHit(target, hitPoint, owner);
        }
    }

    private void ApplyBlast(Vector3 centre)
    {
        float blastRadius = look.BlastRadius;
        if (blastRadius <= 0.0f || hasBlasted) return;
        hasBlasted = true;

        SpawnBlastParticles(centre);

        Collider[] caught = Physics.OverlapSphere(centre, blastRadius, hittableLayers, QueryTriggerInteraction.Collide);
        struckByBlast.Clear();

        foreach (Collider collider in caught)
        {
            if (owner != null && collider.transform.IsChildOf(owner)) continue;

            Hurtbox target = Hurtbox.FindByCollider(collider);
            if (target == null || !struckByBlast.Add(target)) continue;

            Vector3 hitPoint = collider.ClosestPoint(centre);
            target.Health.TakeDamage(damage, hitPoint, knockbackDistance, owner);
            ApplyOnHitEffects(target.Health, hitPoint);
        }
    }

    private void SpawnBlastParticles(Vector3 centre)
    {
        Look drawn = look;

        GameObject effectObject = new GameObject("BlastEffect");
        effectObject.transform.position = centre;

        ParticleSystem particles = effectObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.duration = drawn.BlastEffectDuration;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = drawn.BlastEffectDuration;
        main.startSpeed = 0.0f;
        main.startSize = drawn.BlastRadius * 2.0f;
        main.startColor = new Color(drawn.Color.r, drawn.Color.g, drawn.Color.b, 0.35f);
        main.gravityModifier = 0.0f;
        main.maxParticles = 1;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0.0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 1) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = false;

        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1.0f, AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f));

        ParticleSystem.ColorOverLifetimeModule fade = particles.colorOverLifetime;
        fade.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) });
        fade.color = gradient;

        ParticleSystemRenderer particleRenderer = effectObject.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Mesh;
        particleRenderer.mesh = GetSphereMesh();
        particleRenderer.material = GetBlastMaterial();
        particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        particleRenderer.receiveShadows = false;

        particles.Play();

        effectObject.AddComponent<AutoDestroy>().lifetime = drawn.BlastEffectDuration * 2.0f;
    }

    private static Material GetBodyMaterial(Color color)
    {
        if (!bodyMaterialsByColor.TryGetValue(color, out Material material) || material == null)
        {
            material = new Material(EffectShader()) { color = color };
            bodyMaterialsByColor[color] = material;
        }

        return material;
    }

    private static Shader EffectShader()
    {
        if (effectShader != null) return effectShader;

        Material template = Resources.Load<Material>(EffectMaterialPath);
        effectShader = template != null ? template.shader : null;

        if (effectShader == null)
        {
            Debug.LogWarning($"Projectile: no material at Resources/{EffectMaterialPath}, so shots are drawn " +
                             "without the vision cutoff.");
            effectShader = Shader.Find("Sprites/Default");
        }

        return effectShader;
    }

    private static Mesh GetSphereMesh()
    {
        if (sphereMesh == null)
        {
            GameObject temporarySphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphereMesh = temporarySphere.GetComponent<MeshFilter>().sharedMesh;
            Destroy(temporarySphere);
        }

        return sphereMesh;
    }

    private static Material GetBlastMaterial()
    {
        if (blastMaterial == null)
        {
            blastMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        return blastMaterial;
    }

    private static Material GetTrailMaterial()
    {
        if (trailMaterial == null)
        {
            trailMaterial = new Material(EffectShader());
            trailMaterial.SetFloat("_UseVertexColor", 1.0f);
        }

        return trailMaterial;
    }
}

public static class ProjectileSerializer
{
    public static void WriteBallisticCurve(this FishNet.Serializing.Writer writer, BallisticCurve value)
    {
        writer.WriteVector3(value.LaunchPosition);
        writer.WriteVector3(value.LaunchVelocity);
        writer.WriteSingle(value.Gravity);
    }

    public static BallisticCurve ReadBallisticCurve(this FishNet.Serializing.Reader reader)
    {
        Vector3 launchPosition = reader.ReadVector3();
        Vector3 launchVelocity = reader.ReadVector3();

        return new BallisticCurve(launchPosition, launchVelocity, reader.ReadSingle());
    }

    public static void WriteLook(this FishNet.Serializing.Writer writer, Projectile.Look value)
    {
        writer.WriteVector3(value.Size);
        writer.WriteColor(value.Color);
        writer.WriteSingle(value.TrailDuration);
        writer.WriteSingle(value.TrailWidth);
        writer.WriteColor(value.TrailColor);
        writer.WriteSingle(value.BlastRadius);
        writer.WriteSingle(value.BlastEffectDuration);
    }

    public static Projectile.Look ReadLook(this FishNet.Serializing.Reader reader)
    {
        return new Projectile.Look
        {
            Size = reader.ReadVector3(),
            Color = reader.ReadColor(),
            TrailDuration = reader.ReadSingle(),
            TrailWidth = reader.ReadSingle(),
            TrailColor = reader.ReadColor(),
            BlastRadius = reader.ReadSingle(),
            BlastEffectDuration = reader.ReadSingle()
        };
    }
}
