using System.Collections.Generic;
using UnityEngine;

public static class BloodDecal
{
    public const float MinimumImpactSpeed = 1.0f;

    private const float FastImpactSpeed = 4.5f;

    private const float MinSize = 0.10f;
    private const float MaxSize = 0.28f;
    private const float Lifetime = 14.0f;

    private const float SurfaceOffset = 0.016f;
    private const float SurfaceOffsetJitter = 0.006f;

    private const int VariantCount = 6;
    private const int BodySegments = 24;
    private const int DropletSegments = 8;

    private static readonly Color SplatColor = new Color(0.68f, 0.02f, 0.02f, 1.0f);

    private static Mesh[] splatMeshes;

    public static void Spawn(Vector3 point, Vector3 normal, Vector3 impactVelocity, Transform surface)
    {
        float speedFraction = Mathf.InverseLerp(MinimumImpactSpeed, FastImpactSpeed, impactVelocity.magnitude);
        float size = Mathf.Lerp(MinSize, MaxSize, speedFraction) * Random.Range(0.85f, 1.15f);

        SurfaceDecal.Stamp(
            "BloodDecal",
            GetSplatMesh(),
            point,
            normal,
            impactVelocity,
            size,
            Lifetime,
            SplatColor,
            surface,
            SurfaceOffset + Random.value * SurfaceOffsetJitter);
    }

    private static Mesh GetSplatMesh()
    {
        if (splatMeshes == null || splatMeshes[0] == null)
        {
            splatMeshes = new Mesh[VariantCount];
            for (int i = 0; i < VariantCount; i++)
            {
                splatMeshes[i] = BuildSplatMesh(i);
            }
        }

        return splatMeshes[Random.Range(0, splatMeshes.Length)];
    }

    private static Mesh BuildSplatMesh(int variantIndex)
    {
        Random.State callerState = Random.state;
        Random.InitState(104729 + variantIndex * 7919);

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        List<int> triangles = new List<int>();

        AddFan(vertices, uv, triangles, Vector2.zero, BuildWarpedRadii(
            BodySegments,
            Random.Range(0.26f, 0.32f),
            Random.Range(0.22f, 0.38f), Random.Range(2, 5),
            Random.Range(0.06f, 0.14f), Random.Range(5, 9)));

        int dropletCount = Random.Range(3, 7);
        for (int i = 0; i < dropletCount; i++)
        {
            float angle = Random.Range(0.0f, Mathf.PI * 2.0f);
            float distance = Random.Range(0.30f, 0.46f);
            Vector2 center = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

            AddFan(vertices, uv, triangles, center, BuildUniformRadii(DropletSegments, Random.Range(0.02f, 0.055f)));
        }

        Random.state = callerState;

        Mesh mesh = new Mesh { name = $"BloodSplatMesh{variantIndex}" };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private static float[] BuildWarpedRadii(int segments, float radius, float lobeStrength, int lobeCount, float rippleStrength, int rippleCount)
    {
        float lobePhase = Random.Range(0.0f, Mathf.PI * 2.0f);
        float ripplePhase = Random.Range(0.0f, Mathf.PI * 2.0f);
        float[] radii = new float[segments];

        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2.0f;
            float warp = 1.0f
                + lobeStrength * Mathf.Sin(angle * lobeCount + lobePhase)
                + rippleStrength * Mathf.Sin(angle * rippleCount + ripplePhase);

            radii[i] = radius * Mathf.Max(0.3f, warp);
        }

        return radii;
    }

    private static float[] BuildUniformRadii(int segments, float radius)
    {
        float[] radii = new float[segments];

        for (int i = 0; i < segments; i++)
        {
            radii[i] = radius;
        }

        return radii;
    }

    private static void AddFan(List<Vector3> vertices, List<Vector2> uv, List<int> triangles, Vector2 center, float[] radii)
    {
        int centerIndex = vertices.Count;
        AddVertex(vertices, uv, center);

        for (int i = 0; i < radii.Length; i++)
        {
            float angle = (float)i / radii.Length * Mathf.PI * 2.0f;
            AddVertex(vertices, uv, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radii[i]);

            triangles.Add(centerIndex);
            triangles.Add(centerIndex + 1 + i);
            triangles.Add(centerIndex + 1 + (i + 1) % radii.Length);
        }
    }

    private static void AddVertex(List<Vector3> vertices, List<Vector2> uv, Vector2 point)
    {
        vertices.Add(new Vector3(point.x, point.y, 0.0f));
        uv.Add(new Vector2(point.x + 0.5f, point.y + 0.5f));
    }
}
