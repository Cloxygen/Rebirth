using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine;

public class ClientLogin : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;

    private string accountName;
    private string password;

    public string RefusalReason { get; private set; }

    public void SetCredentials(string accountName, string password)
    {
        this.accountName = accountName;
        this.password = password;
    }

    private void OnEnable()
    {
        networkManager.ClientManager.OnClientConnectionState += SendLoginWhenStarted;
        networkManager.ClientManager.RegisterBroadcast<LoginRefused>(KeepRefusal);
    }

    private void OnDisable()
    {
        if (networkManager == null) return;
        networkManager.ClientManager.OnClientConnectionState -= SendLoginWhenStarted;
        networkManager.ClientManager.UnregisterBroadcast<LoginRefused>(KeepRefusal);
    }

    private void SendLoginWhenStarted(ClientConnectionStateArgs connectionChange)
    {
        if (connectionChange.ConnectionState != LocalConnectionState.Started) return;
        RefusalReason = null;
        networkManager.ClientManager.Broadcast(new LoginRequest { Name = accountName, Password = password });
    }

    private void KeepRefusal(LoginRefused message, Channel channel)
    {
        RefusalReason = message.Reason;
    }
}
