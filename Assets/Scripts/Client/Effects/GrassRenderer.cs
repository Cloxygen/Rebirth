using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class GrassRenderer : MonoBehaviour
{
    private const int BladeStride = 32;
    private const int ThreadGroupSize = 8;

    private const int InstanceCountOffset = sizeof(uint);

    private const int MaxCandidateBlades = 4_000_000;

    private const int InitialOverlapCapacity = 64;
    private const int InitialBenderCapacity = 64;
    private const int BenderStride = sizeof(float) * 4;

    private const int InitialBlockerCapacity = 16;
    private const int BlockerStride = sizeof(float) * 4;

    private const float EnvironmentRadiusMultiplier = 6.0f;

    private const int WarmupFrames = 30;
    private const int SampleFrames = 30;
    private const int MaxReports = 5;

    private static readonly int BladesID = Shader.PropertyToID("_Blades");
    private static readonly int HeightmapID = Shader.PropertyToID("_Heightmap");
    private static readonly int TerrainPositionID = Shader.PropertyToID("_TerrainPosition");
    private static readonly int TerrainSizeID = Shader.PropertyToID("_TerrainSize");
    private static readonly int HeightmapResolutionID = Shader.PropertyToID("_HeightmapResolution");
    private static readonly int GridOriginIndexID = Shader.PropertyToID("_GridOriginIndex");
    private static readonly int CellSizeID = Shader.PropertyToID("_CellSize");
    private static readonly int BladesPerSideID = Shader.PropertyToID("_BladesPerSide");
    private static readonly int CameraPositionID = Shader.PropertyToID("_CameraPosition");
    private static readonly int DrawDistanceID = Shader.PropertyToID("_DrawDistance");
    private static readonly int FalloffStartDistanceID = Shader.PropertyToID("_FalloffStartDistance");
    private static readonly int DensityID = Shader.PropertyToID("_Density");
    private static readonly int FrustumPlanesID = Shader.PropertyToID("_FrustumPlanes");
    private static readonly int BladeHeightRangeID = Shader.PropertyToID("_BladeHeightRange");
    private static readonly int BladeSegmentsID = Shader.PropertyToID("_BladeSegments");
    private static readonly int PatchSizeFalloffID = Shader.PropertyToID("_PatchSizeFalloff");
    private static readonly int BladeWidthRangeID = Shader.PropertyToID("_BladeWidthRange");
    private static readonly int BendersID = Shader.PropertyToID("_Benders");
    private static readonly int BenderCountID = Shader.PropertyToID("_BenderCount");
    private static readonly int BlockersID = Shader.PropertyToID("_Blockers");
    private static readonly int BlockerCountID = Shader.PropertyToID("_BlockerCount");
    private static readonly int BendMapPreviousID = Shader.PropertyToID("_BendMapPrevious");
    private static readonly int BendMapCurrentID = Shader.PropertyToID("_BendMapCurrent");
    private static readonly int BendMapID = Shader.PropertyToID("_BendMap");
    private static readonly int BendMapOriginID = Shader.PropertyToID("_BendMapOrigin");
    private static readonly int BendMapPreviousOriginID = Shader.PropertyToID("_BendMapPreviousOrigin");
    private static readonly int BendMapTexelSizeID = Shader.PropertyToID("_BendMapTexelSize");
    private static readonly int BendMapResolutionID = Shader.PropertyToID("_BendMapResolution");
    private static readonly int BendMapWorldSizeID = Shader.PropertyToID("_BendMapWorldSize");
    private static readonly int BendRecoveryThisFrameID = Shader.PropertyToID("_BendRecoveryThisFrame");
    private static readonly int GrassMaskEnabledID = Shader.PropertyToID("_GrassMaskEnabled");
    private static readonly int GrassMaskThresholdID = Shader.PropertyToID("_GrassMaskThreshold");
    private static readonly int GrassMaskFeatherID = Shader.PropertyToID("_GrassMaskFeather");

    private static readonly string[] GroundMaskSourceProperties =
    {
        "_WorldTileSize",
        "_PatchScale",
        "_EdgeRaggedness",
        "_MossAmount",
        "_MacroVariationScale",
        "_MacroVariationAmount",
        "_BlendSharpness"
    };

    private static readonly int[] GroundMaskTargetIDs =
    {
        Shader.PropertyToID("_GroundWorldTileSize"),
        Shader.PropertyToID("_GroundPatchScale"),
        Shader.PropertyToID("_GroundEdgeRaggedness"),
        Shader.PropertyToID("_GroundMossAmount"),
        Shader.PropertyToID("_GroundMacroVariationScale"),
        Shader.PropertyToID("_GroundMacroVariationAmount"),
        Shader.PropertyToID("_GroundBlendSharpness")
    };

    [Header("References")]
    [SerializeField] private Terrain terrain;
    [SerializeField] private ComputeShader scatterCompute;
    [SerializeField] private Material grassMaterial;
    [SerializeField] private Camera targetCamera;

    private float drawDistance;
    private float falloffStartDistance;
    private float cellSize;
    private float density;
    private float patchSizeFalloff;

    [Header("Ground Mask")]
    [SerializeField] private bool restrictToGroundMoss = true;
    [Range(0.0f, 1.0f)]
    [SerializeField] private float groundMossThreshold = 0.5f;
    [Range(0.001f, 0.5f)]
    [SerializeField] private float groundMossFeather = 0.15f;

    private Vector2 bladeHeightRange;
    private Vector2 bladeWidthRange;
    private int bladeSegments;

    [Header("Interaction")]
    [SerializeField] private LayerMask benderLayers = ~0;
    [SerializeField] private float maxBenderRadius = 2.0f;
    [SerializeField] private float benderVerticalRange = 3.0f;
    [SerializeField] private float groundContactTolerance = 0.15f;
    [SerializeField] private float bendRecoveryDuration = 0.5f;
    [SerializeField] private int bendMapResolution = 1024;

    [Header("Exclusion")]
    [SerializeField] private LayerMask grassBlockerLayers = 0;
    [SerializeField] private float blockerGroundClearance = 0.0f;

    [Header("Diagnostics")]
    [SerializeField] private bool logDiagnosticsOnStart = true;

    private ComputeBuffer bladeBuffer;
    private ComputeBuffer argsBuffer;
    private Mesh bladeMesh;
    private Bounds renderBounds;
    private readonly Plane[] frustumPlanes = new Plane[6];
    private readonly Vector4[] frustumPlaneVectors = new Vector4[6];
    private readonly uint[] args = new uint[5];
    private int scatterKernel;
    private int bendMapKernel;
    private int bladesPerSide;
    private bool ready;

    private readonly List<Vector4> benderList = new List<Vector4>();
    private Collider[] benderOverlapBuffer = new Collider[InitialOverlapCapacity];
    private ComputeBuffer benderBuffer;
    private int benderBufferCapacity;

    private readonly List<Vector4> blockerList = new List<Vector4>();
    private Collider[] blockerOverlapBuffer = new Collider[InitialOverlapCapacity];
    private ComputeBuffer blockerBuffer;
    private int blockerBufferCapacity;

    private RenderTexture bendMapPrevious;
    private RenderTexture bendMapCurrent;
    private Vector2 bendMapOrigin;

    private Material groundMaterial;
    private bool groundExposesMossMask;

    private readonly System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
    private int frameIndex;
    private int reportsLogged;
    private int windowFrames;
    private float windowFrameTime;
    private long windowDrawTicks;

    private float BendMapWorldSize => drawDistance * 2.0f;

    private float BendMapTexelSize => BendMapWorldSize / bendMapResolution;

    private void OnEnable()
    {
        ready = TryInitialize();
        if (!ready)
        {
            enabled = false;
            return;
        }

        frameIndex = 0;
        reportsLogged = 0;
        windowFrames = 0;
        windowFrameTime = 0.0f;
        windowDrawTicks = 0;
    }

    private void OnDisable()
    {
        ReleaseResources();
        ready = false;
    }

    private void LateUpdate()
    {
        if (!ready)
        {
            return;
        }

        Camera camera = ResolveCamera();
        if (camera == null)
        {
            return;
        }

        GatherBlockers(camera);
        DispatchScatter(camera);
        GatherBenders(camera);
        UpdateBendMap(camera);
        DrawBlades(camera);
        ReportPeriodically();
    }

    private void GatherBenders(Camera camera)
    {
        Vector3 cameraPosition = camera.transform.position;
        float groundY = terrain.SampleHeight(cameraPosition) + terrain.transform.position.y;
        Vector3 centre = new Vector3(cameraPosition.x, groundY, cameraPosition.z);
        Vector3 halfExtents = new Vector3(drawDistance, benderVerticalRange, drawDistance);

        int hits;
        while (true)
        {
            hits = Physics.OverlapBoxNonAlloc(
                centre,
                halfExtents,
                benderOverlapBuffer,
                Quaternion.identity,
                benderLayers,
                QueryTriggerInteraction.Ignore);

            if (hits < benderOverlapBuffer.Length)
            {
                break;
            }

            benderOverlapBuffer = new Collider[benderOverlapBuffer.Length * 2];
        }

        benderList.Clear();

        for (int i = 0; i < hits; i++)
        {
            Collider candidate = benderOverlapBuffer[i];

            if (candidate is TerrainCollider)
            {
                continue;
            }

            VisionHidden gated = candidate.GetComponentInParent<VisionHidden>();
            if (gated != null && !gated.IsVisible)
            {
                continue;
            }

            Bounds bounds = candidate.bounds;

            float groundHeightHere = terrain.SampleHeight(bounds.center) + terrain.transform.position.y;
            if (bounds.min.y - groundHeightHere > groundContactTolerance)
            {
                continue;
            }

            float rawRadius = ComputeFootprintRadius(candidate, bounds);

            if (rawRadius > maxBenderRadius * EnvironmentRadiusMultiplier)
            {
                continue;
            }

            float radius = Mathf.Min(rawRadius, maxBenderRadius);
            if (radius <= 0.0f)
            {
                continue;
            }

            benderList.Add(new Vector4(bounds.center.x, bounds.center.z, radius, 0.0f));
        }

        EnsureBenderBufferCapacity(benderList.Count);
        if (benderList.Count > 0)
        {
            benderBuffer.SetData(benderList, 0, 0, benderList.Count);
        }
    }

    private void GatherBlockers(Camera camera)
    {
        blockerList.Clear();

        if (grassBlockerLayers.value != 0)
        {
            Vector3 cameraPosition = camera.transform.position;
            float groundY = terrain.SampleHeight(cameraPosition) + terrain.transform.position.y;
            Vector3 centre = new Vector3(cameraPosition.x, groundY, cameraPosition.z);
            Vector3 halfExtents = new Vector3(drawDistance, benderVerticalRange, drawDistance);

            float pokeThroughHeight = blockerGroundClearance > 0.0f
                ? blockerGroundClearance
                : bladeHeightRange.y;

            int hits;
            while (true)
            {
                hits = Physics.OverlapBoxNonAlloc(
                    centre,
                    halfExtents,
                    blockerOverlapBuffer,
                    Quaternion.identity,
                    grassBlockerLayers,
                    QueryTriggerInteraction.Ignore);

                if (hits < blockerOverlapBuffer.Length)
                {
                    break;
                }

                blockerOverlapBuffer = new Collider[blockerOverlapBuffer.Length * 2];
            }

            for (int i = 0; i < hits; i++)
            {
                Collider candidate = blockerOverlapBuffer[i];

                if (candidate is TerrainCollider)
                {
                    continue;
                }

                Bounds bounds = candidate.bounds;

                float groundHeightHere = terrain.SampleHeight(bounds.center) + terrain.transform.position.y;
                if (bounds.min.y - groundHeightHere > pokeThroughHeight)
                {
                    continue;
                }

                blockerList.Add(new Vector4(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z));
            }
        }

        EnsureBlockerBufferCapacity(blockerList.Count);
        if (blockerList.Count > 0)
        {
            blockerBuffer.SetData(blockerList, 0, 0, blockerList.Count);
        }
    }

    private void EnsureBlockerBufferCapacity(int required)
    {
        if (blockerBuffer != null && blockerBufferCapacity >= required)
        {
            return;
        }

        blockerBuffer?.Release();
        blockerBufferCapacity = Mathf.Max(InitialBlockerCapacity, Mathf.Max(required, blockerBufferCapacity * 2));
        blockerBuffer = new ComputeBuffer(blockerBufferCapacity, BlockerStride, ComputeBufferType.Structured);
    }

    private void UpdateBendMap(Camera camera)
    {
        Vector3 cameraPosition = camera.transform.position;
        float texelSize = BendMapTexelSize;

        Vector2 snappedOrigin = new Vector2(
            Mathf.Floor((cameraPosition.x - drawDistance) / texelSize) * texelSize,
            Mathf.Floor((cameraPosition.z - drawDistance) / texelSize) * texelSize);

        scatterCompute.SetTexture(bendMapKernel, BendMapPreviousID, bendMapPrevious);
        scatterCompute.SetTexture(bendMapKernel, BendMapCurrentID, bendMapCurrent);
        scatterCompute.SetBuffer(bendMapKernel, BendersID, benderBuffer);
        scatterCompute.SetInt(BenderCountID, benderList.Count);
        scatterCompute.SetVector(BendMapOriginID, snappedOrigin);
        scatterCompute.SetVector(BendMapPreviousOriginID, bendMapOrigin);
        scatterCompute.SetFloat(BendMapTexelSizeID, texelSize);
        scatterCompute.SetInt(BendMapResolutionID, bendMapResolution);
        scatterCompute.SetFloat(BendRecoveryThisFrameID, Time.deltaTime / bendRecoveryDuration);

        int groups = Mathf.CeilToInt(bendMapResolution / (float)ThreadGroupSize);
        scatterCompute.Dispatch(bendMapKernel, groups, groups, 1);

        (bendMapPrevious, bendMapCurrent) = (bendMapCurrent, bendMapPrevious);
        bendMapOrigin = snappedOrigin;

        grassMaterial.SetTexture(BendMapID, bendMapPrevious);
        grassMaterial.SetVector(BendMapOriginID, snappedOrigin);
        grassMaterial.SetFloat(BendMapWorldSizeID, BendMapWorldSize);
    }

    private void EnsureBenderBufferCapacity(int required)
    {
        if (benderBuffer != null && benderBufferCapacity >= required)
        {
            return;
        }

        benderBuffer?.Release();
        benderBufferCapacity = Mathf.Max(InitialBenderCapacity, Mathf.Max(required, benderBufferCapacity * 2));
        benderBuffer = new ComputeBuffer(benderBufferCapacity, BenderStride, ComputeBufferType.Structured);
    }

    private static float ComputeFootprintRadius(Collider collider, Bounds bounds)
    {
        if (collider is CapsuleCollider || collider is SphereCollider)
        {
            return Mathf.Max(bounds.extents.x, bounds.extents.z);
        }

        return new Vector2(bounds.extents.x, bounds.extents.z).magnitude;
    }

    private void PullGrowthSettingsFromMaterial()
    {
        Vector4 heightRange = grassMaterial.GetVector(BladeHeightRangeID);
        Vector4 widthRange = grassMaterial.GetVector(BladeWidthRangeID);

        bladeHeightRange = new Vector2(heightRange.x, heightRange.y);
        bladeWidthRange = new Vector2(widthRange.x, widthRange.y);
        density = grassMaterial.GetFloat(DensityID);
        cellSize = grassMaterial.GetFloat(CellSizeID);
        drawDistance = grassMaterial.GetFloat(DrawDistanceID);
        falloffStartDistance = grassMaterial.GetFloat(FalloffStartDistanceID);
        patchSizeFalloff = grassMaterial.GetFloat(PatchSizeFalloffID);
        bladeSegments = Mathf.Clamp(Mathf.RoundToInt(grassMaterial.GetFloat(BladeSegmentsID)), 1, 6);
    }

    private bool TryInitialize()
    {
        if (terrain == null)
        {
            terrain = Terrain.activeTerrain;
        }

        if (terrain == null || terrain.terrainData == null)
        {
            Debug.LogError($"{nameof(GrassRenderer)}: no Terrain assigned and no active Terrain in the scene.", this);
            return false;
        }

        if (scatterCompute == null)
        {
            Debug.LogError($"{nameof(GrassRenderer)}: no scatter ComputeShader assigned (expected GrassScatter.compute).", this);
            return false;
        }

        if (grassMaterial == null)
        {
            Debug.LogError($"{nameof(GrassRenderer)}: no grass Material assigned (expected a material using Rebirth/Grass).", this);
            return false;
        }

        PullGrowthSettingsFromMaterial();

        if (!SystemInfo.supportsComputeShaders)
        {
            Debug.LogError($"{nameof(GrassRenderer)}: compute shaders are unsupported on this device.", this);
            return false;
        }

        scatterKernel = scatterCompute.FindKernel("ScatterGrass");
        bendMapKernel = scatterCompute.FindKernel("UpdateBendMap");

        ResolveGroundMossMask();

        cellSize = Mathf.Max(cellSize, 0.01f);
        falloffStartDistance = Mathf.Clamp(falloffStartDistance, 0.0f, drawDistance);
        bendRecoveryDuration = Mathf.Max(bendRecoveryDuration, 0.01f);
        bendMapResolution = Mathf.Clamp(Mathf.ClosestPowerOfTwo(bendMapResolution), 64, 4096);
        bladesPerSide = Mathf.Max(1, Mathf.CeilToInt(drawDistance * 2.0f / cellSize));

        long candidateCount = (long)bladesPerSide * bladesPerSide;
        if (candidateCount > MaxCandidateBlades)
        {
            Debug.LogError(
                $"{nameof(GrassRenderer)}: {candidateCount:N0} candidate blades exceeds the {MaxCandidateBlades:N0} budget. " +
                $"Raise cellSize ({cellSize}) or lower drawDistance ({drawDistance}).", this);
            return false;
        }

        bladeSegments = Mathf.Clamp(bladeSegments, 1, 6);
        bladeMesh = BuildBladeMesh(bladeSegments);

        bladeBuffer = new ComputeBuffer((int)candidateCount, BladeStride, ComputeBufferType.Append);
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);

        args[0] = bladeMesh.GetIndexCount(0);
        args[1] = 0;
        args[2] = bladeMesh.GetIndexStart(0);
        args[3] = bladeMesh.GetBaseVertex(0);
        args[4] = 0;
        argsBuffer.SetData(args);

        grassMaterial.SetBuffer(BladesID, bladeBuffer);

        bendMapPrevious = CreateClearedBendMap();
        bendMapCurrent = CreateClearedBendMap();
        bendMapOrigin = Vector2.zero;

        EnsureBenderBufferCapacity(0);
        EnsureBlockerBufferCapacity(0);

        Vector3 terrainSize = terrain.terrainData.size;
        renderBounds = new Bounds(
            terrain.transform.position + terrainSize * 0.5f,
            terrainSize + Vector3.one * bladeHeightRange.y);

        return true;
    }

    private RenderTexture CreateClearedBendMap()
    {
        RenderTexture map = new RenderTexture(bendMapResolution, bendMapResolution, 0, RenderTextureFormat.RGHalf)
        {
            name = "GrassBendMap",
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        map.Create();

        RenderTexture previouslyActive = RenderTexture.active;
        RenderTexture.active = map;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = previouslyActive;

        return map;
    }

    private Camera ResolveCamera()
    {
        return targetCamera != null ? targetCamera : Camera.main;
    }

    private void DispatchScatter(Camera camera)
    {
        TerrainData terrainData = terrain.terrainData;
        Vector3 cameraPosition = camera.transform.position;

        int originIndexX = Mathf.FloorToInt((cameraPosition.x - drawDistance) / cellSize);
        int originIndexZ = Mathf.FloorToInt((cameraPosition.z - drawDistance) / cellSize);

        GeometryUtility.CalculateFrustumPlanes(camera, frustumPlanes);
        for (int i = 0; i < frustumPlanes.Length; i++)
        {
            Plane plane = frustumPlanes[i];
            frustumPlaneVectors[i] = new Vector4(plane.normal.x, plane.normal.y, plane.normal.z, plane.distance);
        }

        bladeBuffer.SetCounterValue(0);

        scatterCompute.SetBuffer(scatterKernel, BladesID, bladeBuffer);
        scatterCompute.SetTexture(scatterKernel, HeightmapID, terrainData.heightmapTexture);
        scatterCompute.SetVector(TerrainPositionID, terrain.transform.position);
        scatterCompute.SetVector(TerrainSizeID, terrainData.size);
        scatterCompute.SetFloat(HeightmapResolutionID, terrainData.heightmapResolution);
        scatterCompute.SetInts(GridOriginIndexID, originIndexX, originIndexZ);
        scatterCompute.SetFloat(CellSizeID, cellSize);
        scatterCompute.SetInt(BladesPerSideID, bladesPerSide);
        scatterCompute.SetVector(CameraPositionID, cameraPosition);
        scatterCompute.SetFloat(DrawDistanceID, drawDistance);
        scatterCompute.SetFloat(FalloffStartDistanceID, falloffStartDistance);
        scatterCompute.SetFloat(DensityID, density);
        scatterCompute.SetVectorArray(FrustumPlanesID, frustumPlaneVectors);
        scatterCompute.SetVector(BladeHeightRangeID, bladeHeightRange);
        scatterCompute.SetVector(BladeWidthRangeID, bladeWidthRange);
        scatterCompute.SetFloat(PatchSizeFalloffID, patchSizeFalloff);

        scatterCompute.SetBuffer(scatterKernel, BlockersID, blockerBuffer);
        scatterCompute.SetInt(BlockerCountID, blockerList.Count);

        PushGroundMaskSettings();

        int groups = Mathf.CeilToInt(bladesPerSide / (float)ThreadGroupSize);
        scatterCompute.Dispatch(scatterKernel, groups, groups, 1);

        ComputeBuffer.CopyCount(bladeBuffer, argsBuffer, InstanceCountOffset);
    }

    private void PushGroundMaskSettings()
    {
        bool maskActive = restrictToGroundMoss && groundExposesMossMask;

        scatterCompute.SetFloat(GrassMaskEnabledID, maskActive ? 1.0f : 0.0f);
        if (!maskActive)
        {
            return;
        }

        for (int i = 0; i < GroundMaskSourceProperties.Length; i++)
        {
            scatterCompute.SetFloat(
                GroundMaskTargetIDs[i],
                groundMaterial.GetFloat(GroundMaskSourceProperties[i]));
        }

        scatterCompute.SetFloat(GrassMaskThresholdID, groundMossThreshold);
        scatterCompute.SetFloat(GrassMaskFeatherID, Mathf.Max(groundMossFeather, 1e-4f));
    }

    private void ResolveGroundMossMask()
    {
        groundMaterial = terrain.materialTemplate;
        groundExposesMossMask = groundMaterial != null;

        if (groundExposesMossMask)
        {
            foreach (string property in GroundMaskSourceProperties)
            {
                if (!groundMaterial.HasFloat(property))
                {
                    groundExposesMossMask = false;
                    break;
                }
            }
        }

        if (restrictToGroundMoss && !groundExposesMossMask)
        {
            Debug.LogWarning(
                $"{nameof(GrassRenderer)}: the terrain material " +
                $"({(groundMaterial == null ? "none" : groundMaterial.name)}) does not expose the moss mask " +
                $"properties from Ground_MaskBlend, so grass will grow everywhere. Assign that material to the " +
                $"Terrain or turn off Restrict To Ground Moss.", this);
        }
    }

    private void DrawBlades(Camera camera)
    {
        stopwatch.Restart();
        Graphics.DrawMeshInstancedIndirect(
            bladeMesh,
            0,
            grassMaterial,
            renderBounds,
            argsBuffer,
            0,
            null,
            ShadowCastingMode.Off,
            true,
            gameObject.layer,
            camera);
        stopwatch.Stop();

        if (frameIndex > WarmupFrames)
        {
            windowDrawTicks += stopwatch.ElapsedTicks;
            windowFrameTime += Time.unscaledDeltaTime;
            windowFrames++;
        }
    }

    private void ReportPeriodically()
    {
        if (!logDiagnosticsOnStart || reportsLogged >= MaxReports)
        {
            return;
        }

        frameIndex++;
        if (frameIndex <= WarmupFrames)
        {
            return;
        }

        if (windowFrames < SampleFrames)
        {
            return;
        }

        reportsLogged++;

        argsBuffer.GetData(args);
        double ticksToMs = 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        double drawCallMs = windowDrawTicks * ticksToMs / windowFrames;
        float frameMs = windowFrameTime / windowFrames * 1000.0f;

        Debug.Log(
            $"[GrassDiagnostics #{reportsLogged}] frame={frameMs:F2}ms ({(frameMs > 0.0f ? 1000.0f / frameMs : 0.0f):F0} fps) " +
            $"over {windowFrames} frames | blades={args[1]:N0} triangles={args[1] * (args[0] / 3):N0} " +
            $"drawCallCpu={drawCallMs:F3}ms cellSize={cellSize} bladeWidth={bladeWidthRange.x}-{bladeWidthRange.y}", this);

        windowFrames = 0;
        windowFrameTime = 0.0f;
        windowDrawTicks = 0;
    }

    private static Mesh BuildBladeMesh(int segments)
    {
        int vertexCount = segments * 2 + 1;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector3[] normals = new Vector3[vertexCount];
        Vector2[] uv = new Vector2[vertexCount];
        int[] triangles = new int[((segments - 1) * 2 + 1) * 3];

        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / segments;
            float halfWidth = 0.5f * Mathf.Lerp(1.0f, 0.4f, t);

            vertices[i * 2] = new Vector3(-halfWidth, t, 0.0f);
            vertices[i * 2 + 1] = new Vector3(halfWidth, t, 0.0f);
            uv[i * 2] = new Vector2(0.0f, t);
            uv[i * 2 + 1] = new Vector2(1.0f, t);
            normals[i * 2] = Vector3.back;
            normals[i * 2 + 1] = Vector3.back;
        }

        int tip = segments * 2;
        vertices[tip] = new Vector3(0.0f, 1.0f, 0.0f);
        uv[tip] = new Vector2(0.5f, 1.0f);
        normals[tip] = Vector3.back;

        int index = 0;
        for (int i = 0; i < segments - 1; i++)
        {
            int row = i * 2;
            triangles[index++] = row;
            triangles[index++] = row + 2;
            triangles[index++] = row + 1;
            triangles[index++] = row + 1;
            triangles[index++] = row + 2;
            triangles[index++] = row + 3;
        }

        int lastRow = (segments - 1) * 2;
        triangles[index++] = lastRow;
        triangles[index++] = tip;
        triangles[index] = lastRow + 1;

        Mesh mesh = new Mesh { name = "GrassBlade" };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(triangles, 0);
        mesh.bounds = new Bounds(new Vector3(0.0f, 0.5f, 0.0f), Vector3.one);

        return mesh;
    }

    private void ReleaseResources()
    {
        bladeBuffer?.Release();
        bladeBuffer = null;

        argsBuffer?.Release();
        argsBuffer = null;

        benderBuffer?.Release();
        benderBuffer = null;
        benderBufferCapacity = 0;

        blockerBuffer?.Release();
        blockerBuffer = null;
        blockerBufferCapacity = 0;

        ReleaseBendMap(ref bendMapPrevious);
        ReleaseBendMap(ref bendMapCurrent);

        if (bladeMesh != null)
        {
            Destroy(bladeMesh);
            bladeMesh = null;
        }
    }

    private static void ReleaseBendMap(ref RenderTexture map)
    {
        if (map == null)
        {
            return;
        }

        map.Release();
        Destroy(map);
        map = null;
    }
}
