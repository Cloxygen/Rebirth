using FishNet;
using FishNet.Component.Spawning;
using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    [SerializeField] private float secondsDeadBeforeRespawn = 3.0f;
    [SerializeField] private Health health;
    [SerializeField] private PlayerMovement playerMovement;

    private Transform spawnPoint;
    private float secondsUntilRespawn;

    private void Awake()
    {
        spawnPoint = FindSpawnPoint();
    }

    public void AdvanceCountdown(float secondsElapsed)
    {
        if (health.IsDead) CountDownToRespawn(secondsElapsed);
        else secondsUntilRespawn = secondsDeadBeforeRespawn;
    }

    private void CountDownToRespawn(float secondsElapsed)
    {
        secondsUntilRespawn -= secondsElapsed;
        if (secondsUntilRespawn <= 0.0f) Respawn();
    }

    private void Respawn()
    {
        if (spawnPoint != null) playerMovement.Teleport(spawnPoint.position, spawnPoint.eulerAngles.y);
        else Debug.LogWarning($"{name} has no spawn point to return to; reviving where it fell.", this);

        health.Revive();
    }

    private static Transform FindSpawnPoint()
    {
        if (InstanceFinder.NetworkManager == null) return null;

        PlayerSpawner spawner = InstanceFinder.NetworkManager.GetComponent<PlayerSpawner>();
        if (spawner == null || spawner.Spawns.Length == 0) return null;

        return spawner.Spawns[0];
    }
}
