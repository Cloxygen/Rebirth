using System.Collections;
using FishNet.Object;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))]
public class EnemyAI : NetworkBehaviour
{
    private enum State
    {
        Wander,
        Chase,
        Attack
    }

    [Header("Detection")]
    [SerializeField] private float detectionRadius = 8.0f;
    [SerializeField] private float loseSightRadius = 12.0f;
    [SerializeField] private float forgetAfterSeconds = 8.0f;
    [SerializeField] private float noticeThreat = 1.0f;
    [SerializeField] private float switchMargin = 0.1f;

    [Header("Wander")]
    [SerializeField] private float wanderRadius = 6.0f;
    [SerializeField] private float wanderInterval = 4.0f;
    [SerializeField] private float wanderSpeed = 1.5f;

    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 3.5f;

    [Header("Attack")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackRangeHysteresis = 0.5f;
    [SerializeField] private float attackCooldown = 1.65f;
    [SerializeField] private LayerMask hittableLayers = ~0;
    [SerializeField] private AttackData attack = new AttackData();

    [Header("Attack Hitbox")]
    [SerializeField] private Vector3 hitboxSize = new Vector3(1.2f, 1.5f, 1.6f);
    [SerializeField] private float hitboxForwardOffset = 1.0f;
    [SerializeField] private Color windupColor = new Color(1.0f, 0.45f, 0.15f, 0.2f);
    [SerializeField] private Color hitboxColor = new Color(1.0f, 0.25f, 0.1f, 0.35f);

    [System.Serializable]
    private class AttackData
    {
        public Damage damage = new Damage(DamageType.Blunt, 5.0f);
        public float knockbackDistance;
        public float swingArcDegrees = 100.0f;
        public float swingDirection = 1.0f;
        public float windupDuration = 0.45f;
        public float swingDuration = 0.2f;
        public float lungeDistance = 0.6f;
        public float recoveryDuration = 0.4f;
    }

    [Header("Knockback")]
    [Range(0.0f, 1.0f)]
    [SerializeField] private float knockbackResistance;
    [SerializeField] private float knockbackDuration = 0.2f;

    [Header("Damage Flash")]
    [SerializeField] private Color damageFlashColor = Color.white;
    [SerializeField] private float damageFlashDuration = 0.15f;

    [Header("Death")]
    [SerializeField] private float deathShrinkDuration = 0.6f;

    [Header("Footprints")]
    [SerializeField] private float footprintDistance = 0.35f;
    [SerializeField] private float footSpacing = 0.1f;
    [SerializeField] private FootprintEffect.Settings footprintSettings = new FootprintEffect.Settings { size = 0.15f, lifetime = 4.0f };

    private NavMeshAgent agent;
    private Health health;
    private readonly ThreatTable threat = new ThreatTable();
    private Transform target;

    private State currentState = State.Wander;
    private Vector3 wanderOrigin;
    private Vector3 lastFootprintPosition;
    private Vector3 lastObserverRebuildPosition;
    private float wanderTimer;
    private float attackCooldownRemaining;
    private float accumulatedFootprintDistance;
    private bool nextFootIsRight = true;

    private Renderer bodyRenderer;
    private Color baseColor;
    private Coroutine knockbackRoutine;
    private Coroutine flashRoutine;
    private MeleeSwing swing;
    private float recoveryRemaining;

    public bool IsAttacking => swing != null || recoveryRemaining > 0.0f;

    private const float StoppingDistanceFraction = 0.8f;

    private const float ObserverRebuildDistance = 1.0f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();

        agent.stoppingDistance = attackRange * StoppingDistanceFraction;

        wanderOrigin = transform.position;
        lastFootprintPosition = transform.position;

        bodyRenderer = GetComponentInChildren<Renderer>();
        if (bodyRenderer != null) baseColor = bodyRenderer.material.color;
    }

    private void OnEnable()
    {
        health.onDied.AddListener(HandleDeath);
        health.onDamaged.AddListener(HandleDamaged);
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        TimeManager.OnTick += AdvanceAttackOnTick;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();

        if (TimeManager != null) TimeManager.OnTick -= AdvanceAttackOnTick;
    }

    private void AdvanceAttackOnTick()
    {
        AdvanceAttack((float)TimeManager.TickDelta);
    }

    private void OnDisable()
    {
        health.onDied.RemoveListener(HandleDeath);
        health.onDamaged.RemoveListener(HandleDamaged);
    }

    private void Update()
    {
        UpdateFootprints();

        swing?.Draw(Time.deltaTime);

        if (!IsServerInitialized)
        {
            if (agent != null && agent.enabled) agent.enabled = false;
            return;
        }

        if (health.IsDead) return;

        UpdateThreat();
        UpdateMovementLock();
        UpdateState();
        RebuildObserversIfMoved();

        if (attackCooldownRemaining > 0.0f)
        {
            attackCooldownRemaining -= Time.deltaTime;
        }
    }

    private void UpdateThreat()
    {
        float now = Time.time;
        threat.Update(transform.position, loseSightRadius, forgetAfterSeconds, now);

        foreach (GameObject playerObject in GameObject.FindGameObjectsWithTag("Player"))
        {
            Transform player = playerObject.transform;
            if (threat.Contains(player) || ThreatTable.IsDead(player)) continue;
            if (HorizontalDistanceTo(player.position) > detectionRadius) continue;

            threat.Add(player, noticeThreat, now);
        }

        target = threat.PickTarget(switchMargin);
    }

    public void AddThreat(Transform who, float amount)
    {
        if (who == null) who = NearestPlayer();
        threat.Add(who, amount, Time.time);
    }

    private Transform NearestPlayer()
    {
        Transform nearest = null;
        float nearestDistance = Mathf.Infinity;

        foreach (GameObject playerObject in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (ThreatTable.IsDead(playerObject.transform)) continue;

            float distance = HorizontalDistanceTo(playerObject.transform.position);
            if (distance >= nearestDistance) continue;

            nearestDistance = distance;
            nearest = playerObject.transform;
        }

        return nearest;
    }

    private void UpdateMovementLock()
    {
        agent.isStopped = IsAttacking || knockbackRoutine != null;
    }

    private void HandleDamaged(Damage damage, Vector3 hitPoint)
    {
        if (IsServerInitialized)
        {
            Vector3 direction = transform.position - hitPoint;
            direction.y = 0.0f;
            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : -transform.forward;

            AddThreat(health.LastHitAttacker, Mathf.Max(damage.Total, noticeThreat));

            float shove = health.LastHitKnockback * (1.0f - knockbackResistance);
            if (shove > 0.0f && knockbackDuration > 0.0f)
            {
                if (knockbackRoutine != null) StopCoroutine(knockbackRoutine);
                knockbackRoutine = StartCoroutine(PlayKnockback(direction, shove));
            }
        }

        if (bodyRenderer != null)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(PlayDamageFlash());
        }
    }

    private IEnumerator PlayKnockback(Vector3 direction, float distance)
    {
        float elapsed = 0.0f;
        float initialSpeed = 2.0f * distance / knockbackDuration;

        while (elapsed < knockbackDuration)
        {
            elapsed += Time.deltaTime;
            float speed = initialSpeed * (1.0f - elapsed / knockbackDuration);
            agent.Move(direction * speed * Time.deltaTime);
            yield return null;
        }

        knockbackRoutine = null;
    }

    private IEnumerator PlayDamageFlash()
    {
        bodyRenderer.material.color = damageFlashColor;
        float elapsed = 0.0f;

        while (elapsed < damageFlashDuration)
        {
            elapsed += Time.deltaTime;
            bodyRenderer.material.color = Color.Lerp(damageFlashColor, baseColor,
                                                      elapsed / damageFlashDuration);
            yield return null;
        }

        bodyRenderer.material.color = baseColor;
        flashRoutine = null;
    }

    private void RebuildObserversIfMoved()
    {
        if (!IsServerInitialized) return;
        if ((transform.position - lastObserverRebuildPosition).sqrMagnitude < ObserverRebuildDistance * ObserverRebuildDistance) return;

        lastObserverRebuildPosition = transform.position;
        ServerManager.Objects.RebuildObservers(NetworkObject);
    }

    private void UpdateFootprints()
    {
        float movedThisFrame = Vector3.Distance(transform.position, lastFootprintPosition);
        lastFootprintPosition = transform.position;
        accumulatedFootprintDistance += movedThisFrame;

        if (accumulatedFootprintDistance >= footprintDistance)
        {
            float sidewaysOffset = nextFootIsRight ? footSpacing : -footSpacing;
            FootprintEffect.Spawn(transform.position, transform.rotation, sidewaysOffset, transform, footprintSettings);
            nextFootIsRight = !nextFootIsRight;
            accumulatedFootprintDistance = 0.0f;
        }
    }

    private void UpdateState()
    {
        if (IsAttacking) return;

        float distanceToPlayer = target != null
            ? HorizontalDistanceTo(target.position)
            : Mathf.Infinity;

        switch (currentState)
        {
            case State.Wander:
                if (target != null)
                {
                    currentState = State.Chase;
                    break;
                }
                DoWander();
                break;

            case State.Chase:
                if (target == null)
                {
                    currentState = State.Wander;
                    wanderOrigin = transform.position;
                    break;
                }
                if (distanceToPlayer <= attackRange)
                {
                    currentState = State.Attack;
                    break;
                }
                DoChase();
                break;

            case State.Attack:
                if (distanceToPlayer > attackRange + attackRangeHysteresis)
                {
                    currentState = State.Chase;
                    break;
                }
                DoAttack();
                break;
        }
    }

    private void DoWander()
    {
        agent.speed = wanderSpeed;
        wanderTimer -= Time.deltaTime;

        bool needsNewDestination = wanderTimer <= 0.0f ||
            (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance);

        if (needsNewDestination)
        {
            Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
            Vector3 candidatePoint = wanderOrigin + new Vector3(randomCircle.x, 0.0f, randomCircle.y);

            if (NavMesh.SamplePosition(candidatePoint, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }

            wanderTimer = wanderInterval;
        }
    }

    private void DoChase()
    {
        agent.speed = chaseSpeed;
        agent.SetDestination(target.position);
    }

    private void DoAttack()
    {
        agent.SetDestination(target.position);
        FaceTarget(target.position);

        if (attackCooldownRemaining <= 0.0f)
        {
            attackCooldownRemaining = attackCooldown;
            BeginAttack();
            ObserveAttack();
        }
    }

    [ObserversRpc]
    private void ObserveAttack()
    {
        if (IsServerInitialized) return;

        BeginAttack();
    }

    private void BeginAttack()
    {
        CancelAttack();

        MeleeSwing.Arc arc = new MeleeSwing.Arc(hitboxSize, hitboxForwardOffset, attack.swingArcDegrees,
                                                attack.swingDirection, windupColor, hitboxColor);

        swing = new MeleeSwing(transform, BuildHitbox(), arc, attack.windupDuration, attack.swingDuration);
    }

    private Hitbox BuildHitbox()
    {
        if (!IsServerInitialized) return null;

        return new Hitbox(transform, hitboxSize, hittableLayers, attack.damage,
                          attack.knockbackDistance, null);
    }

    private void AdvanceAttack(float seconds)
    {
        if (swing != null)
        {
            swing.Advance(seconds);

            if (swing.BladeIsOut) Lunge(seconds);

            if (swing.IsFinished)
            {
                swing = null;
                recoveryRemaining = attack.recoveryDuration;
            }

            return;
        }

        if (recoveryRemaining > 0.0f) recoveryRemaining -= seconds;
    }

    private void Lunge(float seconds)
    {
        if (!IsServerInitialized) return;
        if (attack.lungeDistance == 0.0f || attack.swingDuration <= 0.0f) return;

        float frontLoaded = 2.0f * (1.0f - swing.BladeProgress);
        agent.Move(transform.forward * (attack.lungeDistance * frontLoaded * (seconds / attack.swingDuration)));
    }

    private void CancelAttack()
    {
        swing?.Stop();
        swing = null;
        recoveryRemaining = 0.0f;
    }

    private float HorizontalDistanceTo(Vector3 position)
    {
        Vector3 offset = position - transform.position;
        offset.y = 0.0f;

        return offset.magnitude;
    }

    private void FaceTarget(Vector3 targetPosition)
    {
        Vector3 lookDirection = targetPosition - transform.position;
        lookDirection.y = 0.0f;

        if (lookDirection.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(lookDirection, Vector3.up),
                10.0f * Time.deltaTime);
        }
    }

    private IEnumerator PlayDeathShrink()
    {
        float elapsed = 0.0f;
        Vector3 startScale = transform.localScale;

        while (elapsed < deathShrinkDuration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / deathShrinkDuration);
            yield return null;
        }

        transform.localScale = Vector3.zero;
    }

    private IEnumerator DespawnAfterShrink()
    {
        yield return new WaitForSeconds(deathShrinkDuration);

        ServerManager.Despawn(gameObject);
    }

    private void HandleDeath()
    {
        CancelAttack();

        if (IsServerInitialized)
        {
            agent.isStopped = true;
            StartCoroutine(DespawnAfterShrink());
        }

        enabled = false;
        StartCoroutine(PlayDeathShrink());
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.matrix = Matrix4x4.TRS(
            transform.position + transform.forward * hitboxForwardOffset, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, hitboxSize);
    }
}
