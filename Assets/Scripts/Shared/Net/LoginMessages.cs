using FishNet.Broadcast;

public struct LoginRequest : IBroadcast
{
    public string Name;
    public string Password;
}

public struct LoginRefused : IBroadcast
{
    public string Reason;
}
