using FishNet.Object;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(Wallet))]
public class PlayerInteract : NetworkBehaviour
{
    public enum Hover { None, InReach, TooFar }

    [Header("Input Actions")]
    [SerializeField] private InputAction interactAction;

    [Header("Detection")]
    [SerializeField] private float pointerRange = 12.0f;
    [SerializeField] private float hoverRadiusPixels = 48.0f;

    private Camera pickCamera;

    private IInteractable hoveredItem;
    private Hover hoveredState;

    private IInteractable highlightedItem;

    private IInteractable heldItem;
    private float heldDuration;
    private float pressTimer;
    private bool isPressing;
    private bool ringFinished;

    public Hover HoverState => hoveredState;
    public IInteractable TargetedItem => IsAlive(heldItem) ? heldItem : hoveredItem;

    public float HoldProgress =>
        isPressing && !ringFinished && heldDuration > 0.0f
            ? Mathf.Clamp01(pressTimer / heldDuration)
            : 0.0f;

    public bool ConsumesClick =>
        isPressing || (hoveredState == Hover.InReach && interactAction.WasPressedThisFrame());

    private void Awake()
    {
        if (interactAction.bindings.Count == 0)
        {
            interactAction.AddBinding("<Mouse>/leftButton");
        }
    }

    private void OnEnable()
    {
        interactAction.Enable();
    }

    private void OnDisable()
    {
        interactAction.Disable();
        ReleaseHold();
    }

    private void Update()
    {
        if (!IsOwner) return;

        RefreshHover();
        HandleInteractInput();
    }

    private void RefreshHover()
    {
        hoveredItem = FindItemUnderCursor(out hoveredState);

        ApplyHighlight(isPressing && IsAlive(heldItem) ? heldItem
                     : hoveredState == Hover.InReach ? hoveredItem
                     : null);
    }

    private IInteractable FindItemUnderCursor(out Hover state)
    {
        state = Hover.None;

        if (UIPointer.IsOverBlockingUI()) return null;

        if (pickCamera == null) pickCamera = Camera.main;
        if (pickCamera == null || Mouse.current == null) return null;

        Vector2 cursor = Mouse.current.position.ReadValue();
        Collider[] colliders = Physics.OverlapSphere(transform.position, pointerRange, ~0, QueryTriggerInteraction.Collide);

        IInteractable best = null;
        float bestDistance = hoverRadiusPixels;

        foreach (Collider itemCollider in colliders)
        {
            IInteractable item = itemCollider.GetComponentInParent<IInteractable>();
            if (item == null) continue;

            Vector3 screenPoint = pickCamera.WorldToScreenPoint(item.Transform.position);
            if (screenPoint.z <= 0.0f) continue;

            float distance = Vector2.Distance(cursor, new Vector2(screenPoint.x, screenPoint.y));
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = item;
            }
        }

        if (best != null) state = best.IsInRange(transform) ? Hover.InReach : Hover.TooFar;
        return best;
    }

    private void HandleInteractInput()
    {
        if (interactAction.WasPressedThisFrame() && hoveredState == Hover.InReach && IsAlive(hoveredItem))
        {
            heldItem = hoveredItem;
            heldDuration = Mathf.Max(0.0f, heldItem.HoldDuration);
            pressTimer = 0.0f;
            isPressing = true;
            ringFinished = heldDuration <= 0.0f;

            heldItem.BeginHold(gameObject);
        }

        if (isPressing && !ringFinished)
        {
            if (!IsAlive(heldItem) || !heldItem.IsInRange(transform))
            {
                ReleaseHold();
                return;
            }

            pressTimer += Time.deltaTime;
            if (pressTimer >= heldDuration) ringFinished = true;
        }

        if (interactAction.WasReleasedThisFrame())
        {
            ReleaseHold();
        }
    }

    private void ReleaseHold()
    {
        if (IsAlive(heldItem)) heldItem.CancelHold(gameObject);

        heldItem = null;
        heldDuration = 0.0f;
        isPressing = false;
        ringFinished = false;
        pressTimer = 0.0f;
    }

    private void ApplyHighlight(IInteractable item)
    {
        if (!IsAlive(highlightedItem)) highlightedItem = null;
        if (ReferenceEquals(highlightedItem, item)) return;

        if (IsAlive(highlightedItem)) highlightedItem.SetHighlighted(false);

        highlightedItem = item;

        if (IsAlive(highlightedItem)) highlightedItem.SetHighlighted(true);
    }

    private static bool IsAlive(IInteractable item) => (item as MonoBehaviour) != null;
}
