using UnityEngine;

public class BowAimIndicator : AimIndicator
{
    [Header("Lines")]
    [SerializeField] private float aimLineWidth = 0.09f;
    [Header("Scatter Cone")]
    [SerializeField] private float spreadLineWidth = 0.035f;
    [Range(0.0f, 1.0f)]
    [SerializeField] private float spreadLineAlpha = 0.35f;
    [SerializeField] private float spreadLineLength = 1.0f;
    [Range(8, 64)]
    [SerializeField] private int arcSampleCount = 32;
    [Range(2, 16)]
    [SerializeField] private int arcSweepSegments = 8;

    [Header("Dashes")]
    [SerializeField] private float dashLength = 0.8f;
    [SerializeField] private float dashScrollSpeed = 5.0f;

    [Header("Impact Ring")]
    [SerializeField] private float impactRingRadius = 0.35f;
    [SerializeField] private float progressRingGap = 0.12f;
    [SerializeField] private float progressRingWidth = 0.06f;
    [Range(8, 96)]
    [SerializeField] private int progressRingSegments = 48;

    [Header("Colours")]
    [SerializeField] private Color restColor = new Color(0.612f, 0.580f, 0.518f, 0.28f);
    [SerializeField] private Color fullChargeColor = new Color(0.937f, 0.906f, 0.839f, 0.9f);
    [SerializeField] private Color onTargetColor = new Color(0.906f, 0.255f, 0.290f, 1.0f);

    private LineRenderer[] arcs;
    private Material[] arcMaterials;
    private Gradient[] arcGradients;
    private LineRenderer impactRing;
    private Material impactRingMaterial;
    private LineRenderer progressRing;
    private Material progressRingMaterial;
    private Texture2D dashTexture;
    private Vector3[] samplePoints;
    private float dashScrollOffset;

    private readonly GradientColorKey[] gradientColorKeys = new GradientColorKey[2];
    private readonly GradientAlphaKey[] gradientAlphaKeys = new GradientAlphaKey[3];

    private const float MinimumVisibleSpreadDegrees = 0.25f;

    private const float ArcFadeInFraction = 0.18f;
    private const float ArcStartWidthFraction = 0.25f;
    private const float ArcTaperEndFraction = 0.3f;
    private const int ImpactRingSegments = 24;
    private const float ImpactRingSurfaceOffset = 0.03f;

    private void Awake()
    {
        Build();
    }

    public override void Hide()
    {
        if (arcs == null) return;

        foreach (LineRenderer arc in arcs) SetVisible(arc, false);
        SetVisible(impactRing, false);
        SetVisible(progressRing, false);
    }

    public override void Show(in AimShot shot)
    {
        if (arcs == null) return;

        bool showCone = shot.SpreadDegrees > MinimumVisibleSpreadDegrees;

        SetVisible(arcs[0], true);
        SetVisible(arcs[1], showCone);
        SetVisible(arcs[2], showCone);

        dashScrollOffset = Mathf.Repeat(
            dashScrollOffset - Time.deltaTime * dashScrollSpeed / Mathf.Max(0.01f, dashLength), 1.0f);

        ArcTrace aimSample = PlotArc(0, shot, 0.0f, aimLineWidth);

        if (showCone)
        {
            Vector3 aimEnd = samplePoints[Mathf.Max(0, aimSample.PointCount - 1)];
            PlotSpreadEdge(1, shot, -shot.SpreadDegrees, aimEnd);
            PlotSpreadEdge(2, shot, shot.SpreadDegrees, aimEnd);
        }

        UpdateImpactRing(aimSample, shot.Charge);
    }

    private void PlotSpreadEdge(int arcIndex, in AimShot shot, float yawDegrees, Vector3 aimEnd)
    {
        LineRenderer edge = arcs[arcIndex];

        Vector3 toEnd = (aimEnd - shot.LaunchPosition) * spreadLineLength;
        Vector3 flat = Quaternion.AngleAxis(yawDegrees, Vector3.up) * new Vector3(toEnd.x, 0.0f, toEnd.z);

        edge.widthMultiplier = spreadLineWidth;
        edge.positionCount = 2;
        edge.SetPosition(0, shot.LaunchPosition);
        edge.SetPosition(1, shot.LaunchPosition + flat + Vector3.up * toEnd.y);

        Color color = GetColor(false, shot.Charge);
        color.a *= spreadLineAlpha;
        ApplyColor(edge, arcGradients[arcIndex], color);
    }

    private ArcTrace PlotArc(int arcIndex, in AimShot shot, float yawDegrees, float width)
    {
        LineRenderer arc = arcs[arcIndex];
        Vector3[] points = GetSampleBuffer();

        ArcTrace sample = ArcTrace.Sweep(
            shot.Fanned(yawDegrees), shot.Duration, points,
            shot.SweepRadius, shot.SweepLayers, arcSweepSegments);

        arc.widthMultiplier = width;
        arc.positionCount = sample.PointCount;
        for (int index = 0; index < sample.PointCount; index++)
        {
            arc.SetPosition(index, points[index]);
        }

        Material material = arcMaterials[arcIndex];
        material.mainTextureScale = new Vector2(sample.Length / Mathf.Max(0.01f, dashLength), 1.0f);
        material.mainTextureOffset = new Vector2(dashScrollOffset, 0.0f);

        ApplyColor(arc, arcGradients[arcIndex], GetColor(sample.EndsOnTarget, shot.Charge));
        return sample;
    }

    private void UpdateImpactRing(in ArcTrace aimSample, float charge)
    {
        SetVisible(impactRing, aimSample.Landed);

        if (!aimSample.Landed)
        {
            SetVisible(progressRing, false);
            return;
        }

        Quaternion orientation = Quaternion.LookRotation(aimSample.LandingNormal);
        Vector3 centre = aimSample.LandingPoint + aimSample.LandingNormal * ImpactRingSurfaceOffset;

        for (int segment = 0; segment < ImpactRingSegments; segment++)
        {
            float angle = (float)segment / ImpactRingSegments * Mathf.PI * 2.0f;
            Vector3 offset = orientation * new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0.0f);
            impactRing.SetPosition(segment, centre + offset * impactRingRadius);
        }

        Color ringColor = GetColor(aimSample.EndsOnTarget, charge);
        impactRing.widthMultiplier = aimLineWidth;
        impactRing.startColor = ringColor;
        impactRing.endColor = ringColor;

        DrawProgressRing(centre, orientation, charge, GetColor(aimSample.EndsOnTarget, 1.0f));
    }

    private void DrawProgressRing(Vector3 centre, Quaternion orientation, float charge, Color color)
    {
        charge = Mathf.Clamp01(charge);

        int segments = Mathf.Max(2, progressRingSegments);
        int wholeSegments = Mathf.FloorToInt(charge * segments);

        SetVisible(progressRing, charge > 0.0f);
        if (charge <= 0.0f) return;

        float radius = Mathf.Max(0.0f, impactRingRadius - progressRingGap);
        float sweep = charge * Mathf.PI * 2.0f;

        progressRing.widthMultiplier = progressRingWidth;
        progressRing.positionCount = wholeSegments + 2;

        for (int index = 0; index <= wholeSegments + 1; index++)
        {
            float angle = index <= wholeSegments
                ? index / (float)segments * Mathf.PI * 2.0f
                : sweep;

            Vector3 offset = orientation * new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0.0f);
            progressRing.SetPosition(index, centre + offset * radius);
        }

        progressRing.startColor = color;
        progressRing.endColor = color;
    }

    private Color GetColor(bool endsOnTarget, float charge)
    {
        return endsOnTarget ? onTargetColor : Color.Lerp(restColor, fullChargeColor, charge);
    }

    private void ApplyColor(LineRenderer line, Gradient gradient, Color color)
    {
        gradientColorKeys[0] = new GradientColorKey(color, 0.0f);
        gradientColorKeys[1] = new GradientColorKey(color, 1.0f);

        gradientAlphaKeys[0] = new GradientAlphaKey(0.0f, 0.0f);
        gradientAlphaKeys[1] = new GradientAlphaKey(color.a, ArcFadeInFraction);
        gradientAlphaKeys[2] = new GradientAlphaKey(color.a, 1.0f);

        gradient.SetKeys(gradientColorKeys, gradientAlphaKeys);
        line.colorGradient = gradient;
    }

    private Vector3[] GetSampleBuffer()
    {
        int requiredLength = Mathf.Max(2, arcSampleCount);
        if (samplePoints == null || samplePoints.Length != requiredLength)
        {
            samplePoints = new Vector3[requiredLength];
        }

        return samplePoints;
    }

    private void Build()
    {
        dashTexture = CreateDashTexture();

        AnimationCurve taper = new AnimationCurve(
            new Keyframe(0.0f, ArcStartWidthFraction),
            new Keyframe(ArcTaperEndFraction, 1.0f),
            new Keyframe(1.0f, 1.0f));

        arcs = new LineRenderer[3];
        arcMaterials = new Material[arcs.Length];
        arcGradients = new Gradient[arcs.Length];

        for (int arcIndex = 0; arcIndex < arcs.Length; arcIndex++)
        {
            arcMaterials[arcIndex] = new Material(GetAimLineShader());
            if (arcIndex == 0) arcMaterials[arcIndex].mainTexture = dashTexture;
            arcGradients[arcIndex] = new Gradient();

            LineRenderer arc = CreateLine(arcIndex == 0 ? "AimArc" : "AimSpreadArc", arcMaterials[arcIndex]);
            arc.textureMode = LineTextureMode.Stretch;
            arc.widthCurve = taper;

            arcs[arcIndex] = arc;
        }

        impactRingMaterial = new Material(GetAimLineShader());
        impactRing = CreateLine("AimImpactRing", impactRingMaterial);
        impactRing.loop = true;
        impactRing.positionCount = ImpactRingSegments;

        progressRingMaterial = new Material(GetAimLineShader());
        progressRing = CreateLine("AimProgressRing", progressRingMaterial);
    }

    private LineRenderer CreateLine(string name, Material material)
    {
        GameObject lineObject = new GameObject(name);
        lineObject.layer = gameObject.layer;
        lineObject.transform.SetParent(transform, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.sharedMaterial = material;
        line.alignment = LineAlignment.View;
        line.numCapVertices = 2;
        line.numCornerVertices = 2;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;

        lineObject.SetActive(false);
        return line;
    }

    private static void SetVisible(LineRenderer line, bool visible)
    {
        if (line != null && line.gameObject.activeSelf != visible)
        {
            line.gameObject.SetActive(visible);
        }
    }

    private static Shader GetAimLineShader()
    {
        return Shader.Find("Rebirth/AimLine");
    }

    private static Texture2D CreateDashTexture()
    {
        const int Width = 32;
        const int DashPixels = 20;
        const int RampPixels = 3;

        Texture2D texture = new Texture2D(Width, 1, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear
        };

        for (int x = 0; x < Width; x++)
        {
            float alpha;
            if (x >= DashPixels) alpha = 0.0f;
            else if (x < RampPixels) alpha = (x + 1.0f) / (RampPixels + 1.0f);
            else if (x >= DashPixels - RampPixels) alpha = (DashPixels - x) / (RampPixels + 1.0f);
            else alpha = 1.0f;

            texture.SetPixel(x, 0, new Color(1.0f, 1.0f, 1.0f, alpha));
        }

        texture.Apply();
        return texture;
    }

    private void OnDestroy()
    {
        if (arcMaterials != null)
        {
            foreach (Material material in arcMaterials)
            {
                if (material != null) Destroy(material);
            }
        }

        if (impactRingMaterial != null) Destroy(impactRingMaterial);
        if (progressRingMaterial != null) Destroy(progressRingMaterial);
        if (dashTexture != null) Destroy(dashTexture);
    }
}
