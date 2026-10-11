using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[DefaultExecutionOrder(-50)]
public class VisionField : MonoBehaviour
{
    public static VisionField Instance { get; private set; }

    [Header("Radius (world units, measured on XZ)")]
    [SerializeField] private float visionRadius = 10.7f;

    [SerializeField] private float fadeStartRadius = 7.5f;

    [SerializeField] private float fadeBand = 4.0f;

    [SerializeField] private float networkPadding = 3.5f;

    [Range(0.0f, 1.0f)]
    [SerializeField] private float dropMargin = 0.15f;

    [SerializeField] private float rebuildDistance = 1.0f;

    [Header("Master")]
    [SerializeField] private bool visionEnabled = true;

    private static readonly int CenterId = Shader.PropertyToID("_VisionCenter");
    private static readonly int ParamsId = Shader.PropertyToID("_VisionParams");
    private static readonly int EditorBypassId = Shader.PropertyToID("_VisionEditorBypass");

    private readonly List<VisionHidden> registered = new List<VisionHidden>();
    private NetworkObject networkObject;
    private Vector3 center;
    private Vector3 lastRebuildPosition;

    public Vector3 Center => center;
    public bool VisionEnabled => visionEnabled;

    public Color FogTint { get; set; } = Color.black;

    public float HideRadius => visionRadius;

    public bool CanSee(Vector3 worldPosition)
    {
        if (!visionEnabled) return true;

        float dx = worldPosition.x - center.x;
        float dz = worldPosition.z - center.z;
        float hide = HideRadius;
        return dx * dx + dz * dz <= hide * hide;
    }

    public static bool IsVisible(Vector3 worldPosition)
    {
        VisionField field = Instance;
        return field == null || field.CanSee(worldPosition);
    }

    public float SendRadius => visionRadius + networkPadding;

    public bool CanBeSent(Vector3 worldPosition, bool alreadySent)
    {
        if (!visionEnabled) return true;

        float radius = visionRadius + networkPadding;
        if (alreadySent) radius *= 1.0f + dropMargin;

        float dx = worldPosition.x - transform.position.x;
        float dz = worldPosition.z - transform.position.z;
        return dx * dx + dz * dz <= radius * radius;
    }

    internal void Register(VisionHidden hidden)
    {
        if (!registered.Contains(hidden)) registered.Add(hidden);
    }

    internal void Unregister(VisionHidden hidden)
    {
        registered.Remove(hidden);
    }

    private void OnEnable()
    {
        networkObject = GetComponentInParent<NetworkObject>();
        ResolveCenter();
        PushGlobals();

        AdoptExisting();

#if UNITY_EDITOR
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
#endif
    }

#if UNITY_EDITOR
    private static void OnBeginCameraRendering(ScriptableRenderContext context, Camera renderingCamera)
    {
        bool bypass = renderingCamera.cameraType == CameraType.SceneView
                   || renderingCamera.cameraType == CameraType.Preview;
        Shader.SetGlobalFloat(EditorBypassId, bypass ? 1.0f : 0.0f);
    }
#endif

    private void OnDisable()
    {
        if (Instance == this) Instance = null;

#if UNITY_EDITOR
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        Shader.SetGlobalFloat(EditorBypassId, 0.0f);
#endif

        PushClearedGlobals();
        for (int i = 0; i < registered.Count; i++)
        {
            if (registered[i] != null) registered[i].ApplyFade(0.0f, FogTint);
        }
    }

    private void OnValidate()
    {
        visionRadius = Mathf.Max(visionRadius, 0.0f);
        fadeStartRadius = Mathf.Clamp(fadeStartRadius, 0.0f, visionRadius);
        fadeBand = Mathf.Clamp(fadeBand, 0.0f, visionRadius);
        if (isActiveAndEnabled) PushGlobals();
    }

    private void LateUpdate()
    {
        RebuildObserversIfMoved();

        if (!IsLocalPlayers)
        {
            if (Instance == this) Instance = null;
            return;
        }

        if (Instance != this)
        {
            Instance = this;
            AdoptExisting();
        }

        ResolveCenter();
        PushGlobals();
        ApplyHiding();
    }

    private bool IsLocalPlayers => !Application.isPlaying || networkObject == null || networkObject.IsOwner;

    private void ResolveCenter()
    {
        center = transform.position;
    }

    private void RebuildObserversIfMoved()
    {
        if (networkObject == null || !networkObject.IsServerInitialized) return;
        if ((transform.position - lastRebuildPosition).sqrMagnitude < rebuildDistance * rebuildDistance) return;

        lastRebuildPosition = transform.position;

        networkObject.ServerManager.Objects.RebuildObservers(networkObject.Owner);
        networkObject.ServerManager.Objects.RebuildObservers(networkObject);
    }

    private void AdoptExisting()
    {
        VisionHidden[] existing = FindObjectsByType<VisionHidden>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < existing.Length; i++) Register(existing[i]);
    }

    private void PushGlobals()
    {
        if (!visionEnabled)
        {
            PushClearedGlobals();
            return;
        }

        float radius = Mathf.Max(visionRadius, 0.001f);
        float fadeStart = Mathf.Min(fadeStartRadius, radius - 0.001f);

        Shader.SetGlobalVector(CenterId, new Vector4(center.x, center.y, center.z, 0.0f));
        Shader.SetGlobalVector(ParamsId, new Vector4(fadeStart, radius, 1.0f, 0.0f));
    }

    private void PushClearedGlobals()
    {
        Shader.SetGlobalVector(ParamsId, new Vector4(0.0f, 1.0f, 0.0f, 0.0f));
    }

    private void ApplyHiding()
    {

        float hide = HideRadius;
        float band = Mathf.Max(fadeBand, 0.0001f);
        float fadeBegins = Mathf.Max(hide - band, 0.0f);

        for (int i = registered.Count - 1; i >= 0; i--)
        {
            VisionHidden hidden = registered[i];
            if (hidden == null)
            {
                registered.RemoveAt(i);
                continue;
            }

            if (!visionEnabled)
            {
                hidden.ApplyFade(0.0f, FogTint);
                continue;
            }

            Vector3 p = hidden.SamplePosition;
            float dx = p.x - center.x;
            float dz = p.z - center.z;
            float distance = Mathf.Sqrt(dx * dx + dz * dz);

            hidden.ApplyFade(Mathf.InverseLerp(fadeBegins, hide, distance), FogTint);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 c = transform.position;

        Gizmos.color = new Color(1.0f, 0.85f, 0.4f, 0.9f);
        DrawDisc(c, fadeStartRadius);
        Gizmos.color = new Color(0.4f, 0.7f, 1.0f, 0.6f);
        DrawDisc(c, Mathf.Max(visionRadius - fadeBand, 0.0f));
        Gizmos.color = new Color(1.0f, 0.5f, 0.2f, 0.9f);
        DrawDisc(c, visionRadius);
    }

    private static void DrawDisc(Vector3 center, float radius)
    {
        const int Segments = 64;
        Vector3 previous = center + new Vector3(radius, 0.0f, 0.0f);
        for (int i = 1; i <= Segments; i++)
        {
            float a = i / (float)Segments * Mathf.PI * 2.0f;
            Vector3 next = center + new Vector3(Mathf.Cos(a) * radius, 0.0f, Mathf.Sin(a) * radius);
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }
}
