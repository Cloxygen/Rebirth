using FishNet;
using FishNet.Managing.Client;
using FishNet.Object;
using UnityEngine;

public static class LocalPlayer
{
    public static NetworkObject Object
    {
        get
        {
            ClientManager clientManager = InstanceFinder.ClientManager;
            return clientManager != null ? clientManager.Connection.FirstObject : null;
        }
    }

    public static Transform Body
    {
        get
        {
            NetworkObject owned = Object;
            return owned != null ? owned.transform : null;
        }
    }

    public static T Get<T>() where T : Component
    {
        NetworkObject owned = Object;
        return owned != null ? owned.GetComponent<T>() : null;
    }
}
