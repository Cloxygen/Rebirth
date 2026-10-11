using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class LoginBox : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private ClientLogin clientLogin;

    private VisualElement loginPanel;
    private TextField nameField;
    private TextField passwordField;
    private Label refusalMessage;
    private Button hostButton;
    private Button joinButton;
    private LocalConnectionState clientState = LocalConnectionState.Stopped;
    private bool holdsTextEntry;

    private void OnEnable()
    {
        FindElements();
        hostButton.clicked += HostAndJoin;
        joinButton.clicked += Join;
        networkManager.ClientManager.OnClientConnectionState += TrackClientState;
    }

    private void OnDisable()
    {
        if (hostButton != null) hostButton.clicked -= HostAndJoin;
        if (joinButton != null) joinButton.clicked -= Join;
        if (networkManager != null) networkManager.ClientManager.OnClientConnectionState -= TrackClientState;
        ReleaseTextEntry();
    }

    private void Update()
    {
        bool panelShown = !Application.isEditor && clientState == LocalConnectionState.Stopped;
        loginPanel.style.display = panelShown ? DisplayStyle.Flex : DisplayStyle.None;
        if (panelShown) HoldTextEntry();
        else ReleaseTextEntry();
        refusalMessage.text = clientLogin.RefusalReason;
    }

    private void FindElements()
    {
        VisualElement root = document.rootVisualElement;
        FindFields(root);
        FindButtons(root);
    }

    private void FindFields(VisualElement root)
    {
        loginPanel = root.Q<VisualElement>("login");
        nameField = root.Q<TextField>("login-name");
        passwordField = root.Q<TextField>("login-password");
        refusalMessage = root.Q<Label>("login-reason");
    }

    private void FindButtons(VisualElement root)
    {
        hostButton = root.Q<Button>("login-host");
        joinButton = root.Q<Button>("login-join");
    }

    private void HoldTextEntry()
    {
        holdsTextEntry = true;
        UITextEntry.Active = true;
    }

    private void ReleaseTextEntry()
    {
        if (!holdsTextEntry) return;
        holdsTextEntry = false;
        UITextEntry.Active = false;
    }

    private void TrackClientState(ClientConnectionStateArgs connectionChange)
    {
        clientState = connectionChange.ConnectionState;
    }

    private void HostAndJoin()
    {
        if (!networkManager.ServerManager.Started) networkManager.ServerManager.StartConnection();
        Join();
    }

    private void Join()
    {
        clientLogin.SetCredentials(nameField.value, passwordField.value);
        networkManager.ClientManager.StartConnection();
    }
}
