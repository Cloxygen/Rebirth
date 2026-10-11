using FishNet;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;
using UnityEngine.InputSystem;

public class ModifierTestbed : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        public string label = "Modifier";
        public Key key = Key.Digit1;
        public ModifierSet modifiers = new ModifierSet();
        [System.NonSerialized] public bool active;
    }

    private struct Request : IBroadcast
    {
        public int EntryIndex;
        public bool On;
    }

    [SerializeField] private bool showOverlay = true;
    [SerializeField]
    private Entry[] entries = new Entry[]
    {
        new Entry { label = "+2 arrows", key = Key.Digit1, modifiers = new ModifierSet { extraProjectiles = 2 } },
        new Entry { label = "Pierce 2",  key = Key.Digit2, modifiers = new ModifierSet { pierceCount = 2 } },
        new Entry { label = "Blast 1.5", key = Key.Digit3, modifiers = new ModifierSet { addedBlastRadius = 1.5f } },
    };

    private Modifiers playerModifiers;
    private bool listening;

    private void Update()
    {
        Listen();
        Rebind();

        if (playerModifiers == null || Keyboard.current == null || entries == null) return;

        for (int index = 0; index < entries.Length; index++)
        {
            Entry entry = entries[index];
            if (entry == null || !Keyboard.current[entry.key].wasPressedThisFrame) continue;

            entry.active = !entry.active;
            Apply(index, entry);

            Debug.Log($"{entry.label} {(entry.active ? "on" : "off")}", this);
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

    private void Apply(int index, Entry entry)
    {
        if (entry.active) playerModifiers.Register(entry.modifiers);
        else playerModifiers.Unregister(entry.modifiers);

        if (InstanceFinder.ClientManager != null)
        {
            InstanceFinder.ClientManager.Broadcast(new Request { EntryIndex = index, On = entry.active });
        }
    }

    private void HandleRequest(NetworkConnection sender, Request request, Channel channel)
    {
        if (!Debug.isDebugBuild || entries == null) return;
        if (request.EntryIndex < 0 || request.EntryIndex >= entries.Length) return;

        NetworkObject body = sender != null ? sender.FirstObject : null;
        Modifiers modifiers = body != null ? body.GetComponent<Modifiers>() : null;
        ModifierSet set = entries[request.EntryIndex]?.modifiers;
        if (modifiers == null || set == null) return;

        if (request.On) modifiers.Register(set);
        else modifiers.Unregister(set);
    }

    private void Rebind()
    {
        Modifiers current = LocalPlayer.Get<Modifiers>();
        if (current == playerModifiers) return;

        playerModifiers = current;
        if (playerModifiers == null || entries == null) return;

        for (int index = 0; index < entries.Length; index++)
        {
            Entry entry = entries[index];
            if (entry != null && entry.active) Apply(index, entry);
        }
    }

    private const float PanelWidth = 320.0f;

    private bool collapsed;

    private void OnGUI()
    {
        if (!showOverlay || playerModifiers == null || entries == null) return;

        float height = collapsed
            ? DebugPanel.TitleBarHeight
            : 24.0f + entries.Length * 20.0f + 90.0f;

        GUILayout.BeginArea(DebugPanel.Claim(PanelWidth, height), GUI.skin.box);

        if (!DebugPanel.DrawTitleBar("Modifier testbed", ref collapsed))
        {
            GUILayout.EndArea();
            return;
        }

        foreach (Entry entry in entries)
        {
            if (entry == null) continue;
            GUILayout.Label($"[{entry.key.ToString().Replace("Digit", "")}] {entry.label}  {(entry.active ? "ON" : "off")}");
        }

        GUILayout.Space(6);
        GUILayout.Label($"extra projectiles  {playerModifiers.ExtraProjectiles}");
        GUILayout.Label($"pierce             {playerModifiers.PierceCount}");
        GUILayout.Label($"added blast        {playerModifiers.AddedBlastRadius:0.##}");
        GUILayout.Label($"on-hit effects     {playerModifiers.OnHitEffects.Count}");
        GUILayout.EndArea();
    }
}
