using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class VisionHidden : MonoBehaviour
{
    [SerializeField] private Transform measureFrom;

    [SerializeField] private bool includeInactiveChildren = true;

    [SerializeField] private bool fadeOut = true;

    private static readonly int VisionFadeId = Shader.PropertyToID("_VisionFade");
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int MetallicId = Shader.PropertyToID("_Metallic");
    private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
    private static readonly int CullId = Shader.PropertyToID("_Cull");

    private const string DitherMaterialPath = "VisionDither";

    private static readonly Dictionary<Material, Material> ditherStandIns = new Dictionary<Material, Material>();
    private static Shader ditherShader;
    private static bool ditherShaderLoaded;

    private Renderer[] renderers;
    private Canvas[] canvases;

    private bool[] rendererWasEnabled;
    private bool[] canvasWasEnabled;

    private bool[] canDither;

    private Material[] solidMaterials;
    private Material[] fadingMaterials;
    private bool[] wearingFadingMaterial;

    private MaterialPropertyBlock block;

    private bool visible = true;
    private float appliedFade = -1.0f;
    private bool hasCache;

    public Vector3 SamplePosition => measureFrom != null ? measureFrom.position : transform.position;
    public bool IsVisible => visible;

    public float Fade => appliedFade < 0.0f ? 0.0f : appliedFade;

    private void OnEnable()
    {
        BuildCache();
        visible = true;
        appliedFade = -1.0f;
        if (VisionField.Instance != null) VisionField.Instance.Register(this);
    }

    private void OnDisable()
    {
        if (VisionField.Instance != null) VisionField.Instance.Unregister(this);

        ApplyFade(0.0f, Color.black);
    }

    private void Start()
    {
        if (VisionField.Instance != null) VisionField.Instance.Register(this);
    }

    public void RefreshCache()
    {
        hasCache = false;
        BuildCache();
    }

    private void BuildCache()
    {
        block ??= new MaterialPropertyBlock();

        if (hasCache && renderers != null) return;

        renderers = GetComponentsInChildren<Renderer>(includeInactiveChildren);
        canvases = GetComponentsInChildren<Canvas>(includeInactiveChildren);
        rendererWasEnabled = new bool[renderers.Length];
        canvasWasEnabled = new bool[canvases.Length];
        canDither = new bool[renderers.Length];
        solidMaterials = new Material[renderers.Length];
        fadingMaterials = new Material[renderers.Length];
        wearingFadingMaterial = new bool[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            Material material = renderers[i] != null ? renderers[i].sharedMaterial : null;
            if (material == null) continue;

            solidMaterials[i] = material;

            if (material.HasProperty(VisionFadeId))
            {
                canDither[i] = true;
                continue;
            }

            fadingMaterials[i] = StandInFor(material);
            canDither[i] = fadingMaterials[i] != null;
        }

        hasCache = true;
    }

    private static Material StandInFor(Material source)
    {
        if (ditherStandIns.TryGetValue(source, out Material existing) && existing != null) return existing;

        Shader shader = DitherShader();
        if (shader == null) return null;

        Material standIn = new Material(shader) { name = source.name + " (dissolving)" };

        if (source.HasProperty(BaseMapId)) standIn.SetTexture(BaseMapId, source.GetTexture(BaseMapId));
        if (source.HasProperty(BaseColorId)) standIn.SetColor(BaseColorId, source.GetColor(BaseColorId));
        if (source.HasProperty(MetallicId)) standIn.SetFloat(MetallicId, source.GetFloat(MetallicId));
        if (source.HasProperty(SmoothnessId)) standIn.SetFloat(SmoothnessId, source.GetFloat(SmoothnessId));
        if (source.HasProperty(CullId)) standIn.SetFloat(CullId, source.GetFloat(CullId));

        ditherStandIns[source] = standIn;
        return standIn;
    }

    private static Shader DitherShader()
    {
        if (ditherShaderLoaded) return ditherShader;
        ditherShaderLoaded = true;

        Material template = Resources.Load<Material>(DitherMaterialPath);
        ditherShader = template != null ? template.shader : null;

        if (ditherShader == null)
        {
            Debug.LogWarning($"VisionHidden: no material at Resources/{DitherMaterialPath}, so anything without its " +
                             "own vision fade switches off at the edge instead of dissolving.");
        }

        return ditherShader;
    }

    private void WearFadingMaterial(int index, bool fading)
    {
        if (fadingMaterials[index] == null || wearingFadingMaterial[index] == fading) return;

        wearingFadingMaterial[index] = fading;
        renderers[index].sharedMaterial = fading ? fadingMaterials[index] : solidMaterials[index];
    }

    internal void ApplyFade(float fade, Color fogTint)
    {
        BuildCache();
        fade = Mathf.Clamp01(fade);
        if (!fadeOut) fade = fade >= 1.0f ? 1.0f : 0.0f;

        bool nextVisible = fade < 1.0f;

        if (Application.isPlaying && nextVisible != visible) SetRenderersEnabled(nextVisible);

        if (Mathf.Approximately(fade, appliedFade)) return;
        appliedFade = fade;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null || !canDither[i]) continue;

            WearFadingMaterial(i, fade > 0.0f);

            renderers[i].GetPropertyBlock(block);
            block.SetFloat(VisionFadeId, fade);
            renderers[i].SetPropertyBlock(block);
        }
    }

    private void SetRenderersEnabled(bool nextVisible)
    {
        visible = nextVisible;

        if (!nextVisible)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null || canDither[i]) continue;
                rendererWasEnabled[i] = renderers[i].enabled;
                renderers[i].enabled = false;
            }
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] == null) continue;
                canvasWasEnabled[i] = canvases[i].enabled;
                canvases[i].enabled = false;
            }
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && !canDither[i]) renderers[i].enabled = rendererWasEnabled[i];
        }
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] != null) canvases[i].enabled = canvasWasEnabled[i];
        }
    }
}
