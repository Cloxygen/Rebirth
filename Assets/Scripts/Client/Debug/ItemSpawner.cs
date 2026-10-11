using FishNet;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;
using UnityEngine.InputSystem;

public class ItemSpawner : MonoBehaviour
{
    [SerializeField] private ItemDatabase database;

    [SerializeField] private Key toggleKey = Key.F2;
    [SerializeField] private Key invulnerabilityKey = Key.F3;
    [SerializeField] private bool showOverlay;

    [SerializeField] private float dropScatter = 0.6f;

    private struct Request : IBroadcast
    {
        public const byte Invulnerable = 0;
        public const byte Bag = 1;
        public const byte Drop = 2;

        public byte Kind;
        public int ItemIndex;
        public bool On;
    }

    private Vector2 scroll;
    private ItemDefinition pendingDrop;
    private ItemDefinition pendingBag;
    private bool pendingInvulnerabilityToggle;
    private bool listening;

    private void Awake()
    {
        swingCount = 0;
        contactCount = 0;
        confirmCount = 0;
        lastConfirmMilliseconds = -1.0f;
        lastConfirmMissed = false;
        StopWaiting();
    }

    private void Update()
    {
        Listen();
        SweepHitConfirm();

        if (Keyboard.current != null)
        {
            if (Keyboard.current[toggleKey].wasPressedThisFrame) showOverlay = !showOverlay;
            if (Keyboard.current[invulnerabilityKey].wasPressedThisFrame) pendingInvulnerabilityToggle = true;
        }

        if (pendingInvulnerabilityToggle)
        {
            pendingInvulnerabilityToggle = false;
            Health health = LocalPlayer.Get<Health>();
            if (health != null) Send(new Request { Kind = Request.Invulnerable, On = !health.IsInvulnerable });
        }

        if (pendingDrop != null)
        {
            Send(new Request { Kind = Request.Drop, ItemIndex = IndexOf(pendingDrop) });
            pendingDrop = null;
        }

        if (pendingBag != null)
        {
            Send(new Request { Kind = Request.Bag, ItemIndex = IndexOf(pendingBag) });
            pendingBag = null;
        }
    }

    private void OnDisable()
    {
        if (!listening) return;
        listening = false;

        if (InstanceFinder.ServerManager != null)
            InstanceFinder.ServerManager.UnregisterBroadcast<Request>(HandleRequest);
    }

    private void Listen()
    {
        if (listening || InstanceFinder.ServerManager == null) return;

        InstanceFinder.ServerManager.RegisterBroadcast<Request>(HandleRequest);
        listening = true;
    }

    private static void Send(Request request)
    {
        if (InstanceFinder.ClientManager != null) InstanceFinder.ClientManager.Broadcast(request);
    }

    private void HandleRequest(NetworkConnection sender, Request request, Channel channel)
    {
        if (!Debug.isDebugBuild) return;

        NetworkObject body = sender != null ? sender.FirstObject : null;
        if (body == null) return;

        switch (request.Kind)
        {
            case Request.Invulnerable:
                Health health = body.GetComponent<Health>();
                if (health != null) health.SetInvulnerable(request.On);
                break;

            case Request.Bag:
                Bag(body, ItemAt(request.ItemIndex));
                break;

            case Request.Drop:
                Drop(body, ItemAt(request.ItemIndex));
                break;
        }
    }

    private void Drop(NetworkObject body, ItemDefinition item)
    {
        if (item == null) return;

        Vector2 scatter = Random.insideUnitCircle * dropScatter;
        GroundItem.Spawn(body.transform.position, new Vector3(scatter.x, 0.0f, scatter.y),
                         new Item(item), body.transform);
    }

    private void Bag(NetworkObject body, ItemDefinition item)
    {
        Inventory inventory = item != null ? body.GetComponent<Inventory>() : null;
        if (inventory == null) return;

        if (!inventory.TryAddItem(new Item(item)))
        {
            Debug.Log($"ItemSpawner: no room for {item.name}.", this);
        }
    }

    private ItemDefinition ItemAt(int index)
    {
        if (database == null || index < 0 || index >= database.Items.Count) return null;

        return database.Items[index];
    }

    private int IndexOf(ItemDefinition item)
    {
        if (database == null) return -1;

        for (int index = 0; index < database.Items.Count; index++)
        {
            if (database.Items[index] == item) return index;
        }

        return -1;
    }

    private const float PanelWidth = 340.0f;
    private const float PanelHeight = 480.0f;

    private bool collapsed;

    private void OnGUI()
    {
        if (!showOverlay) return;

        float height = collapsed ? DebugPanel.TitleBarHeight : PanelHeight;

        GUILayout.BeginArea(DebugPanel.Claim(PanelWidth, height), GUI.skin.box);

        if (!DebugPanel.DrawTitleBar($"Item spawner  [{toggleKey}]", ref collapsed))
        {
            GUILayout.EndArea();
            return;
        }

        Health health = LocalPlayer.Get<Health>();
        string invulnerable = health != null && health.IsInvulnerable ? "ON" : "off";
        if (GUILayout.Button($"Invulnerable [{invulnerabilityKey}]  {invulnerable}")) pendingInvulnerabilityToggle = true;
        GUILayout.Label(DescribeHitConfirm());
        GUILayout.Space(6);

        if (database == null)
        {
            GUILayout.Label("No ItemDatabase assigned.");
            GUILayout.EndArea();
            return;
        }

        if (database.Items.Count == 0)
        {
            GUILayout.Label("No items yet. Create > Rebirth > Items > Item.");
        }

        scroll = GUILayout.BeginScrollView(scroll);

        foreach (ItemDefinition item in database.Items)
        {
            if (item == null) continue;

            GUILayout.BeginHorizontal();
            GUILayout.Label(item.name, GUILayout.Width(170));
            if (GUILayout.Button("Drop")) pendingDrop = item;
            if (GUILayout.Button("Bag")) pendingBag = item;
            GUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private const float GiveUpAfterSeconds = 2.0f;

    private static Health waitingOn;
    private static float contactTime;
    private static float lastConfirmMilliseconds = -1.0f;
    private static bool lastConfirmMissed;
    private static int swingCount;
    private static int contactCount;
    private static int confirmCount;

    public static void NoteSwing()
    {
        swingCount++;
    }

    public static void NoteContact(Health target)
    {
        if (target == null) return;

        contactCount++;

        if (waitingOn != null) StopWaiting();

        waitingOn = target;
        contactTime = Time.realtimeSinceStartup;
        target.onDamaged.AddListener(HandleConfirmed);
    }

    private static void SweepHitConfirm()
    {
        if (waitingOn == null || Time.realtimeSinceStartup - contactTime < GiveUpAfterSeconds) return;

        StopWaiting();
        lastConfirmMissed = true;
    }

    private static string DescribeHitConfirm()
    {
        string tally = $"  (swings {swingCount}, contacts {contactCount}, confirms {confirmCount})";

        if (waitingOn != null) return "hit confirm: waiting" + tally;
        if (lastConfirmMissed) return "hit confirm: server never landed it" + tally;
        if (lastConfirmMilliseconds < 0.0f) return "hit confirm: no hits yet" + tally;

        return $"hit confirm: {lastConfirmMilliseconds:0} ms" + tally;
    }

    private static void HandleConfirmed(Damage damage, Vector3 hitPoint)
    {
        lastConfirmMilliseconds = (Time.realtimeSinceStartup - contactTime) * 1000.0f;
        lastConfirmMissed = false;
        confirmCount++;
        StopWaiting();
    }

    private static void StopWaiting()
    {
        if (waitingOn != null) waitingOn.onDamaged.RemoveListener(HandleConfirmed);
        waitingOn = null;
    }
}
