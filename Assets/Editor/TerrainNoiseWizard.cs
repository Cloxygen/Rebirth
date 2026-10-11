using UnityEditor;
using UnityEngine;

public class TerrainNoiseWizard : ScriptableWizard
{
    public Terrain terrain;

    [Range(0.0f, 20.0f)]
    public float amplitudeMetres = 2.0f;

    [Range(5.0f, 400.0f)]
    public float featureSizeMetres = 70.0f;

    [Range(1, 5)]
    public int octaves = 3;

    public int seed = 1;

    [Header("Flatten")]
    public float flattenHeightMetres = 0.0f;

    [MenuItem("Rebirth/Terrain/Add Gentle Noise...")]
    private static void Open()
    {
        DisplayWizard<TerrainNoiseWizard>("Add Gentle Terrain Noise", "Apply", "Flatten");
    }

    private void OnEnable()
    {
        if (terrain == null) terrain = Terrain.activeTerrain;
        if (terrain == null) terrain = FindFirstObjectByType<Terrain>();
    }

    private void OnWizardUpdate()
    {
        if (terrain == null || terrain.terrainData == null)
        {
            errorString = "No terrain found — assign one.";
            isValid = false;
            return;
        }

        errorString = string.Empty;
        isValid = true;

        TerrainData data = terrain.terrainData;
        helpString =
            $"{data.heightmapResolution}x{data.heightmapResolution} heightmap over {data.size.x:0}x{data.size.z:0}m, " +
            $"max height {data.size.y:0}m.\n" +
            "Apply adds on top of what's already there, so running it twice stacks. " +
            $"Flatten levels the whole terrain to {flattenHeightMetres:0.##}m — every sample, not just the noise, " +
            "so hand-sculpting goes with it. Ctrl+Z undoes either.";
    }

    private void OnWizardOtherButton()
    {
        if (terrain == null || terrain.terrainData == null) return;

        TerrainData data = terrain.terrainData;
        int resolution = data.heightmapResolution;

        float normalisedHeight = data.size.y > 0.0001f
            ? Mathf.Clamp01(flattenHeightMetres / data.size.y)
            : 0.0f;

        Undo.RegisterCompleteObjectUndo(data, "Flatten Terrain");

        float[,] heights = new float[resolution, resolution];

        if (normalisedHeight > 0.0f)
        {
            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    heights[z, x] = normalisedHeight;
                }
            }
        }

        data.SetHeights(0, 0, heights);
        terrain.Flush();
        EditorUtility.SetDirty(data);

        Debug.Log($"Terrain flattened to {flattenHeightMetres:0.##}m.", terrain);
    }

    private void OnWizardCreate()
    {
        if (terrain == null || terrain.terrainData == null) return;

        TerrainData data = terrain.terrainData;
        int resolution = data.heightmapResolution;
        Vector3 size = data.size;

        float normalisedAmplitude = size.y > 0.0001f ? amplitudeMetres / size.y : 0.0f;
        if (normalisedAmplitude <= 0.0f) return;

        Undo.RegisterCompleteObjectUndo(data, "Add Gentle Terrain Noise");

        float[,] heights = data.GetHeights(0, 0, resolution, resolution);

        float offsetX = seed * 137.31f;
        float offsetZ = seed * 71.77f;

        for (int z = 0; z < resolution; z++)
        {
            float worldZ = z / (float)(resolution - 1) * size.z;

            for (int x = 0; x < resolution; x++)
            {
                float worldX = x / (float)(resolution - 1) * size.x;
                float noise = Fbm(worldX + offsetX, worldZ + offsetZ);

                heights[z, x] = Mathf.Clamp01(heights[z, x] + noise * normalisedAmplitude);
            }
        }

        data.SetHeights(0, 0, heights);
        terrain.Flush();
        EditorUtility.SetDirty(data);

        Debug.Log($"Terrain noise applied: {amplitudeMetres}m over {featureSizeMetres}m features, {octaves} octaves, seed {seed}.", terrain);
    }

    private float Fbm(float worldX, float worldZ)
    {
        float frequency = 1.0f / Mathf.Max(featureSizeMetres, 0.001f);
        float amplitude = 0.5f;
        float sum = 0.0f;
        float normalisation = 0.0f;

        for (int i = 0; i < octaves; i++)
        {
            sum += amplitude * Mathf.PerlinNoise(worldX * frequency, worldZ * frequency);
            normalisation += amplitude;
            frequency *= 2.03f;
            amplitude *= 0.5f;
        }

        return normalisation > 0.0f ? sum / normalisation : 0.0f;
    }
}
