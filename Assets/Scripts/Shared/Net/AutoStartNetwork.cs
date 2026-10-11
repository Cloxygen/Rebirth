using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine;
#if UNITY_EDITOR
using Unity.Multiplayer.PlayMode;
#endif

[RequireComponent(typeof(NetworkManager))]
public class AutoStartNetwork : MonoBehaviour
{
    [SerializeField] private float retryInterval = 0.5f;
    [SerializeField] private ClientLogin clientLogin;

    private NetworkManager networkManager;
    private LocalConnectionState clientState = LocalConnectionState.Stopped;
    private float retryTimer;

    private void Awake()
    {
        networkManager = GetComponent<NetworkManager>();
    }

#if UNITY_EDITOR
    private const string DevPassword = "dev-password";

    private void OnEnable()
    {
        networkManager.ClientManager.OnClientConnectionState += HandleClientState;
    }

    private void OnDisable()
    {
        networkManager.ClientManager.OnClientConnectionState -= HandleClientState;
    }

    private void Start()
    {
        clientLogin.SetCredentials(CurrentPlayer.IsMainEditor ? "host" : "player2", DevPassword);

        if (CurrentPlayer.IsMainEditor) networkManager.ServerManager.StartConnection();

        networkManager.ClientManager.StartConnection();
        retryTimer = retryInterval;
    }

    private void Update()
    {
        if (clientState != LocalConnectionState.Stopped) return;

        retryTimer -= Time.unscaledDeltaTime;
        if (retryTimer > 0.0f) return;
        retryTimer = retryInterval;

        networkManager.ClientManager.StartConnection();
    }

    private void HandleClientState(ClientConnectionStateArgs args)
    {
        clientState = args.ConnectionState;
    }
#endif
}
