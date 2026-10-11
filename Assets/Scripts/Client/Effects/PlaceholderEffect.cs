using UnityEngine;

public class PlaceholderEffect : MonoBehaviour
{
    public Color color = Color.cyan;
    public float maxScale = 1.5f;

    private MeshRenderer meshRenderer;
    private float timer = 0f;
    private AutoDestroy autoDestroy;

    void Awake()
    {
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.transform.SetParent(this.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = Vector3.one * 0.3f;

        if (visual.TryGetComponent(out Collider col))
        {
            Destroy(col);
        }

        meshRenderer = visual.GetComponent<MeshRenderer>();
        meshRenderer.material = new Material(Shader.Find("Sprites/Default"));
        meshRenderer.material.color = color;

        autoDestroy = gameObject.GetComponent<AutoDestroy>();
        if (autoDestroy == null) autoDestroy = gameObject.AddComponent<AutoDestroy>();
        autoDestroy.lifetime = 0.4f;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float progress = timer / autoDestroy.lifetime;

        transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * maxScale, progress);

        if (meshRenderer != null)
        {
            Color c = meshRenderer.material.color;
            c.a = 1f - progress;
            meshRenderer.material.color = c;
        }
    }
}
