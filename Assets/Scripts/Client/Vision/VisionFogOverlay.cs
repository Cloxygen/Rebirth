using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
[DisallowMultipleComponent]
public class VisionFogOverlay : MonoBehaviour
{
    private const string ShaderName = "Rebirth/VisionFog";

    [Header("Look")]
    [ColorUsage(false)]
    [SerializeField] private Color fogColor = new Color(0.02f, 0.02f, 0.035f, 1.0f);

    [Range(0.0f, 1.0f)]
    [SerializeField] private float maxOpacity = 1.0f;

    [Range(0.0f, 1.0f)]
    [SerializeField] private float desaturation = 0.8f;

    [Range(0.0f, 1.0f)]
    [SerializeField] private float detailRetention = 0.7f;

    [Header("Edge")]
    [Range(0.0f, 8.0f)]
    [SerializeField] private float turbulence = 2.0f;

    [Range(0.0f, 3.0f)]
    [SerializeField] private float turbulenceSpeed = 0.35f;

    [Range(0.01f, 0.5f)]
    [SerializeField] private float turbulenceScale = 0.08f;

    [Range(0.0f, 8.0f)]
    [SerializeField] private float edgeBlur = 2.5f;

    [Header("Debug")]
    [SerializeField] private bool drawFog = true;

    private static readonly int ColorId = Shader.PropertyToID("_VisionColor");
    private static readonly int FogParamsId = Shader.PropertyToID("_VisionFogParams");
    private static readonly int FogNoiseId = Shader.PropertyToID("_VisionFogNoise");

    private Camera fogCamera;
    private Material fogMaterial;
    private Mesh quad;
    private bool warnedNoSceneColor;

    private void OnEnable()
    {
        fogCamera = GetComponent<Camera>();

        UniversalAdditionalCameraData data = fogCamera.GetUniversalAdditionalCameraData();
        if (data != null)
        {
            data.requiresDepthTexture = true;
            data.requiresColorTexture = true;
        }

        warnedNoSceneColor = false;
        PushLookGlobals();

        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;

        if (fogMaterial != null) DestroyImmediate(fogMaterial);
        if (quad != null) DestroyImmediate(quad);
        fogMaterial = null;
        quad = null;
    }

    private void OnValidate()
    {
        if (isActiveAndEnabled) PushLookGlobals();
    }

    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera renderingCamera)
    {
        if (renderingCamera != fogCamera) return;
        if (!drawFog || fogCamera == null) return;
        if (VisionField.Instance == null || !VisionField.Instance.VisionEnabled) return;
        if (!EnsureResources()) return;

        PushLookGlobals();

        RenderParams parameters = new RenderParams(fogMaterial)
        {
            camera = fogCamera,
            layer = gameObject.layer,
            receiveShadows = false,
            shadowCastingMode = ShadowCastingMode.Off,
            worldBounds = new Bounds(Vector3.zero, Vector3.one * 1.0e6f)
        };

        Graphics.RenderMesh(parameters, quad, 0, Matrix4x4.identity);
    }

    private void PushLookGlobals()
    {
        float sceneColor = SceneColorAvailable() ? 1.0f : 0.0f;

        if (sceneColor < 0.5f && !warnedNoSceneColor && (desaturation > 0.0f || detailRetention > 0.0f || edgeBlur > 0.0f))
        {
            warnedNoSceneColor = true;
            Debug.LogWarning(
                $"{nameof(VisionFogOverlay)}: Desaturation, Detail Retention and Edge Blur all read the URP Opaque " +
                "Texture, which this pipeline asset has off (Mobile_RPAsset ships that way). Falling back to flat fog. " +
                "Turbulence is unaffected — it is pure shader math.", this);
        }

        if (VisionField.Instance != null) VisionField.Instance.FogTint = fogColor;

        Shader.SetGlobalColor(ColorId, fogColor);
        Shader.SetGlobalVector(FogParamsId, new Vector4(
            Mathf.Clamp01(maxOpacity),
            Mathf.Clamp01(desaturation),
            sceneColor,
            Mathf.Clamp01(detailRetention)));
        Shader.SetGlobalVector(FogNoiseId, new Vector4(
            Mathf.Max(turbulence, 0.0f),
            Mathf.Max(turbulenceSpeed, 0.0f),
            Mathf.Max(turbulenceScale, 0.001f),
            Mathf.Max(edgeBlur, 0.0f)));
    }

    private bool SceneColorAvailable()
    {
        if (fogCamera != null)
        {
            UniversalAdditionalCameraData data = fogCamera.GetUniversalAdditionalCameraData();
            if (data != null && data.requiresColorTexture) return true;
        }

        UniversalRenderPipelineAsset asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        return asset != null && asset.supportsCameraOpaqueTexture;
    }

    private bool EnsureResources()
    {
        if (fogMaterial == null)
        {
            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogWarning($"{nameof(VisionFogOverlay)}: shader '{ShaderName}' not found — no fog will draw.", this);
                enabled = false;
                return false;
            }
            fogMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };
        }

        if (quad == null)
        {
            quad = new Mesh { name = "VisionFogQuad", hideFlags = HideFlags.DontSave };
            quad.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0.0f),
                new Vector3( 0.5f, -0.5f, 0.0f),
                new Vector3( 0.5f,  0.5f, 0.0f),
                new Vector3(-0.5f,  0.5f, 0.0f)
            };
            quad.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            quad.bounds = new Bounds(Vector3.zero, Vector3.one * 1.0e6f);
        }

        return true;
    }
}
