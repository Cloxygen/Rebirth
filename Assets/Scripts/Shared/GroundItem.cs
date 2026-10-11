using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GroundItem : NetworkBehaviour, IInteractable
{
    private const float PopOriginHeight = 0.3f;
    private const float PopArcHeight = 0.6f;
    private const float PopDuration = 0.4f;

    [SerializeField] private float holdDuration = 0.4f;
    [SerializeField] private float interactRange = 4.0f;
    [SerializeField] private float groundedHeight = 0.15f;

    private Item item;

    private readonly SyncVar<string> sentName = new SyncVar<string>();
    private readonly SyncVar<ItemRarity> sentRarity = new SyncVar<ItemRarity>();
    private readonly SyncVar<Vector3> landing = new SyncVar<Vector3>();

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float popTimer;
    private bool popping;

    private GameObject holder;
    private float holdElapsed;

    public Transform Transform => transform;
    public string DisplayName => item != null ? item.DisplayName : sentName.Value;
    public float HoldDuration => holdDuration;

    public ItemRarity Rarity => item != null ? item.Definition.rarity : sentRarity.Value;

    private void Awake()
    {
        landing.OnChange += (previous, next, asServer) =>
        {
            if (!asServer) BeginPop(next);
        };
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsServerInitialized && landing.Value != Vector3.zero) BeginPop(landing.Value);
    }

    public static bool Spawn(Vector3 originPosition, Vector3 scatterOffset, Item item,
                             Transform ignoreRoot = null)
    {
        if (item == null) return false;

        NetworkManager networkManager = InstanceFinder.NetworkManager;
        if (networkManager == null || !networkManager.IsServerStarted)
        {
            Debug.LogWarning($"GroundItem: only the server can drop {item.DisplayName}.");
            return false;
        }

        GameObject groundPrefab = item.Definition.groundPrefab;
        if (groundPrefab == null)
        {
            Debug.LogWarning($"GroundItem: '{item.Definition.name}' has no groundPrefab, so it cannot be dropped. " +
                             "Assign one on the item asset.", item.Definition);
            return false;
        }

        GameObject instance = Instantiate(groundPrefab);
        GroundItem drop = instance.GetComponent<GroundItem>();
        if (drop == null)
        {
            Debug.LogWarning($"GroundItem: the ground prefab for '{item.Definition.name}' has no GroundItem component, " +
                             "so nothing could pick it up.", groundPrefab);
            Destroy(instance);
            return false;
        }

        drop.name = $"GroundItem ({item.DisplayName})";
        drop.item = item;
        drop.PlaceAtOrigin(originPosition, scatterOffset, ignoreRoot);

        networkManager.ServerManager.Spawn(instance);

        drop.sentName.Value = item.DisplayName;
        drop.sentRarity.Value = item.Definition.rarity;
        drop.landing.Value = drop.targetPosition;

        return true;
    }

    private void PlaceAtOrigin(Vector3 originPosition, Vector3 scatterOffset, Transform ignoreRoot)
    {
        Vector3 landingSpot = GroundSnap.Find(originPosition + scatterOffset, ignoreRoot);

        transform.position = originPosition + Vector3.up * PopOriginHeight;
        BeginPop(landingSpot + Vector3.up * groundedHeight);
    }

    private void BeginPop(Vector3 destination)
    {
        startPosition = transform.position;
        targetPosition = destination;
        popTimer = 0.0f;
        popping = true;
    }

    private void OnEnable()
    {
        GroundItemLabels.Register(this);
    }

    private void OnDisable()
    {
        GroundItemLabels.Unregister(this);
    }

    private void Update()
    {
        if (IsServerInitialized) TickHold();

        if (!popping || popTimer >= PopDuration) return;

        popTimer += Time.deltaTime;
        float t = Mathf.Clamp01(popTimer / PopDuration);
        Vector3 position = Vector3.Lerp(startPosition, targetPosition, t);
        position.y += Mathf.Sin(t * Mathf.PI) * PopArcHeight;
        transform.position = position;
    }

    public bool IsInRange(Transform interactor)
    {
        if (interactor == null) return false;

        return (transform.position - interactor.position).sqrMagnitude <= interactRange * interactRange;
    }

    public void BeginHold(GameObject interactor) => ServerBeginHold();

    public void CancelHold(GameObject interactor) => ServerCancelHold();

    [ServerRpc(RequireOwnership = false)]
    private void ServerBeginHold(NetworkConnection sender = null)
    {
        if (holder != null) return;

        GameObject interactor = BodyOf(sender);
        if (interactor == null || IsDeadBody(interactor) || !IsInRange(interactor.transform)) return;

        holder = interactor;
        holdElapsed = 0.0f;

        if (holdDuration <= 0.0f) CompleteHold();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerCancelHold(NetworkConnection sender = null)
    {
        if (holder != BodyOf(sender)) return;

        holder = null;
        holdElapsed = 0.0f;
    }

    private static GameObject BodyOf(NetworkConnection connection)
    {
        if (connection == null) return null;

        NetworkObject body = connection.FirstObject;
        return body != null ? body.gameObject : null;
    }

    private static bool IsDeadBody(GameObject body) => body.TryGetComponent(out Health health) && health.IsDead;

    private void TickHold()
    {
        if (holder == null) return;

        if (IsDeadBody(holder) || !IsInRange(holder.transform))
        {
            holder = null;
            holdElapsed = 0.0f;
            return;
        }

        holdElapsed += Time.deltaTime;
        if (holdElapsed >= holdDuration) CompleteHold();
    }

    private void CompleteHold()
    {
        GameObject interactor = holder;

        holder = null;
        holdElapsed = 0.0f;

        Collect(interactor);
    }

    public void SetHighlighted(bool highlighted)
    {
    }

    private void Collect(GameObject interactor)
    {
        if (interactor == null) return;

        Inventory inventory = interactor.GetComponent<Inventory>();
        if (inventory == null || !inventory.TryAddItem(item))
        {
            Debug.Log($"{name}: bag full, {DisplayName} left on the ground.");
            return;
        }

        InstanceFinder.ServerManager.Despawn(gameObject);
    }
}
