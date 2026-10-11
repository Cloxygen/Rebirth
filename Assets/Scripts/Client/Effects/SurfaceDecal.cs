using UnityEngine;
using UnityEngine.Rendering;

public static class SurfaceDecal
{
    private static Material decalMaterial;

    public static void Stamp(
        string name,
        Mesh mesh,
        Vector3 surfacePoint,
        Vector3 surfaceNormal,
        Vector3 alignDirection,
        float size,
        float lifetime,
        Color color,
        Transform surface = null,
        float surfaceOffset = 0.01f)
    {
        Vector3 projectedDirection = Vector3.ProjectOnPlane(alignDirection, surfaceNormal);
        if (projectedDirection.sqrMagnitude < 0.0001f)
        {
            projectedDirection = Vector3.ProjectOnPlane(Vector3.forward, surfaceNormal);
        }
        if (projectedDirection.sqrMagnitude < 0.0001f)
        {
            projectedDirection = Vector3.ProjectOnPlane(Vector3.right, surfaceNormal);
        }

        GameObject decal = new GameObject(name);
        decal.transform.position = surfacePoint + surfaceNormal * surfaceOffset;
        decal.transform.rotation = Quaternion.LookRotation(surfaceNormal, projectedDirection);
        decal.transform.localScale = new Vector3(size, size, 1.0f);

        if (surface != null)
        {
            decal.transform.SetParent(surface, true);
        }

        MeshFilter meshFilter = decal.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;

        MeshRenderer meshRenderer = decal.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = GetDecalMaterial();
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        decal.AddComponent<SurfaceDecalFader>().Initialize(meshRenderer, lifetime, color);
    }

    private static Material GetDecalMaterial()
    {
        if (decalMaterial == null)
        {
            decalMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));

            decalMaterial.SetFloat("_Surface", 1.0f);
            decalMaterial.SetOverrideTag("RenderType", "Transparent");
            decalMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            decalMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            decalMaterial.SetInt("_ZWrite", 0);
            decalMaterial.SetInt("_Cull", (int)CullMode.Off);
            decalMaterial.DisableKeyword("_ALPHATEST_ON");
            decalMaterial.EnableKeyword("_ALPHABLEND_ON");
            decalMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            decalMaterial.renderQueue = (int)RenderQueue.Transparent;
        }

        return decalMaterial;
    }
}

public class SurfaceDecalFader : MonoBehaviour
{
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    private MeshRenderer targetRenderer;
    private MaterialPropertyBlock propertyBlock;
    private Color baseColor;
    private float lifetime;
    private float elapsed;

    public void Initialize(MeshRenderer meshRenderer, float fadeLifetime, Color color)
    {
        targetRenderer = meshRenderer;
        lifetime = fadeLifetime;
        baseColor = color;
        propertyBlock = new MaterialPropertyBlock();
        ApplyColor(baseColor);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / lifetime);

        Color faded = baseColor;
        faded.a = baseColor.a * (1.0f - t);
        ApplyColor(faded);

        if (t >= 1.0f)
        {
            Destroy(gameObject);
        }
    }

    private void ApplyColor(Color color)
    {
        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(BaseColorID, color);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }
}
