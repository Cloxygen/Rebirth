using FishNet;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class EssencePickup : NetworkBehaviour
{
    private enum State { Popping, Settled, Drawn }

    [Header("Drop")]
    [SerializeField] private float popOriginHeight = 0.3f;
    [SerializeField] private float popArcHeight = 0.6f;
    [SerializeField] private float popDuration = 0.4f;

    [Header("Rest")]
    [SerializeField] private float hoverHeight = 0.4f;
    [SerializeField] private float bobHeight = 0.05f;
    [SerializeField] private float bobSpeed = 2.2f;

    [Header("Slurp")]
    [SerializeField] private float drawRadius = 2.6f;
    [SerializeField] private float drawAcceleration = 24.0f;
    [SerializeField] private float maxDrawSpeed = 12.0f;
    [SerializeField] private float drawDrag = 2.0f;
    [SerializeField] private float recoilSpeed = 2.5f;
    [SerializeField] private float collectDistance = 0.4f;

    [Header("Light")]
    [SerializeField] private float drawLightBoost = 1.6f;
    [Range(0.0f, 1.0f)] [SerializeField] private float bobLightPulse = 0.3f;

    [Header("Wiring")]
    [SerializeField] private Light moteLight;
    [SerializeField] private ParticleSystem trail;
    [SerializeField] private ParticleSystem absorbFlash;

    private readonly SyncVar<Vector3> landing = new SyncVar<Vector3>();

    private int amount;
    private State state;
    private Vector3 popStart;
    private Vector3 restPosition;
    private float popTimer;
    private float bobPhase;
    private float baseLightIntensity;
    private Vector3 drawVelocity;
    private Wallet target;

    public static void Spawn(GameObject motePrefab, Vector3 originPosition, Vector3 scatterOffset, int amount,
                             Transform ignoreRoot = null)
    {
        if (amount <= 0) return;

        NetworkManager networkManager = InstanceFinder.NetworkManager;
        if (networkManager == null || !networkManager.IsServerStarted)
        {
            Debug.LogWarning("EssencePickup: only the server drops essence; this mote was dropped.");
            return;
        }

        if (motePrefab == null)
        {
            Debug.LogError("EssencePickup: the loot table has no essence mote prefab. " +
                           "Rebuild it with Tools > Rebirth > Build Loot Assets.");
            return;
        }

        GameObject moteObject = Instantiate(motePrefab);
        EssencePickup mote = moteObject.GetComponent<EssencePickup>();
        if (mote == null)
        {
            Debug.LogWarning("EssencePickup: the essence mote prefab has no EssencePickup on it.", motePrefab);
            Destroy(moteObject);
            return;
        }

        mote.Initialize(originPosition, scatterOffset, amount, ignoreRoot);

        networkManager.ServerManager.Spawn(moteObject);
    }

    private void Awake()
    {
        baseLightIntensity = moteLight.intensity;
    }

    private void Initialize(Vector3 originPosition, Vector3 scatterOffset, int amount, Transform ignoreRoot)
    {
        this.amount = amount;

        transform.position = originPosition + Vector3.up * popOriginHeight;
        popStart = transform.position;
        restPosition = GroundSnap.Find(originPosition + scatterOffset, ignoreRoot) + Vector3.up * hoverHeight;
        bobPhase = Random.value * Mathf.PI * 2.0f;

        landing.Value = restPosition;
    }

    public override void OnStartClient()
    {
        if (IsServerInitialized) return;

        restPosition = landing.Value;
        popStart = transform.position;
        bobPhase = Random.value * Mathf.PI * 2.0f;
    }

    private void Update()
    {
        switch (state)
        {
            case State.Popping: Pop(); break;
            case State.Settled: Settle(); break;
            case State.Drawn: Draw(); break;
        }
    }

    private void Pop()
    {
        popTimer += Time.deltaTime;
        float t = Mathf.Clamp01(popTimer / popDuration);

        Vector3 position = Vector3.Lerp(popStart, restPosition, t);
        position.y += Mathf.Sin(t * Mathf.PI) * popArcHeight;
        transform.position = position;

        if (t >= 1.0f) state = State.Settled;
    }

    private void Settle()
    {
        bobPhase += Time.deltaTime * bobSpeed;
        transform.position = restPosition + Vector3.up * (Mathf.Sin(bobPhase) * bobHeight);

        float pulse = (Mathf.Sin(bobPhase) + 1.0f) * 0.5f;
        moteLight.intensity = baseLightIntensity * Mathf.Lerp(1.0f - bobLightPulse, 1.0f, pulse);

        target = Wallet.Nearest(transform.position, drawRadius);
        if (target == null) return;

        Vector3 toWallet = target.transform.position - transform.position;

        state = State.Drawn;
        drawVelocity = (Vector3.up * 0.6f - toWallet.normalized).normalized * recoilSpeed;
        moteLight.intensity = baseLightIntensity * drawLightBoost;
    }

    private void Draw()
    {
        if (target == null || !target.CanTakeEssence)
        {
            restPosition = transform.position;
            state = State.Settled;
            return;
        }

        Vector3 toWallet = target.transform.position - transform.position;
        if (toWallet.sqrMagnitude <= collectDistance * collectDistance)
        {
            if (IsServerInitialized) Collect(target);
            return;
        }

        float deltaTime = Time.deltaTime;

        drawVelocity += toWallet.normalized * (drawAcceleration * deltaTime);
        drawVelocity -= drawVelocity * Mathf.Min(1.0f, drawDrag * deltaTime);
        drawVelocity = Vector3.ClampMagnitude(drawVelocity, maxDrawSpeed);

        transform.position += drawVelocity * deltaTime;
    }

    private void Collect(Wallet wallet)
    {
        wallet.AddEssence(amount);
        Swallow();

        NetworkManager networkManager = InstanceFinder.NetworkManager;
        if (networkManager != null) networkManager.ServerManager.Despawn(gameObject);
        else Destroy(gameObject);
    }

    public override void OnStopClient()
    {
        if (IsServerInitialized) return;

        Swallow();
    }

    private void Swallow()
    {
        trail.transform.SetParent(null, true);
        trail.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        absorbFlash.transform.SetParent(null, true);
        absorbFlash.gameObject.SetActive(true);
        absorbFlash.Play();
    }
}
