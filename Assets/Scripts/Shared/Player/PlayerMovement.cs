using FishNet.Component.Transforming.Beta;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using FishNet.Utility.Template;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : TickNetworkBehaviour
{
    public enum RotationStyle
    {
        FaceMovementDirection,
        FaceMouseCursor
    }

    public struct MoveInput : IReplicateData
    {
        public Vector3 Direction;
        public Vector3 AimPoint;
        public bool Sprint;
        public bool Dash;
        public bool Jump;
        public bool Guard;

        public byte HeldSlots;
        public byte PressedSlots;

        private uint tick;

        public void Dispose() { }
        public uint GetTick() => tick;
        public void SetTick(uint value) => tick = value;
    }

    public struct MoveState : IReconcileData
    {
        public Vector3 Position;

        public float Yaw;
        public Vector3 DashDirection;
        public float VerticalVelocity;
        public float ActiveSpeed;
        public float DashTimeRemaining;
        public float DashCooldownRemaining;
        public float ForwardSpeed;
        public bool Dashing;
        public bool Grounded;
        public bool Dead;
        public uint Teleports;

        private uint tick;

        public void Dispose() { }
        public uint GetTick() => tick;
        public void SetTick(uint value) => tick = value;
    }

    private const float MaximumAimDistance = 60.0f;

    [Header("Input Actions")]
    [SerializeField] private InputAction moveAction;
    [SerializeField] private InputAction sprintAction;
    [SerializeField] private InputAction dashAction;
    [SerializeField] private InputAction jumpAction;

    [Header("Movement Settings")]
    [SerializeField] private float walkingSpeed = 5.0f;
    [SerializeField] private float sprintingSpeed = 8.0f;
    [SerializeField] private float speedTransitionSmoothness = 10.0f;
    [SerializeField] private float gravityForce = 20.0f;
    [SerializeField] private float jumpForce = 8.0f;

    [Header("Rotation Settings")]
    [SerializeField] private RotationStyle rotationStyle = RotationStyle.FaceMovementDirection;
    [SerializeField] private float rotationSpeed = 15.0f;

    [Header("Dash Settings")]
    [SerializeField] private float dashForce = 15.0f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldownDuration = 1.0f;

    [Header("Ground Settings")]
    [SerializeField] private LayerMask groundProbeMask = ~0;
    [SerializeField] private float groundSnapDistance = 0.5f;

    private const float GroundProbeStartHeight = 0.2f;

    [Header("Footstep Timing")]
    [SerializeField] private float leftFootstepPhase = 0.0f;
    [SerializeField] private float rightFootstepPhase = 0.5f;

    private CharacterController characterController;
    private ActionBar actionBar;
    private PlayerBlock playerBlock;
    private Aim playerAim;
    private Camera mainCamera;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private WalkingParticles walkingParticles;
    [SerializeField] private Health health;
    [SerializeField] private PlayerRespawn playerRespawn;
    [SerializeField] private NetworkTickSmoother modelSmoother;

    [Header("Footstep Bones")]
    [SerializeField] private Transform leftFootBone;
    [SerializeField] private Transform rightFootBone;

    private float lastFootstepPhase;

    private Vector3 movementInputDirection;
    private Vector3 rawInputDirection;
    private Vector3 dashDirection;
    private Vector3 currentGroundSlopeNormal = Vector3.up;

    private float activeMovementSpeed;
    private float verticalVelocity;
    private float dashCooldownTimeRemaining;
    private float dashTimeRemaining;

    private float forwardSpeed;

    private bool isSprinting;
    private bool isDashing;

    private bool isGrounded;
    private bool wantsToDash;
    private bool wantsToJump;
    private bool isDead;
    private uint teleportCount;

    private bool pressedDashSinceTick;
    private bool pressedJumpSinceTick;

    private Vector3 lastAimPoint;

    public float DashCooldownRemaining => dashCooldownTimeRemaining;
    public float DashCooldownDuration => dashCooldownDuration;
    public bool IsSprinting => isSprinting;
    public bool IsDashing => isDashing;

    public event System.Action Teleported;

    public Transform Visuals => modelSmoother != null ? modelSmoother.transform : transform;

    private float assertedSpeedMultiplier = 1.0f;
    private float assertedLungeSpeed;
    private bool assertedDashBlock;
    private bool assertedAimFacing;
    private bool assertedYawFacing;
    private float assertedYaw;

    public void Slow(float multiplier)
    {
        if (multiplier < assertedSpeedMultiplier) assertedSpeedMultiplier = multiplier;
    }

    public void FaceAimPoint()
    {
        assertedAimFacing = true;
    }

    public void FaceYaw(float yaw)
    {
        assertedYawFacing = true;
        assertedYaw = yaw;
    }

    public void BlockDash()
    {
        assertedDashBlock = true;
    }

    public void Lunge(float unitsPerSecond)
    {
        if (unitsPerSecond > assertedLungeSpeed) assertedLungeSpeed = unitsPerSecond;
    }

    private void ClearAssertions()
    {
        assertedSpeedMultiplier = 1.0f;
        assertedLungeSpeed = 0.0f;
        assertedDashBlock = false;
        assertedAimFacing = false;
        assertedYawFacing = false;
    }

    private float slowInEffect = 1.0f;
    private float lungeInEffect;
    private bool dashBlockInEffect;
    private bool aimFacingInEffect;
    private bool yawFacingInEffect;
    private float yawInEffect;

    private struct TickAssertions
    {
        public uint Tick;
        public bool Recorded;
        public float SpeedMultiplier;
        public float LungeSpeed;
        public bool DashBlocked;
        public bool AimFacing;
        public bool YawFacing;
        public float Yaw;
    }

    private const int AssertionHistoryLength = 512;
    private readonly TickAssertions[] assertionHistory = new TickAssertions[AssertionHistoryLength];

    private void ResolveAssertions(uint tick, bool replaying)
    {
        ref TickAssertions slot = ref assertionHistory[tick % AssertionHistoryLength];

        if (!replaying)
        {
            slowInEffect = assertedSpeedMultiplier;
            lungeInEffect = assertedLungeSpeed;
            dashBlockInEffect = assertedDashBlock;
            aimFacingInEffect = assertedAimFacing;
            yawFacingInEffect = assertedYawFacing;
            yawInEffect = assertedYaw;

            slot.Tick = tick;
            slot.Recorded = true;
            slot.SpeedMultiplier = slowInEffect;
            slot.LungeSpeed = lungeInEffect;
            slot.DashBlocked = dashBlockInEffect;
            slot.AimFacing = aimFacingInEffect;
            slot.YawFacing = yawFacingInEffect;
            slot.Yaw = yawInEffect;
            return;
        }

        bool usable = slot.Recorded && slot.Tick == tick;

        slowInEffect = usable ? slot.SpeedMultiplier : 1.0f;
        lungeInEffect = usable ? slot.LungeSpeed : 0.0f;
        dashBlockInEffect = usable && slot.DashBlocked;
        aimFacingInEffect = usable && slot.AimFacing;
        yawFacingInEffect = usable && slot.YawFacing;
        yawInEffect = slot.Yaw;
    }

    private void OnEnable()
    {
        moveAction.Enable();
        sprintAction.Enable();
        dashAction.Enable();
        jumpAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        sprintAction.Disable();
        dashAction.Disable();
        jumpAction.Disable();
    }

    private void Awake()
    {
        SetTickCallbacks(TickCallback.Tick | TickCallback.PostTick);

        InitializeComponents();
    }

    private void Update()
    {
        if (IsOwner) CollectOneTimeInput();

        UpdateAnimatorParameters();
        UpdateFootstepEvents();
    }

    private void CollectOneTimeInput()
    {
        if (UITextEntry.Active) return;

        bool canDash = dashCooldownTimeRemaining <= 0.0f && !isDashing && !assertedDashBlock;
        if (dashAction.WasPressedThisFrame() && canDash) pressedDashSinceTick = true;

        if (jumpAction.WasPressedThisFrame() && isGrounded && !isDashing)
        {
            pressedJumpSinceTick = true;
        }
    }

    protected override void TimeManager_OnTick()
    {
        if (IsServerInitialized && playerRespawn != null) playerRespawn.AdvanceCountdown((float)TimeManager.TickDelta);

        PerformMove(BuildInput());

        ClearAssertions();

        if (playerAim != null) playerAim.PointAt(lastAimPoint);

        if (playerBlock != null) playerBlock.AdvanceGuard((float)TimeManager.TickDelta);

        if (actionBar != null) actionBar.AdvanceAbilities((float)TimeManager.TickDelta);
    }

    protected override void TimeManager_OnPostTick()
    {
        if (IsServerInitialized) isDead = health != null && health.IsDead;
        CreateReconcile();
    }

    private MoveInput BuildInput()
    {
        if (!IsOwner) return default;

        MoveInput input = new MoveInput();

        if (!UITextEntry.Active)
        {
            input.Direction = ReadSteeringDirection();
            input.Sprint = sprintAction.IsPressed();
        }

        input.Dash = pressedDashSinceTick;
        input.Jump = pressedJumpSinceTick;

        if (actionBar != null) actionBar.TakeSlotInput(out input.HeldSlots, out input.PressedSlots);
        if (playerBlock != null) input.Guard = playerBlock.TakeGuardInput();

        if (playerAim != null && playerAim.TryReadCursorPoint(out Vector3 aimPoint)) input.AimPoint = aimPoint;

        pressedDashSinceTick = false;
        pressedJumpSinceTick = false;

        return input;
    }

    public override void CreateReconcile()
    {
        MoveState state = new MoveState
        {
            Position = transform.position,
            Yaw = transform.eulerAngles.y,
            DashDirection = dashDirection,
            VerticalVelocity = verticalVelocity,
            ActiveSpeed = activeMovementSpeed,
            DashTimeRemaining = dashTimeRemaining,
            DashCooldownRemaining = dashCooldownTimeRemaining,
            ForwardSpeed = forwardSpeed,
            Dashing = isDashing,
            Grounded = isGrounded,
            Dead = isDead,
            Teleports = teleportCount
        };

        PerformReconcile(state);
    }

    [Replicate]
    private void PerformMove(MoveInput input, ReplicateState state = ReplicateState.Invalid,
                             Channel channel = Channel.Unreliable)
    {
        float delta = (float)TimeManager.TickDelta;
        Vector3 tickStartPosition = transform.position;

        ResolveAssertions(input.GetTick(), state.ContainsReplayed());

        if (state.ContainsCreated()) lastAimPoint = AimPointWithinReach(input.AimPoint);

        if (state.IsTickedCreated() && actionBar != null)
        {
            actionBar.ReceiveSlotInput(input.HeldSlots, input.PressedSlots);
        }

        if (state.IsTickedCreated() && playerBlock != null) playerBlock.ReceiveGuardInput(input.Guard);

        movementInputDirection = FlattenToGround(input.Direction);
        isSprinting = input.Sprint;
        wantsToDash = input.Dash;
        wantsToJump = input.Jump;

        UpdateDashCooldown(delta);
        UpdateGroundNormal();

        bool movementLocked = slowInEffect <= 0.0f;

        if (isDead)
        {
            HoldDeadBody(delta);
        }
        else if (isDashing)
        {
            ExecuteDashMovement(delta);
        }
        else if (movementLocked)
        {
            ApplyGravity(delta);
            ApplyJump();
            characterController.Move(new Vector3(0.0f, verticalVelocity, 0.0f) * delta);

            ExecutePlayerRotation(delta, lastAimPoint);
        }
        else
        {
            ExecuteStandardMovement(delta);
            ExecutePlayerRotation(delta, lastAimPoint);
        }

        if (!isDashing && !isDead && lungeInEffect > 0.0f)
        {
            characterController.Move(transform.forward * (lungeInEffect * delta));
        }

        isGrounded = characterController.isGrounded;

        if (state.ContainsCreated())
        {
            Vector3 travelled = (transform.position - tickStartPosition) / delta;
            forwardSpeed = transform.InverseTransformDirection(travelled).z;
        }

        if (state.IsTickedCreated()) RecordPrediction(input.GetTick());
    }

    private readonly Vector3[] predictedPositions = new Vector3[AssertionHistoryLength];
    private readonly float[] predictedYaws = new float[AssertionHistoryLength];
    private readonly uint[] predictedTicks = new uint[AssertionHistoryLength];

    private void RecordPrediction(uint tick)
    {
        int slot = (int)(tick % AssertionHistoryLength);
        predictedTicks[slot] = tick;
        predictedPositions[slot] = transform.position;
        predictedYaws[slot] = transform.eulerAngles.y;
    }

    private void LogCorrection(MoveState state)
    {
        if (!IsOwner || IsServerInitialized) return;

        int slot = (int)(state.GetTick() % AssertionHistoryLength);
        if (predictedTicks[slot] != state.GetTick()) return;

        float positionError = (state.Position - predictedPositions[slot]).magnitude;
        float yawError = Mathf.Abs(Mathf.DeltaAngle(predictedYaws[slot], state.Yaw));
        if (positionError < 0.01f && yawError < 0.5f) return;

        Debug.Log($"[Correction] tick {state.GetTick()} position off by {positionError:F3} yaw off by {yawError:F1} slow {slowInEffect:F2} speed {activeMovementSpeed:F2} -> {state.ActiveSpeed:F2}");
    }

    [Reconcile]
    private void PerformReconcile(MoveState state, Channel channel = Channel.Unreliable)
    {
        LogCorrection(state);
        RestoreMotion(state);
        RestoreDash(state);
        isDead = state.Dead;
        PlaceBody(state.Position, state.Yaw);

        if (state.Teleports > teleportCount) CatchUpWithTeleport(state.Teleports);
    }

    private void RestoreMotion(MoveState state)
    {
        verticalVelocity = state.VerticalVelocity;
        activeMovementSpeed = state.ActiveSpeed;
        forwardSpeed = state.ForwardSpeed;
        isGrounded = state.Grounded;
    }

    private void RestoreDash(MoveState state)
    {
        dashTimeRemaining = state.DashTimeRemaining;
        dashCooldownTimeRemaining = state.DashCooldownRemaining;
        dashDirection = state.DashDirection;
        isDashing = state.Dashing;
    }

    public void Teleport(Vector3 position, float yaw)
    {
        if (!IsServerInitialized) return;

        PlaceBody(position, yaw);
        StopMomentum();
        teleportCount++;
        AnnounceTeleport();
    }

    private void PlaceBody(Vector3 position, float yaw)
    {
        characterController.enabled = false;
        transform.SetPositionAndRotation(position, Quaternion.Euler(0.0f, yaw, 0.0f));
        characterController.enabled = true;
    }

    private void StopMomentum()
    {
        verticalVelocity = 0.0f;
        activeMovementSpeed = 0.0f;
        isDashing = false;
        dashTimeRemaining = 0.0f;
        forwardSpeed = 0.0f;
    }

    private void AnnounceTeleport()
    {
        SnapModelToBody();
        Teleported?.Invoke();
    }

    private void SnapModelToBody()
    {
        if (modelSmoother == null) return;
        modelSmoother.SmootherController?.UniversalSmoother?.Teleport();
    }

    private void CatchUpWithTeleport(uint serverTeleportCount)
    {
        teleportCount = serverTeleportCount;
        AnnounceTeleport();
    }

    private void HoldDeadBody(float delta)
    {
        isDashing = false;
        dashTimeRemaining = 0.0f;
        ApplyGravity(delta);
        characterController.Move(Vector3.up * (verticalVelocity * delta));
    }

    private Vector3 AimPointWithinReach(Vector3 aimPoint)
    {
        Vector3 offset = aimPoint - transform.position;
        if (offset.sqrMagnitude <= MaximumAimDistance * MaximumAimDistance) return aimPoint;

        return transform.position + offset.normalized * MaximumAimDistance;
    }

    private void InitializeComponents()
    {
        characterController = GetComponent<CharacterController>();
        actionBar = GetComponent<ActionBar>();
        playerBlock = GetComponent<PlayerBlock>();
        playerAim = GetComponent<Aim>();
        mainCamera = Camera.main;

        lastAimPoint = transform.position + transform.forward;

        if (playerAnimator == null)
        {
            playerAnimator = GetComponentInChildren<Animator>();
        }

        if (walkingParticles == null)
        {
            walkingParticles = GetComponent<WalkingParticles>();
        }

        if (playerAnimator != null)
        {
            if (leftFootBone == null)
            {
                leftFootBone = FindBoneRecursive(playerAnimator.transform, "Foot.L");
            }

            if (rightFootBone == null)
            {
                rightFootBone = FindBoneRecursive(playerAnimator.transform, "Foot.R");
            }

            if (leftFootBone == null || rightFootBone == null)
            {
                Debug.LogWarning("PlayerMovement: could not find Foot.L/Foot.R bones for footstep decals; assign leftFootBone/rightFootBone manually.", this);
            }
        }

        activeMovementSpeed = walkingSpeed;
    }

    private static Transform FindBoneRecursive(Transform root, string boneName)
    {
        if (root.name == boneName) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindBoneRecursive(root.GetChild(i), boneName);
            if (found != null) return found;
        }

        return null;
    }

    private Vector3 ReadSteeringDirection()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        rawInputDirection = new Vector3(moveInput.x, 0.0f, moveInput.y);

        if (mainCamera == null) return rawInputDirection.normalized;

        Vector3 cameraForwardDirection = mainCamera.transform.forward;
        Vector3 cameraRightDirection = mainCamera.transform.right;

        cameraForwardDirection.y = 0.0f;
        cameraRightDirection.y = 0.0f;

        cameraForwardDirection.Normalize();
        cameraRightDirection.Normalize();

        return (cameraForwardDirection * moveInput.y + cameraRightDirection * moveInput.x).normalized;
    }

    private static Vector3 FlattenToGround(Vector3 sentDirection)
    {
        sentDirection.y = 0.0f;
        return sentDirection.normalized;
    }

    private void UpdateDashCooldown(float delta)
    {
        if (dashCooldownTimeRemaining > 0.0f)
        {
            dashCooldownTimeRemaining -= delta;
        }
    }

    private void ExecuteStandardMovement(float delta)
    {
        if (CanStartDash())
        {
            StartDash();
            return;
        }

        float targetSpeed = isSprinting ? sprintingSpeed : walkingSpeed;

        if (slowInEffect < 1.0f) targetSpeed = walkingSpeed * slowInEffect;

        activeMovementSpeed = Mathf.Lerp(activeMovementSpeed, targetSpeed, delta * speedTransitionSmoothness);

        Vector3 targetMoveVector = movementInputDirection * activeMovementSpeed;

        ApplyGravity(delta);
        ApplyJump();

        Vector3 totalMovementVelocity;
        if (isGrounded && verticalVelocity <= 0.0f)
        {
            Vector3 slopeMovementVelocity = Vector3.ProjectOnPlane(targetMoveVector, currentGroundSlopeNormal);
            bool isMovingDownSlope = slopeMovementVelocity.y < -0.001f;
            if (isMovingDownSlope && slopeMovementVelocity.sqrMagnitude > 0.001f)
            {
                slopeMovementVelocity = slopeMovementVelocity.normalized * targetMoveVector.magnitude;
                totalMovementVelocity = slopeMovementVelocity;
                totalMovementVelocity.y += verticalVelocity;
            }
            else
            {
                totalMovementVelocity = new Vector3(targetMoveVector.x, verticalVelocity, targetMoveVector.z);
            }
        }
        else
        {
            totalMovementVelocity = new Vector3(targetMoveVector.x, verticalVelocity, targetMoveVector.z);
        }

        bool wasGroundedBeforeMove = isGrounded;

        characterController.Move(totalMovementVelocity * delta);

        if (wasGroundedBeforeMove && verticalVelocity <= 0.0f)
        {
            SnapToGround();
        }
    }

    private void ApplyGravity(float delta)
    {
        if (isGrounded)
        {
            verticalVelocity = -2.0f;
        }
        else
        {
            verticalVelocity -= gravityForce * delta;
        }
    }

    private void ApplyJump()
    {
        if (wantsToJump && isGrounded)
        {
            verticalVelocity = jumpForce;
            wantsToJump = false;
        }
    }

    private void ExecutePlayerRotation(float delta, Vector3 aimPoint)
    {
        if (yawFacingInEffect)
        {
            RotateYawTowards(yawInEffect, delta);
            return;
        }

        if (aimFacingInEffect)
        {
            LookAtTargetPosition(aimPoint, delta);
            return;
        }

        if (rotationStyle == RotationStyle.FaceMovementDirection)
        {
            RotateTowardsMovementDirection(delta);
        }
        else if (rotationStyle == RotationStyle.FaceMouseCursor)
        {
            LookAtTargetPosition(aimPoint, delta);
        }
    }

    private void RotateTowardsMovementDirection(float delta)
    {
        bool isMoving = movementInputDirection.sqrMagnitude > 0.01f;
        if (isMoving)
        {
            RotateYawTowards(YawAlong(movementInputDirection), delta);
        }
    }

    private void LookAtTargetPosition(Vector3 targetPosition, float delta)
    {
        Vector3 lookDirection = targetPosition - transform.position;
        lookDirection.y = 0.0f;

        bool isTargetFarEnough = lookDirection.sqrMagnitude > 0.01f;
        if (isTargetFarEnough)
        {
            RotateYawTowards(YawAlong(lookDirection), delta);
        }
    }

    private static float YawAlong(Vector3 direction) => Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

    private void RotateYawTowards(float targetYaw, float delta)
    {
        float currentYaw = transform.eulerAngles.y;
        float step = Mathf.DeltaAngle(currentYaw, targetYaw) * Mathf.Clamp01(rotationSpeed * delta);

        transform.rotation = Quaternion.Euler(0.0f, currentYaw + step, 0.0f);
    }

    private bool CanStartDash()
    {
        bool cooledDown = dashCooldownTimeRemaining <= 0.0f;
        return wantsToDash && cooledDown && !dashBlockInEffect && movementInputDirection.sqrMagnitude > 0.01f;
    }

    private void StartDash()
    {
        wantsToDash = false;
        isDashing = true;
        dashTimeRemaining = dashDuration;
        dashCooldownTimeRemaining = dashCooldownDuration;
        dashDirection = movementInputDirection;
    }

    private void ExecuteDashMovement(float delta)
    {
        dashTimeRemaining -= delta;
        if (dashTimeRemaining <= 0.0f)
        {
            isDashing = false;
        }

        characterController.Move(dashDirection * dashForce * delta);
    }

    private void UpdateAnimatorParameters()
    {
        if (playerAnimator == null) return;

        playerAnimator.SetFloat("ForwardSpeed", forwardSpeed);
        playerAnimator.SetBool("IsGrounded", isGrounded);
        playerAnimator.SetFloat("VerticalVelocity", verticalVelocity);
    }

    private void UpdateFootstepEvents()
    {
        if (playerAnimator == null || walkingParticles == null) return;

        if (!isGrounded || Mathf.Abs(forwardSpeed) < 0.1f)
        {
            lastFootstepPhase = 0.0f;
            return;
        }

        AnimatorStateInfo state = playerAnimator.GetCurrentAnimatorStateInfo(0);
        float phase = state.normalizedTime % 1.0f;

        if (leftFootBone != null && CrossedPhase(lastFootstepPhase, phase, leftFootstepPhase))
        {
            walkingParticles.SpawnFootstepDecal(leftFootBone.position);
        }

        if (rightFootBone != null && CrossedPhase(lastFootstepPhase, phase, rightFootstepPhase))
        {
            walkingParticles.SpawnFootstepDecal(rightFootBone.position);
        }

        lastFootstepPhase = phase;
    }

    private static bool CrossedPhase(float previousPhase, float currentPhase, float threshold)
    {
        if (currentPhase >= previousPhase)
        {
            return previousPhase < threshold && currentPhase >= threshold;
        }

        return previousPhase < threshold || currentPhase >= threshold;
    }

    private bool TryProbeGround(float maximumDistance, out RaycastHit groundHit)
    {
        Vector3 footCenter = transform.TransformPoint(characterController.center)
                             + Vector3.down * (characterController.height * 0.5f - characterController.radius);

        float probeRadius = Mathf.Max(0.01f, characterController.radius - characterController.skinWidth);

        bool hitSomething = Physics.SphereCast(
            footCenter + Vector3.up * GroundProbeStartHeight,
            probeRadius,
            Vector3.down,
            out groundHit,
            GroundProbeStartHeight + maximumDistance,
            groundProbeMask,
            QueryTriggerInteraction.Ignore);

        return hitSomething && Vector3.Angle(Vector3.up, groundHit.normal) <= characterController.slopeLimit;
    }

    private void UpdateGroundNormal()
    {
        currentGroundSlopeNormal = TryProbeGround(groundSnapDistance, out RaycastHit ground)
            ? ground.normal
            : Vector3.up;
    }

    private void SnapToGround()
    {
        if (characterController.isGrounded) return;
        if (!TryProbeGround(groundSnapDistance, out RaycastHit ground)) return;

        float gapBelowFeet = ground.distance - GroundProbeStartHeight;
        if (gapBelowFeet <= 0.0f) return;

        characterController.Move(Vector3.down * gapBelowFeet);
    }
}
