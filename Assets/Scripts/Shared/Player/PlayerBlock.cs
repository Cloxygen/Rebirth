using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Health))]
public class PlayerBlock : NetworkBehaviour, IDamageAbsorber
{
    [Header("Input Actions")]
    [SerializeField] private InputAction blockAction;

    [Header("Guard Settings")]
    [SerializeField] private float raiseDuration = 0.15f;
    [SerializeField] private float guardArcDegrees = 140.0f;
    [Range(0.0f, 1.0f)]
    [SerializeField] private float damageReduction = 0.75f;
    [Range(0.0f, 1.0f)]
    [SerializeField] private float movementSpeedMultiplier = 0.45f;

    [Header("Guard Plate")]
    [SerializeField] private Vector3 plateSize = new Vector3(1.3f, 1.3f, 0.1f);
    [SerializeField] private float plateForwardOffset = 0.7f;
    [SerializeField] private float plateHeight = 0.1f;
    [SerializeField] private Color plateColor = new Color(0.45f, 0.7f, 1.0f, 0.22f);
    [SerializeField] private Color plateImpactColor = new Color(1.0f, 0.94f, 0.65f, 0.75f);
    [SerializeField] private float impactFlashDuration = 0.18f;

    private Health health;
    private PlayerMovement playerMovement;
    private ActionBar actionBar;
    private Aim playerAim;

    private Transform plateTransform;
    private MeshRenderer plateRenderer;
    private Material plateMaterial;

    private float raiseProgress;
    private float impactFlashRemaining;
    private bool guardHeld;

    public bool Guarding => raiseProgress > 0.0f;
    public bool IsGuardUp => raiseProgress >= 1.0f;

    public float MovementSpeedMultiplier => movementSpeedMultiplier;

    private void Awake()
    {
        health = GetComponent<Health>();
        playerMovement = GetComponent<PlayerMovement>();
        actionBar = GetComponent<ActionBar>();
        playerAim = GetComponent<Aim>();

        EnsureDefaultBlockBindingsExist();
        CreateGuardPlate();
    }

    private void EnsureDefaultBlockBindingsExist()
    {
        if (blockAction.bindings.Count == 0)
        {
            blockAction.AddBinding("<Mouse>/rightButton");
            blockAction.AddBinding("<Gamepad>/leftShoulder");
        }
    }

    private void OnEnable()
    {
        blockAction.Enable();
    }

    private void OnDisable()
    {
        blockAction.Disable();
        raiseProgress = 0.0f;
        UpdateGuardPlate();
    }

    private void Update()
    {
        if (!IsOwner) return;

        UpdateGuardPlate();

        if (impactFlashRemaining > 0.0f)
        {
            impactFlashRemaining -= Time.deltaTime;
        }
    }

    public bool TakeGuardInput() => WantsToGuard();

    public void ReceiveGuardInput(bool held)
    {
        guardHeld = held;
    }

    public void AdvanceGuard(float delta)
    {
        raiseProgress = AdvanceRaise(raiseProgress, CanRaiseGuard(), delta);
        AssertMovement();
    }

    private bool CanRaiseGuard()
    {
        bool attacking = actionBar != null && actionBar.IsAttacking;
        return guardHeld && !health.IsDead && !attacking;
    }

    private void AssertMovement()
    {
        if (playerMovement == null || !Guarding) return;

        playerMovement.Slow(movementSpeedMultiplier);

        playerMovement.FaceAimPoint();
    }

    private float AdvanceRaise(float progress, bool wantsToGuard, float delta)
    {
        if (wantsToGuard)
        {
            return raiseDuration > 0.0f
                ? Mathf.Min(1.0f, progress + delta / raiseDuration)
                : 1.0f;
        }

        float lowerRate = raiseDuration > 0.0f ? delta / (raiseDuration * 0.5f) : 1.0f;
        return Mathf.Max(0.0f, progress - lowerRate);
    }

    private bool WantsToGuard()
    {
        if (health.IsDead) return false;
        if (actionBar != null && actionBar.IsAttacking) return false;
        if (!blockAction.IsPressed()) return false;

        return Guarding || !UIPointer.IsOverBlockingUI();
    }

    public Damage Absorb(Damage damage, Vector3 hitPoint)
    {
        if (!IsGuardUp || !IsHitInsideGuardArc(hitPoint)) return damage;

        FlashGuard(Owner);
        return damage.Scaled(1.0f - damageReduction);
    }

    [TargetRpc]
    private void FlashGuard(NetworkConnection connection)
    {
        impactFlashRemaining = impactFlashDuration;
    }

    private bool IsHitInsideGuardArc(Vector3 hitPoint)
    {
        Vector3 towardHit = hitPoint - transform.position;
        towardHit.y = 0.0f;

        if (towardHit.sqrMagnitude < 0.0001f) return true;

        return Vector3.Angle(transform.forward, towardHit) <= guardArcDegrees * 0.5f;
    }

    private void CreateGuardPlate()
    {
        GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plate.name = "GuardPlate";
        plate.layer = gameObject.layer;

        if (plate.TryGetComponent(out Collider plateCollider))
        {
            Destroy(plateCollider);
        }

        plateTransform = plate.transform;
        plateTransform.SetParent(playerMovement != null ? playerMovement.Visuals : transform, false);

        plateRenderer = plate.GetComponent<MeshRenderer>();
        plateRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        plateRenderer.receiveShadows = false;

        plateMaterial = new Material(Shader.Find("Sprites/Default"));
        plateRenderer.material = plateMaterial;

        plate.SetActive(false);
    }

    private void UpdateGuardPlate()
    {
        if (plateTransform == null) return;

        bool visible = raiseProgress > 0.0f;
        if (plateRenderer.gameObject.activeSelf != visible)
        {
            plateRenderer.gameObject.SetActive(visible);
        }

        if (!visible) return;

        PlacePlate();
        PaintPlate();
    }

    private void PlacePlate()
    {
        float height = Mathf.Lerp(plateHeight - 0.35f, plateHeight, raiseProgress);

        plateTransform.localPosition = new Vector3(0.0f, height, plateForwardOffset);
        plateTransform.localRotation = Quaternion.identity;
        plateTransform.localScale = plateSize * Mathf.Lerp(0.6f, 1.0f, raiseProgress);
    }

    private void PaintPlate()
    {
        Color target = impactFlashRemaining > 0.0f ? plateImpactColor : plateColor;
        target.a *= raiseProgress;
        plateMaterial.color = target;
    }

    private void OnDestroy()
    {
        if (plateMaterial != null) Destroy(plateMaterial);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.45f, 0.7f, 1.0f, 1.0f);
        Vector3 left = Quaternion.AngleAxis(-guardArcDegrees * 0.5f, Vector3.up) * transform.forward;
        Vector3 right = Quaternion.AngleAxis(guardArcDegrees * 0.5f, Vector3.up) * transform.forward;
        Gizmos.DrawRay(transform.position, left * 2.0f);
        Gizmos.DrawRay(transform.position, right * 2.0f);
    }
}
