using UnityEngine;

public static class FootprintEffect
{
    [System.Serializable]
    public class Settings
    {
        public float size = 0.4f;
        public float lifetime = 6.0f;
        public Color color = new Color(0.03f, 0.02f, 0.02f, 0.65f);
    }

    private const float RaycastHeight = 1.0f;
    private const float RaycastDistance = 3.0f;
    private const float GroundOffset = 0.01f;

    private static readonly Vector2[] FootOutline =
    {
        new Vector2(0.00f, -0.50f),
        new Vector2(0.16f, -0.46f),
        new Vector2(0.24f, -0.30f),
        new Vector2(0.23f, -0.10f),
        new Vector2(0.19f, 0.10f),
        new Vector2(0.22f, 0.28f),
        new Vector2(0.16f, 0.44f),
        new Vector2(0.06f, 0.50f),
        new Vector2(-0.06f, 0.50f),
        new Vector2(-0.16f, 0.44f),
        new Vector2(-0.22f, 0.28f),
        new Vector2(-0.19f, 0.10f),
        new Vector2(-0.23f, -0.10f),
        new Vector2(-0.24f, -0.30f),
        new Vector2(-0.16f, -0.46f),
    };

    private static Mesh footMesh;

    public static void Spawn(Vector3 originPosition, Quaternion facingRotation, float sidewaysOffset, Transform owner = null, Settings settings = null)
    {
        settings ??= DefaultSettings;

        Vector3 rightDirection = facingRotation * Vector3.right;
        Vector3 footOrigin = originPosition + rightDirection * sidewaysOffset;

        if (!TryFindGroundHit(footOrigin, owner, out RaycastHit hit))
        {
            return;
        }

        if (!VisionField.IsVisible(hit.point))
        {
            return;
        }

        SurfaceDecal.Stamp(
            "FootprintDecal",
            GetFootMesh(),
            hit.point,
            hit.normal,
            facingRotation * Vector3.forward,
            settings.size,
            settings.lifetime,
            settings.color,
            surfaceOffset: GroundOffset);
    }

    private static readonly Settings DefaultSettings = new Settings();

    private static bool TryFindGroundHit(Vector3 originPosition, Transform owner, out RaycastHit groundHit)
    {
        RaycastHit[] hits = Physics.RaycastAll(
            originPosition + Vector3.up * RaycastHeight,
            Vector3.down,
            RaycastDistance,
            ~0,
            QueryTriggerInteraction.Ignore);

        bool found = false;
        groundHit = default;
        float closestDistance = float.MaxValue;

        foreach (RaycastHit candidate in hits)
        {
            if (owner != null && candidate.collider.transform.IsChildOf(owner)) continue;
            if (candidate.distance >= closestDistance) continue;

            closestDistance = candidate.distance;
            groundHit = candidate;
            found = true;
        }

        return found;
    }

    private static Mesh GetFootMesh()
    {
        if (footMesh == null)
        {
            int perimeterCount = FootOutline.Length;
            Vector3[] vertices = new Vector3[perimeterCount + 1];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[perimeterCount * 3];

            vertices[0] = Vector3.zero;
            uv[0] = new Vector2(0.5f, 0.5f);

            for (int i = 0; i < perimeterCount; i++)
            {
                Vector2 point = FootOutline[i];
                vertices[i + 1] = new Vector3(point.x, -point.y, 0.0f);
                uv[i + 1] = new Vector2(point.x + 0.5f, -point.y + 0.5f);

                int next = (i + 1) % perimeterCount;
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = next + 1;
            }

            footMesh = new Mesh { name = "FootprintMesh" };
            footMesh.SetVertices(vertices);
            footMesh.SetUVs(0, uv);
            footMesh.SetTriangles(triangles, 0);
            footMesh.RecalculateNormals();
            footMesh.RecalculateBounds();
        }

        return footMesh;
    }
}
