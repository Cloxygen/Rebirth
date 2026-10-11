#ifndef REBIRTH_GROUND_MASK_INCLUDED
#define REBIRTH_GROUND_MASK_INCLUDED

float _GroundWorldTileSize;
float _GroundPatchScale;
float _GroundEdgeRaggedness;
float _GroundMossAmount;
float _GroundMacroVariationScale;
float _GroundMacroVariationAmount;
float _GroundBlendSharpness;

float2 GradientNoiseDirection(float2 p)
{
    p = p % 289;
    float x = (34 * p.x + 1) * p.x % 289 + p.y;
    x = (34 * x + 1) * x % 289;
    x = frac(x / 41) * 2 - 1;
    return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
}

float GradientNoiseValue(float2 p)
{
    float2 ip = floor(p);
    float2 fp = frac(p);
    float d00 = dot(GradientNoiseDirection(ip), fp);
    float d01 = dot(GradientNoiseDirection(ip + float2(0, 1)), fp - float2(0, 1));
    float d10 = dot(GradientNoiseDirection(ip + float2(1, 0)), fp - float2(1, 0));
    float d11 = dot(GradientNoiseDirection(ip + float2(1, 1)), fp - float2(1, 1));
    fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
    return lerp(lerp(d00, d10, fp.x), lerp(d01, d11, fp.x), fp.y);
}

float GradientNoise(float2 uv, float scale)
{
    return GradientNoiseValue(uv * scale) + 0.5;
}

float Remap(float value, float2 inMinMax, float2 outMinMax)
{
    return outMinMax.x
         + (value - inMinMax.x) * (outMinMax.y - outMinMax.x) / (inMinMax.y - inMinMax.x);
}

float GroundMossMask(float2 worldXZ)
{
    float2 worldUV = worldXZ / _GroundWorldTileSize;

    float macro = GradientNoise(worldUV, _GroundPatchScale * 1.2);
    float detail = GradientNoise(worldUV, _GroundPatchScale * 2.8);
    float edgeRaggedness = GradientNoise(worldUV, _GroundPatchScale * 7.0);

    float combinedOctaves = lerp(macro, detail, 0.5);
    float raggedEdges = lerp(combinedOctaves, edgeRaggedness, _GroundEdgeRaggedness);

    float macroVariation = Remap(
        GradientNoise(worldUV, _GroundMacroVariationScale),
        float2(0.0, 1.0),
        float2(-1.0, 1.0)) * _GroundMacroVariationAmount;

    float sharpened = (raggedEdges + _GroundMossAmount + macroVariation) * _GroundBlendSharpness;

    return saturate(Remap(sharpened, float2(0.4, 0.6), float2(0.0, 1.0)));
}

float GroundMossColor2Weight(float2 worldXZ)
{
    float2 worldUV = worldXZ / _GroundWorldTileSize;
    return saturate(GradientNoise(worldUV, _GroundMacroVariationScale));
}

#endif
