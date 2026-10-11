using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using FishNet.Authenticating;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Server;
using FishNet.Transporting;

public class LoginHandler : Authenticator
{
    public override event Action<NetworkConnection, bool> OnAuthenticationResult;

    public ServerDatabase Database { get; private set; }

    private const int LongestNameLength = 32, HashRounds = 100000, SaltBytes = 16, HashBytes = 32;

    private readonly Dictionary<int, int> accountsByClient = new Dictionary<int, int>();

    private ServerManager ServerManager => NetworkManager.ServerManager;

    public override void InitializeOnce(NetworkManager networkManager)
    {
        base.InitializeOnce(networkManager);
        ServerManager.RegisterBroadcast<LoginRequest>(AnswerLoginRequest, false);
        ServerManager.OnServerConnectionState += OpenOrCloseDatabase;
        ServerManager.OnRemoteConnectionState += ForgetDisconnectedClient;
    }

    private void OnDestroy()
    {        Database?.Close();
    }

    private void OpenOrCloseDatabase(ServerConnectionStateArgs serverState)
    {
        if (serverState.ConnectionState == LocalConnectionState.Started)
            Database ??= new ServerDatabase(ServerDatabase.DefaultPath);
        else if (serverState.ConnectionState == LocalConnectionState.Stopped)
            CloseDatabase();
    }

    private void CloseDatabase()
    {        Database?.Close();
        Database = null;
        accountsByClient.Clear();
    }

    private void ForgetDisconnectedClient(NetworkConnection connection, RemoteConnectionStateArgs clientState)
    {
        if (clientState.ConnectionState == RemoteConnectionState.Stopped)
            accountsByClient.Remove(connection.ClientId);
    }

    private void AnswerLoginRequest(NetworkConnection connection, LoginRequest request, Channel channel)
    {
        if (connection.IsAuthenticated)
            connection.Disconnect(true);
        else if (!connection.Disconnecting)
            CheckTypedLogin(connection, TrimName(request.Name), request.Password);
    }

    private void CheckTypedLogin(NetworkConnection connection, string accountName, string password)
    {
        string problem = FindTypingProblem(accountName, password);
        if (problem != null)
            Refuse(connection, problem);
        else
            CheckPasswordAndSignIn(connection, FindOrCreateAccount(accountName, password), password);
    }

    private void CheckPasswordAndSignIn(NetworkConnection connection, AccountRow account, string password)
    {
        string problem = FindSignInProblem(account, password);
        if (problem != null)
            Refuse(connection, problem);
        else
            Accept(connection, account);
    }

    private static string TrimName(string typedName)
    {
        return typedName?.Trim() ?? "";
    }

    private static string FindTypingProblem(string accountName, string password)
    {
        if (accountName.Length == 0 || string.IsNullOrEmpty(password))
            return "Enter a name and a password.";
        if (accountName.Length > LongestNameLength)
            return "That name is too long.";
        return null;
    }

    private AccountRow FindOrCreateAccount(string accountName, string password)
    {
        return Database.FindAccount(accountName) ?? CreateAccount(accountName, password);
    }

    private AccountRow CreateAccount(string accountName, string password)
    {
        var account = new AccountRow { Name = accountName, CreatedUtc = DateTime.UtcNow };
        StorePasswordHash(account, password);
        Database.AddAccount(account);
        return account;
    }

    private string FindSignInProblem(AccountRow account, string password)
    {
        if (!PasswordMatches(account, password))
            return "Wrong password.";
        if (accountsByClient.ContainsValue(account.Id))
            return "That account is already logged in.";
        return null;
    }

    private void Accept(NetworkConnection connection, AccountRow account)
    {
        accountsByClient[connection.ClientId] = account.Id;
        connection.CustomData = CharacterToPlay(account);
        OnAuthenticationResult?.Invoke(connection, true);
    }

    private void Refuse(NetworkConnection connection, string reason)
    {
        ServerManager.Broadcast(connection, new LoginRefused { Reason = reason }, false);
        OnAuthenticationResult?.Invoke(connection, false);
    }

    private int CharacterToPlay(AccountRow account)
    {
        List<CharacterRow> characters = Database.CharactersOf(account.Id);
        if (characters.Count > 0)
            return characters[0].Id;
        return Database.AddCharacter(account.Id, account.Name).Id;
    }

    private static void StorePasswordHash(AccountRow account, string password)
    {
        account.PasswordSalt = new byte[SaltBytes];
        using (var random = RandomNumberGenerator.Create())
            random.GetBytes(account.PasswordSalt);
        account.HashRounds = HashRounds;
        account.PasswordHash = HashPassword(password, account.PasswordSalt, HashRounds);
    }

    private static bool PasswordMatches(AccountRow account, string password)
    {
        byte[] attemptHash = HashPassword(password, account.PasswordSalt, account.HashRounds);
        return CryptographicOperations.FixedTimeEquals(attemptHash, account.PasswordHash);
    }

    private static byte[] HashPassword(string password, byte[] salt, int rounds)
    {
        using (var hasher = new Rfc2898DeriveBytes(password, salt, rounds, HashAlgorithmName.SHA256))
            return hasher.GetBytes(HashBytes);
    }
}
