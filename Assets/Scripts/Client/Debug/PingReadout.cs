using System.Reflection;
using FishNet;
using FishNet.Managing;
using FishNet.Transporting.Tugboat;
using LiteNetLib;
using UnityEngine;

public class PingReadout : MonoBehaviour
{
    public static bool Show = true;

    private const int FontSize = 18;
    private const float Inset = 10f;

    private static PingReadout instance;
    private GUIStyle style;

    private static readonly FieldInfo NetManagerField =
        typeof(CommonSocket).GetField("NetManager", BindingFlags.NonPublic | BindingFlags.Instance);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Spawn()
    {
        if (instance != null) return;
        GameObject go = new GameObject("PingReadout");
        DontDestroyOnLoad(go);
        go.AddComponent<PingReadout>();
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(this); return; }
        instance = this;
    }

    private static NetPeer ServerPeer()
    {
        NetworkManager manager = InstanceFinder.NetworkManager;
        if (manager == null || !manager.ClientManager.Started) return null;
        if (manager.TransportManager.Transport is not Tugboat tugboat) return null;
        if (tugboat.ClientSocket == null || NetManagerField == null) return null;
        NetManager net = NetManagerField.GetValue(tugboat.ClientSocket) as NetManager;
        return net?.FirstPeer;
    }

    private void OnGUI()
    {
        if (!Show) return;

        style ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = FontSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperRight
        };

        string text;
        Color colour;

        NetPeer peer = ServerPeer();
        if (peer == null)
        {
            text = "ping: offline";
            colour = Color.grey;
        }
        else
        {
            int rtt = peer.RoundTripTime;
            text = InstanceFinder.IsHostStarted ? $"ping: {rtt} ms (host)" : $"ping: {rtt} ms";
            colour = rtt < 80 ? Color.green : rtt < 150 ? Color.yellow : Color.red;
        }

        float width = 300f;
        Rect rect = new Rect(Screen.width - width - Inset, Inset, width, FontSize * 2f);
        style.normal.textColor = Color.black;
        GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, style);
        style.normal.textColor = colour;
        GUI.Label(rect, text, style);
    }
}
