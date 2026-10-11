#ifndef PARALLAX_OFFSET_INCLUDED
#define PARALLAX_OFFSET_INCLUDED

void ParallaxOffset_float(
    float Height,
    float Strength,
    float3 ViewDirTangent,
    float2 UV,
    out float2 OffsetUV)
{
    float3 viewDir = normalize(ViewDirTangent);

    float safeZ = max(viewDir.z, 0.0001);

    float2 offset = (viewDir.xy / safeZ) * Height * Strength;

    OffsetUV = UV - offset;
}

#endif
