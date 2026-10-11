using System.Collections.Generic;
using UnityEngine;

public static class BoxMesh
{
    private static Mesh cubeMesh;
    private static readonly Dictionary<Color, Material> materialsByColor = new Dictionary<Color, Material>();

    public static GameObject Create(Transform owner, string name, Vector3 worldSize, Color color)
    {
        GameObject box = CreateDetached(name, worldSize, color);
        box.transform.SetParent(owner, false);
        SetWorldSize(box.transform, owner, worldSize);

        return box;
    }

    public static GameObject CreateDetached(string name, Vector3 worldSize, Color color)
    {
        GameObject box = new GameObject(name);
        box.transform.localScale = worldSize;

        MeshFilter meshFilter = box.AddComponent<MeshFilter>();
        meshFilter.mesh = GetCubeMesh();
        MeshRenderer meshRenderer = box.AddComponent<MeshRenderer>();
        meshRenderer.material = GetMaterial(color);

        return box;
    }

    public static void SetWorldSize(Transform box, Transform owner, Vector3 worldSize)
    {
        Vector3 ownerScale = owner.lossyScale;
        box.localScale = new Vector3(
            ownerScale.x != 0.0f ? worldSize.x / ownerScale.x : worldSize.x,
            ownerScale.y != 0.0f ? worldSize.y / ownerScale.y : worldSize.y,
            ownerScale.z != 0.0f ? worldSize.z / ownerScale.z : worldSize.z);
    }

    private static Mesh GetCubeMesh()
    {
        if (cubeMesh == null)
        {
            GameObject temporaryCubeForMeshExtraction = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubeMesh = temporaryCubeForMeshExtraction.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(temporaryCubeForMeshExtraction);
        }

        return cubeMesh;
    }

    private static Material GetMaterial(Color color)
    {
        if (!materialsByColor.TryGetValue(color, out Material material) || material == null)
        {
            material = new Material(Shader.Find("Sprites/Default")) { color = color };
            materialsByColor[color] = material;
        }

        return material;
    }
}
