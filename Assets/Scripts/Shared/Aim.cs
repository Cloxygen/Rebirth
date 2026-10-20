using UnityEngine;
using UnityEngine.InputSystem;

public class Aim : MonoBehaviour
{
    [SerializeField] private LayerMask aimLayers = ~0;
    [SerializeField] private LayerMask hittableLayers = ~0;

    private CharacterController characterController;
    private Camera aimCamera;

    public Vector3 Point { get; private set; }
    public LayerMask HittableLayers => hittableLayers;

    public void PointAt(Vector3 worldPoint)
    {
        Point = worldPoint;
    }

    public int LineOfSightLayers => aimLayers.value & ~(1 << gameObject.layer);

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    public bool TryReadCursorPoint(out Vector3 point)
    {
        point = Vector3.zero;

        if (aimCamera == null) aimCamera = Camera.main;
        if (aimCamera == null || Mouse.current == null) return false;

        Ray ray = aimCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, aimLayers, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        point = hit.point;
        return true;
    }

    public Vector3 GetLaunchPosition(float heightAboveGround, float forwardOffset)
    {
        float soleOffset = characterController != null
            ? (characterController.center.y - characterController.height * 0.5f) * transform.lossyScale.y
            : 0.0f;

        return transform.position
            + Vector3.up * (soleOffset + heightAboveGround)
            + transform.forward * forwardOffset;
    }

    public int SweepLayers => hittableLayers.value & ~(1 << gameObject.layer);
}
