#ifndef REBIRTH_VISION_DITHER_INCLUDED
#define REBIRTH_VISION_DITHER_INCLUDED

float _VisionEditorBypass;

float VisionBayer(uint2 pixel)
{
    static const float threshold[16] =
    {
         0.0 / 16.0,  8.0 / 16.0,  2.0 / 16.0, 10.0 / 16.0,
        12.0 / 16.0,  4.0 / 16.0, 14.0 / 16.0,  6.0 / 16.0,
         3.0 / 16.0, 11.0 / 16.0,  1.0 / 16.0,  9.0 / 16.0,
        15.0 / 16.0,  7.0 / 16.0, 13.0 / 16.0,  5.0 / 16.0
    };
    return threshold[(pixel.y & 3) * 4 + (pixel.x & 3)];
}

float VisionDitherAlpha(float2 pixel, float fade)
{
    fade = fade * (1.0 - saturate(_VisionEditorBypass));
    return (1.0 - fade) - VisionBayer(uint2(pixel)) - 1e-4;
}

void VisionDitherClip(float4 positionCS, float fade)
{
    clip(VisionDitherAlpha(positionCS.xy, fade));
}

void VisionDither_float(float4 ScreenPosition, float Fade, out float Alpha)
{
    Alpha = VisionDitherAlpha(ScreenPosition.xy * _ScreenParams.xy, Fade);
}

#endif
