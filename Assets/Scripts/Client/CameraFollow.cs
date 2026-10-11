using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    public Transform player;
    public Vector3 offset = new Vector3(0, 15, -10);

    [Header("Framing")]
    [Range(-0.4f, 0.4f)]
    [SerializeField] private float raisePlayerOnScreen = 0.12f;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSensitivity = 2f;
    [SerializeField] private bool requireMiddleClickToRotate = true;

    private float currentYawAngleDegrees;
    private float horizontalDistanceToPlayer;
    private float verticalDistanceToPlayer;
    private float gamepadMultiplier = 50f;
    private bool cursorLocked;
    private Vector3 smoothedTarget;
    private Vector3 targetVelocity;
    private bool snapToPlayer;
    private PlayerMovement followedMovement;

    private float followSmoothTime = 1.0f / 30.0f;
    private Vector2 cachedMousePosition;
    private Camera framingCamera;
    private bool framingApplied;
    private Vector3 appliedOffset;

    private void Start()
    {
        InitializeCameraDimensions();
    }

    private void LateUpdate()
    {
        if (player == null)
        {
            ResolvePlayer();
            if (player == null) return;
        }

        ApplyOffsetChanges();
        UpdateCameraRotation();
        FollowPlayer();
    }

    private void OnDisable()
    {
        SetCursorLocked(false);

        if (framingApplied && framingCamera != null)
        {
            framingCamera.ResetProjectionMatrix();
            framingApplied = false;
        }
    }

    private void ResolvePlayer()
    {
        Transform body = LocalPlayer.Body;
        if (body == null) return;
        player = body;
        snapToPlayer = true;
        ListenForTeleports();
        MatchSmoothingToTickRate(body);
    }

    private void MatchSmoothingToTickRate(Transform body)
    {
        FishNet.Object.NetworkObject networkedBody = body.GetComponent<FishNet.Object.NetworkObject>();
        if (networkedBody != null && networkedBody.TimeManager != null)
        {
            followSmoothTime = (float)networkedBody.TimeManager.TickDelta * 2.0f;
        }
    }

    private void ListenForTeleports()
    {
        StopListeningForTeleports();
        followedMovement = player.GetComponent<PlayerMovement>();
        if (followedMovement != null) followedMovement.Teleported += SnapToPlayer;
    }

    private void StopListeningForTeleports()
    {
        if (followedMovement != null) followedMovement.Teleported -= SnapToPlayer;
    }

    private void SnapToPlayer()
    {
        snapToPlayer = true;
    }

    private void OnDestroy()
    {
        StopListeningForTeleports();
    }

    private void InitializeCameraDimensions()
    {
        horizontalDistanceToPlayer = new Vector3(offset.x, 0f, offset.z).magnitude;
        verticalDistanceToPlayer = offset.y;
        currentYawAngleDegrees = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
        appliedOffset = offset;
    }

    private void ApplyOffsetChanges()
    {
        if (offset == appliedOffset) return;

        horizontalDistanceToPlayer = new Vector3(offset.x, 0f, offset.z).magnitude;
        verticalDistanceToPlayer = offset.y;

        if (!Mathf.Approximately(offset.x, appliedOffset.x) ||
            !Mathf.Approximately(offset.z, appliedOffset.z))
        {
            currentYawAngleDegrees = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
        }

        appliedOffset = offset;
    }

    private void UpdateCameraRotation()
    {
        bool isRotationActive = IsMouseRotationActive();
        SetCursorLocked(isRotationActive);

        float rotationInput = GetHorizontalRotationInput(isRotationActive);
        currentYawAngleDegrees += rotationInput;
    }

    private bool IsMouseRotationActive()
    {
        return Mouse.current != null && (!requireMiddleClickToRotate || Mouse.current.middleButton.isPressed);
    }

    private void SetCursorLocked(bool locked)
    {
        if (locked == cursorLocked) return;
        cursorLocked = locked;

        if (locked)
        {
            if (Mouse.current != null) cachedMousePosition = Mouse.current.position.ReadValue();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (Mouse.current != null) Mouse.current.WarpCursorPosition(cachedMousePosition);
        }
    }

    private float GetHorizontalRotationInput(bool isRotationActive)
    {
        float input = 0f;

        if (isRotationActive)
        {
            input += Mouse.current.delta.x.ReadValue() * rotationSensitivity * Time.deltaTime;
        }

        if (Gamepad.current != null)
        {
            input += Gamepad.current.rightStick.x.ReadValue() * rotationSensitivity * gamepadMultiplier * Time.deltaTime;
        }

        return input;
    }

    private void FollowPlayer()
    {
        float angleInRadians = currentYawAngleDegrees * Mathf.Deg2Rad;
        float offsetX = Mathf.Sin(angleInRadians) * horizontalDistanceToPlayer;
        float offsetZ = Mathf.Cos(angleInRadians) * horizontalDistanceToPlayer;

        Vector3 calculatedOffset = new Vector3(offsetX, verticalDistanceToPlayer, offsetZ);

        smoothedTarget = snapToPlayer
            ? player.position
            : Vector3.SmoothDamp(smoothedTarget, player.position, ref targetVelocity, followSmoothTime);

        if (snapToPlayer) targetVelocity = Vector3.zero;
        snapToPlayer = false;

        transform.position = smoothedTarget + calculatedOffset;
        transform.LookAt(smoothedTarget);
        ApplyFraming();
    }

    private void ApplyFraming()
    {
        if (framingCamera == null) framingCamera = GetComponent<Camera>();
        if (framingCamera == null) return;

        if (Mathf.Approximately(raisePlayerOnScreen, 0f))
        {
            if (framingApplied)
            {
                framingCamera.ResetProjectionMatrix();
                framingApplied = false;
            }

            return;
        }

        Matrix4x4 projection = Matrix4x4.Perspective(
            framingCamera.fieldOfView,
            framingCamera.aspect,
            framingCamera.nearClipPlane,
            framingCamera.farClipPlane);

        projection[1, 2] = -2f * raisePlayerOnScreen;

        framingCamera.projectionMatrix = projection;
        framingApplied = true;
    }
}

