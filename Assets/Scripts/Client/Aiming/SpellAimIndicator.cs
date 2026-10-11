using UnityEngine;

public class SpellAimIndicator : AimIndicator
{
    [Header("Filament")]
    [SerializeField] private float filamentWidth = 0.035f;
    [Range(2, 48)]
    [SerializeField] private int sampleCount = 16;
    [Range(0.0f, 1.0f)]
    [SerializeField] private float filamentNearAlpha = 0.05f;

    [Header("Sigil")]
    [SerializeField] private float sigilLineWidth = 0.05f;
    [SerializeField] private float sigilSpinSpeed = 24.0f;
    [Range(4, 16)]
    [SerializeField] private int runeTicks = 8;

    [Header("Colours")]
    [SerializeField] private Color schoolColor = new Color(0.722f, 0.306f, 0.878f, 0.85f);
    [SerializeField] private Color outOfRangeColor = new Color(0.612f, 0.580f, 0.518f, 0.4f);

    private LineRenderer filament;
    private LineRenderer sigilRing;
    private LineRenderer sigilCore;
    private LineRenderer[] runes;
    private Material lineMaterial;
    private Gradient filamentGradient;
    private Vector3[] samplePoints;
    private float spinDegrees;

    private readonly GradientColorKey[] gradientColorKeys = new GradientColorKey[2];
    private readonly GradientAlphaKey[] gradientAlphaKeys = new GradientAlphaKey[2];

    private const int RingSegments = 40;
    private const float SurfaceOffset = 0.03f;

    private void Awake()
    {
        Build();
    }

    public override void Hide()
    {
        if (filament == null) return;

        SetVisible(filament, false);
        SetVisible(sigilRing, false);
        SetVisible(sigilCore, false);
        foreach (LineRenderer rune in runes) SetVisible(rune, false);
    }

    public override void Show(in AimShot shot)
    {
        if (filament == null) return;

        Vector3[] points = GetSampleBuffer();
        ArcTrace sample = ArcTrace.Sweep(
            shot.Curve, shot.Duration, points,
            shot.SweepRadius, shot.SweepLayers, 2);

        Color color = sample.Landed ? schoolColor : outOfRangeColor;
        spinDegrees += sigilSpinSpeed * Time.deltaTime;

        DrawFilament(points, sample, color, shot.Charge);
        DrawSigil(sample, color, shot.Charge, shot.GroundRadius);
    }

    private void DrawFilament(Vector3[] points, in ArcTrace sample, Color color, float charge)
    {
        SetVisible(filament, true);

        filament.widthMultiplier = filamentWidth;
        filament.positionCount = sample.PointCount;
        for (int index = 0; index < sample.PointCount; index++)
        {
            filament.SetPosition(index, points[index]);
        }

        Color near = color;
        near.a = color.a * filamentNearAlpha;
        Color far = color;
        far.a = color.a * (0.35f + 0.65f * charge);

        gradientColorKeys[0] = new GradientColorKey(near, 0.0f);
        gradientColorKeys[1] = new GradientColorKey(far, 1.0f);
        gradientAlphaKeys[0] = new GradientAlphaKey(near.a, 0.0f);
        gradientAlphaKeys[1] = new GradientAlphaKey(far.a, 1.0f);

        filamentGradient.SetKeys(gradientColorKeys, gradientAlphaKeys);
        filament.colorGradient = filamentGradient;
    }

    private void DrawSigil(in ArcTrace sample, Color color, float charge, float radius)
    {
        bool visible = sample.Landed;

        SetVisible(sigilRing, visible);
        SetVisible(sigilCore, visible);
        foreach (LineRenderer rune in runes) SetVisible(rune, visible);

        if (!visible) return;

        Quaternion orientation = Quaternion.LookRotation(sample.LandingNormal);
        Vector3 centre = sample.LandingPoint + sample.LandingNormal * SurfaceOffset;

        Color ringColor = color;
        ringColor.a = color.a * (0.45f + 0.55f * charge);

        DrawRing(sigilRing, centre, orientation, radius, sigilLineWidth, ringColor);
        DrawRing(sigilCore, centre, orientation, radius * 0.34f, sigilLineWidth * 0.6f, ringColor);

        for (int index = 0; index < runes.Length; index++)
        {
            float angle = (index / (float)runes.Length) * 360.0f + spinDegrees;
            float inner = radius * 0.78f;
            float outer = radius * (index % 2 == 0 ? 1.14f : 1.05f);

            Vector3 direction = orientation * new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0.0f);

            runes[index].widthMultiplier = sigilLineWidth * 0.8f;
            runes[index].positionCount = 2;
            runes[index].SetPosition(0, centre + direction * inner);
            runes[index].SetPosition(1, centre + direction * outer);
            runes[index].startColor = ringColor;
            runes[index].endColor = ringColor;
        }
    }

    private static void DrawRing(LineRenderer ring, Vector3 centre, Quaternion orientation,
                                 float radius, float width, Color color)
    {
        ring.widthMultiplier = width;
        ring.positionCount = RingSegments;

        for (int segment = 0; segment < RingSegments; segment++)
        {
            float angle = (segment / (float)RingSegments) * Mathf.PI * 2.0f;
            Vector3 offset = orientation * new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0.0f);
            ring.SetPosition(segment, centre + offset * radius);
        }

        ring.startColor = color;
        ring.endColor = color;
    }

    private Vector3[] GetSampleBuffer()
    {
        int required = Mathf.Max(2, sampleCount);
        if (samplePoints == null || samplePoints.Length != required)
        {
            samplePoints = new Vector3[required];
        }

        return samplePoints;
    }

    private void Build()
    {
        lineMaterial = new Material(Shader.Find("Rebirth/AimLine"));
        filamentGradient = new Gradient();

        filament = CreateLine("SpellFilament");
        sigilRing = CreateLine("SpellSigilRing");
        sigilRing.loop = true;
        sigilCore = CreateLine("SpellSigilCore");
        sigilCore.loop = true;

        runes = new LineRenderer[Mathf.Max(4, runeTicks)];
        for (int index = 0; index < runes.Length; index++)
        {
            runes[index] = CreateLine("SpellRune");
        }
    }

    private LineRenderer CreateLine(string name)
    {
        GameObject lineObject = new GameObject(name);
        lineObject.layer = gameObject.layer;
        lineObject.transform.SetParent(transform, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.sharedMaterial = lineMaterial;
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

    private void OnDestroy()
    {
        if (lineMaterial != null) Destroy(lineMaterial);
    }
}
