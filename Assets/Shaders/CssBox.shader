Shader "RebirthUI/CssBox"
{
    Properties
    {
        _WidgetSize ("Widget Size (px)", Vector) = (430, 62, 0, 0)
        _Pad        ("Padding (px)", Float) = 24
        _Radius     ("Corner Radius (px)", Float) = 6
        _ClipRadius ("Clip Circle Radius (px, 0=off)", Float) = 0

        _Shape      ("Shape (0 rect, 1 triangle, 2 arrow)", Float) = 0

        _Hole       ("Hole Radius (px, 0 = solid)", Float) = 0

        _LevelOn    ("Level Mode On", Float) = 0
        _Level      ("Fill Level (0..1)", Float) = 1
        _LevelAxis  ("Level Axis (0 vertical, 1 horizontal)", Float) = 0
        _LevelMin   ("Level Range Start (0..1)", Float) = 0

        _SweepOn    ("Sweep Mode On", Float) = 0
        _Sweep      ("Sweep (0..1 turns)", Float) = 0

        _SwirlOn       ("Swirl On", Float) = 0
        _SwirlStrength ("Swirl Strength (0..1)", Float) = 0.6
        _SwirlScale    ("Swirl Noise Scale", Float) = 3
        _SwirlSpeed    ("Swirl Rotation Speed", Float) = 3.5
        _SwirlParallax ("Swirl Parallax Depth Ratio", Float) = 0.8
        _SwirlSpherical("Swirl Spherical Amount (barrel distortion)", Float) = 1
        _SwirlLatitude ("Swirl Differential Rotation (0=uniform, 1=full)", Float) = 1
        _SwirlTime     ("Swirl Time (seconds)", Float) = 0
        _SwirlWarp     ("Swirl Domain Warp (0=off, plain fbm)", Float) = 0.45

        _RimStrength   ("Rim Light Strength (0=off)", Float) = 0
        _RimWidth      ("Rim Light Width (px)", Float) = 16

        _SurfaceStrength ("Surface Highlight Strength (0=off)", Float) = 0
        _SurfaceWidth    ("Surface Highlight Width (px)", Float) = 3

        _FillMode   ("Fill Mode", Float) = 1
        _FillColor  ("Fill / Stop A", Color) = (0.16, 0.15, 0.13, 1)
        _FillColor2 ("Stop B", Color) = (0.07, 0.06, 0.09, 1)
        _GradAngle  ("Linear Angle (deg)", Float) = 90
        _GradCenter ("Radial Centre (0..1 of box)", Vector) = (0.5, 0.5, 0, 0)
        _GradRadius ("Radial Radii (frac of box)", Vector) = (0.5, 0.5, 0, 0)
        _GradStart  ("Stop A pos (0..1)", Float) = 0
        _GradEnd    ("Stop B pos (0..1)", Float) = 1

        _Offset     ("Shadow Offset (px)", Vector) = (0, 4.32, 0, 0)
        _Blur       ("Shadow Blur-radius (px)", Float) = 10
        _ShadowColor("Shadow Color", Color) = (0, 0, 0, 0.5)

        _InsetOffset("Inset Offset (px)", Vector) = (0, 2.5, 0, 0)
        _InsetBlur  ("Inset Blur (px)", Float) = 5
        _InsetSpread("Inset Spread (px)", Float) = 0
        _InsetColor ("Inset Color", Color) = (0, 0, 0, 0.85)

        _Inset2Offset("Inset2 Offset (px)", Vector) = (0, 0, 0, 0)
        _Inset2Blur  ("Inset2 Blur (px)", Float) = 0
        _Inset2Spread("Inset2 Spread (px)", Float) = 0
        _Inset2Color ("Inset2 Color", Color) = (0, 0, 0, 0)

        _Inset3Offset("Inset3 Offset (px)", Vector) = (0, 0, 0, 0)
        _Inset3Blur  ("Inset3 Blur (px)", Float) = 0
        _Inset3Spread("Inset3 Spread (px)", Float) = 0
        _Inset3Color ("Inset3 Color", Color) = (0, 0, 0, 0)

        _Ov1On    ("Overlay1 On", Float) = 0
        _Ov1Mode  ("Overlay1 Mode", Float) = 2
        _Ov1ColA  ("Overlay1 A", Color) = (1,1,1,0)
        _Ov1ColB  ("Overlay1 B", Color) = (0,0,0,0)
        _Ov1Geo   ("Overlay1 Geo", Vector) = (0.5,0.5,0.5,0.5)
        _Ov1Start ("Overlay1 Start", Float) = 0
        _Ov1End   ("Overlay1 End", Float) = 1

        _Ov2On    ("Overlay2 On", Float) = 0
        _Ov2Mode  ("Overlay2 Mode", Float) = 2
        _Ov2ColA  ("Overlay2 A", Color) = (0,0,0,0)
        _Ov2ColB  ("Overlay2 B", Color) = (0,0,0,0)
        _Ov2Geo   ("Overlay2 Geo", Vector) = (0.5,0.5,0.5,0.5)
        _Ov2Start ("Overlay2 Start", Float) = 0
        _Ov2End   ("Overlay2 End", Float) = 1
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _WidgetSize;  float _Pad;  float _Radius;  float _ClipRadius;  float _Shape;  float _Hole;
            float  _LevelOn;     float _Level;  float _LevelAxis;  float _LevelMin;
            float  _SweepOn;     float _Sweep;
            float  _FillMode;    fixed4 _FillColor; fixed4 _FillColor2;
            float  _GradAngle;   float4 _GradCenter; float4 _GradRadius; float _GradStart; float _GradEnd;
            float4 _Offset;      float _Blur;  fixed4 _ShadowColor;
            float4 _InsetOffset; float _InsetBlur; float _InsetSpread; fixed4 _InsetColor;
            float4 _Inset2Offset; float _Inset2Blur; float _Inset2Spread; fixed4 _Inset2Color;
            float4 _Inset3Offset; float _Inset3Blur; float _Inset3Spread; fixed4 _Inset3Color;
            float  _Ov1On; float _Ov1Mode; fixed4 _Ov1ColA; fixed4 _Ov1ColB; float4 _Ov1Geo; float _Ov1Start; float _Ov1End;
            float  _Ov2On; float _Ov2Mode; fixed4 _Ov2ColA; fixed4 _Ov2ColB; float4 _Ov2Geo; float _Ov2Start; float _Ov2End;
            float  _SwirlOn; float _SwirlStrength; float _SwirlScale; float _SwirlSpeed;
            float  _SwirlParallax; float _SwirlSpherical; float _SwirlLatitude; float _SwirlTime;
            float  _SwirlWarp; float _RimStrength; float _RimWidth;
            float  _SurfaceStrength; float _SurfaceWidth;

            static const float PI = 3.14159265358979323846;

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }
            float vnoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i),               b2 = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1)), d = hash21(i + float2(1, 1));
                return lerp(lerp(a, b2, f.x), lerp(c, d, f.x), f.y);
            }
            static const float2x2 OCT_ROT = float2x2(0.80, -0.60, 0.60, 0.80);

            float liquidTex(float2 p)
            {
                float v = 0.0, amp = 0.5, norm = 0.0;
                for (int i = 0; i < 3; i++)
                {
                    v += amp * vnoise(p);
                    norm += amp;
                    p = mul(OCT_ROT, p) * 2.03; amp *= 0.5;
                }
                return v / norm;
            }

            float2 liquidWarp(float2 p)
            {
                return float2(liquidTex(p), liquidTex(p + float2(5.2, 1.3))) - 0.5;
            }

            float2 barrelDistortion(float2 h, float spherical)
            {
                float r2 = dot(h, h);
                return (1.0 + r2 * (spherical * sqrt(r2))) * h;
            }

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f     { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

            float sdRoundBox(float2 p, float2 b, float r)
            {
                float2 q = abs(p) - b + r;
                return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r;
            }

            float sdTriangleUp(float2 p, float2 b)
            {
                float2 q  = float2(b.x, 2.0 * b.y);
                float2 pt = float2(abs(p.x), p.y + b.y);
                float2 a = pt - q * clamp(dot(pt, q) / dot(q, q), 0.0, 1.0);
                float2 c = pt - q * float2(clamp(pt.x / q.x, 0.0, 1.0), 1.0);
                float s = -sign(q.y);
                float2 d = min(float2(dot(a, a), s * (pt.x * q.y - pt.y * q.x)),
                               float2(dot(c, c), s * (pt.y - q.y)));
                return -sqrt(d.x) * sign(d.y);
            }

            float sdArrowUp(float2 p, float2 b)
            {
                float nh = 0.35 * b.y;
                float notch = sdTriangleUp(p - float2(0.0, b.y - nh), float2(b.x, nh));
                return max(sdTriangleUp(p, b), -notch);
            }

            float gaussian(float x, float sigma)
            {
                return exp(-(x * x) / (2.0 * sigma * sigma)) / (sqrt(2.0 * PI) * sigma);
            }

            float2 erf2(float2 x)
            {
                float2 s = sign(x); float2 a = abs(x);
                float2 t = 1.0 + (0.278393 + (0.230389 + 0.078108 * (a * a)) * a) * a;
                t = t * t;
                return s - s / (t * t);
            }

            float roundedBoxShadowX(float x, float y, float sigma, float corner, float2 b)
            {
                float delta = min(b.y - corner - abs(y), 0.0);
                float curved = b.x - corner + sqrt(max(0.0, corner * corner - delta * delta));
                float2 integral = 0.5 + 0.5 * erf2((x + float2(-curved, curved)) * (sqrt(0.5) / sigma));
                return integral.y - integral.x;
            }

            float roundedBoxShadow(float2 b, float2 pt, float sigma, float corner)
            {
                float low = pt.y - b.y, high = pt.y + b.y;
                float start = clamp(-3.0 * sigma, low, high);
                float end   = clamp( 3.0 * sigma, low, high);
                float dy = (end - start) / 4.0;
                float y = start + dy * 0.5;
                float value = 0.0;
                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    value += roundedBoxShadowX(pt.x, pt.y - y, sigma, corner, b) * gaussian(y, sigma) * dy;
                    y += dy;
                }
                return value;
            }

            float4 gammaLerp(float4 ca, float4 cb, float t)
            {
                float3 rgb = GammaToLinearSpace(lerp(LinearToGammaSpace(ca.rgb), LinearToGammaSpace(cb.rgb), t));
                return float4(rgb, lerp(ca.a, cb.a, t));
            }

            float4 gammaLerpSrgb(float4 ca, float4 cb, float t)
            {
                return float4(lerp(LinearToGammaSpace(ca.rgb), LinearToGammaSpace(cb.rgb), t), lerp(ca.a, cb.a, t));
            }

            float gradT(float mode, float4 geo, float start, float end, float2 p, float2 b)
            {
                float t;
                if (mode < 1.5)
                {
                    float ang = radians(geo.x);
                    float2 d = float2(cos(ang), sin(ang));
                    float extent = abs(b.x * d.x) + abs(b.y * d.y);
                    t = dot(p, d) / (2.0 * extent) + 0.5;
                }
                else
                {
                    float2 c   = (geo.xy - 0.5) * _WidgetSize.xy;
                    float2 rad = max(geo.zw * _WidgetSize.xy, 1e-3);
                    t = length((p - c) / rad);
                }
                return saturate((t - start) / max(end - start, 1e-4));
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 rtSize = _WidgetSize.xy + 2.0 * _Pad;
                float2 uv = i.uv;
                #if UNITY_UV_STARTS_AT_TOP
                    uv.y = 1.0 - uv.y;
                #endif
                float2 p = (uv - 0.5) * rtSize;
                float2 b = _WidgetSize.xy * 0.5;
                float  r = min(_Radius, min(b.x, b.y));

                float df = (_Shape > 1.5) ? sdArrowUp(p, b)
                         : (_Shape > 0.5) ? sdTriangleUp(p, b)
                                          : sdRoundBox(p, b, r);

                if (_Hole > 0.0) df = max(df, _Hole - length(p));

                float shape = 1.0 - smoothstep(-(fwidth(df) + 1e-4), (fwidth(df) + 1e-4), df);

                bool  vertLevel = (_LevelOn > 0.5) && (_LevelAxis < 0.5);
                float surface   = b.y * (1.0 - 2.0 * _Level);
                float edgeX     = b.x * (2.0 * _Level - 1.0);

                float2 pg = p;
                if (vertLevel) pg.y = (p.y - surface) / max(_Level, 1e-3) - b.y;

                float outSigma = max(_Blur * 0.5, 0.5);
                float outerA = _ShadowColor.a * saturate(roundedBoxShadow(b, p - _Offset.xy, outSigma, r));

                float4 baseGeo = (_FillMode < 1.5) ? float4(_GradAngle, 0, 0, 0)
                                                   : float4(_GradCenter.xy, _GradRadius.xy);
                float4 fill;
                if (_FillMode < 0.5)
                    fill = float4(LinearToGammaSpace(_FillColor.rgb), _FillColor.a);
                else
                    fill = gammaLerpSrgb(_FillColor, _FillColor2,
                                         gradT(_FillMode, baseGeo, _GradStart, _GradEnd, pg, b));

                if (_Ov1On > 0.5)
                {
                    float4 ov = gammaLerpSrgb(_Ov1ColA, _Ov1ColB,
                                              gradT(_Ov1Mode, _Ov1Geo, _Ov1Start, _Ov1End, pg, b));
                    fill.rgb = fill.rgb * (1.0 - ov.a) + ov.rgb * ov.a;
                    fill.a   = ov.a + fill.a * (1.0 - ov.a);
                }
                if (_Ov2On > 0.5)
                {
                    float4 ov = gammaLerpSrgb(_Ov2ColA, _Ov2ColB,
                                              gradT(_Ov2Mode, _Ov2Geo, _Ov2Start, _Ov2End, pg, b));
                    fill.rgb = fill.rgb * (1.0 - ov.a) + ov.rgb * ov.a;
                    fill.a   = ov.a + fill.a * (1.0 - ov.a);
                }
                if (_SwirlOn > 0.5)
                {
                    float2 h = p / (2.0 * max(b.x, 1.0));

                    float  yy    = clamp(h.y * 2.0, -1.0, 1.0);
                    float  band  = pow(saturate(1.0 - yy * yy), 0.35);
                    float  bandc = lerp(1.0, max(band, 0.45), saturate(_SwirlLatitude));

                    float2 bd  = barrelDistortion(h, _SwirlSpherical);
                    float2 suv = float2(bd.x / bandc, bd.y) * _SwirlScale;

                    float t  = _SwirlTime * _SwirlSpeed;
                    float d2 = 1.0 + _SwirlParallax;
                    float d3 = 1.0 + 2.0 * _SwirlParallax;

                    if (_SwirlWarp > 1e-4)
                        suv += _SwirlWarp * liquidWarp(suv * 0.55 + t * float2(-0.04, -0.05));

                    float c1 = liquidTex(suv               + t * float2(-0.08, -0.12));
                    float c2 = liquidTex(suv * 1.30 + 17.3 + t * float2(-0.10, -0.08) / d2);
                    float c3 = liquidTex(suv * 0.80 + 41.7 + t * float2(-0.06, -0.02) / d3);

                    float n = saturate((c1 + c2) * 0.62 * (c3 * 1.9));
                    n = saturate((n - 0.12) / 0.50);

                    float shade = lerp(1.0 - _SwirlStrength * 0.55,
                                       1.0 + _SwirlStrength * 0.85, n);
                    fill.rgb = saturate(fill.rgb * shade);
                }

                if (_RimStrength > 1e-4)
                {
                    float rimT = saturate(1.0 + df / max(_RimWidth, 1e-3));
                    fill.rgb = saturate(fill.rgb * (1.0 + _RimStrength * rimT * rimT));
                }

                if (_LevelOn > 0.5 && _SurfaceStrength > 1e-4)
                {
                    float depth = (_LevelAxis < 0.5) ? (p.y - surface) : (edgeX - p.x);
                    float surfT = 1.0 - saturate(depth / max(_SurfaceWidth, 1e-3));
                    fill.rgb = saturate(fill.rgb * (1.0 + _SurfaceStrength * surfT * surfT));
                }

                float fillA = fill.a * shape;

                float inSigma = max(_InsetBlur * 0.5, 0.5);
                float2 ib = max(b - _InsetSpread, 1.0);
                float inR = min(r, min(ib.x, ib.y));
                float insetMask = 1.0 - saturate(roundedBoxShadow(ib, p - _InsetOffset.xy, inSigma, inR));
                float insetA = _InsetColor.a * insetMask * shape;

                float in2Sigma = max(_Inset2Blur * 0.5, 0.5);
                float2 ib2 = max(b - _Inset2Spread, 1.0);
                float in2R = min(r, min(ib2.x, ib2.y));
                float inset2Mask = 1.0 - saturate(roundedBoxShadow(ib2, p - _Inset2Offset.xy, in2Sigma, in2R));
                float inset2A = _Inset2Color.a * inset2Mask * shape;

                float in3Sigma = max(_Inset3Blur * 0.5, 0.5);
                float2 ib3 = max(b - _Inset3Spread, 1.0);
                float in3R = min(r, min(ib3.x, ib3.y));
                float inset3Mask = 1.0 - saturate(roundedBoxShadow(ib3, p - _Inset3Offset.xy, in3Sigma, in3R));
                float inset3A = _Inset3Color.a * inset3Mask * shape;

                float3 shadowRGB = LinearToGammaSpace(_ShadowColor.rgb);
                float3 insetRGB  = LinearToGammaSpace(_InsetColor.rgb);
                float3 inset2RGB = LinearToGammaSpace(_Inset2Color.rgb);
                float3 inset3RGB = LinearToGammaSpace(_Inset3Color.rgb);
                float4 col = float4(shadowRGB, outerA);
                col.rgb = lerp(col.rgb, fill.rgb, fillA);
                col.a   = fillA + col.a * (1.0 - fillA);
                col.rgb = lerp(col.rgb, inset3RGB, inset3A);
                col.a   = inset3A + col.a * (1.0 - inset3A);
                col.rgb = lerp(col.rgb, inset2RGB, inset2A);
                col.a   = inset2A + col.a * (1.0 - inset2A);
                col.rgb = lerp(col.rgb, insetRGB, insetA);
                col.a   = insetA + col.a * (1.0 - insetA);

                if (_ClipRadius > 0.5)
                {
                    float2 cc = float2(0.0, b.y - _ClipRadius);
                    float  dc = length(p - cc) - _ClipRadius;
                    float  aac = fwidth(dc) + 1e-4;
                    col.a *= 1.0 - smoothstep(-aac, aac, dc);
                }

                if (_LevelOn > 0.5)
                {
                    if (_LevelAxis < 0.5)
                    {
                        float aaw = fwidth(p.y) + 1e-4;
                        col.a *= smoothstep(surface - aaw, surface + aaw, p.y);
                    }
                    else
                    {
                        float aaw = fwidth(p.x) + 1e-4;
                        col.a *= 1.0 - smoothstep(edgeX - aaw, edgeX + aaw, p.x);
                        if (_LevelMin > 1e-4)
                        {
                            float edgeMin = b.x * (2.0 * _LevelMin - 1.0);
                            col.a *= smoothstep(edgeMin - aaw, edgeMin + aaw, p.x);
                        }
                    }
                }

                if (_SweepOn > 0.5)
                {
                    float ang = atan2(p.x, -p.y);
                    if (ang < 0.0) ang += 2.0 * PI;
                    float angT = ang / (2.0 * PI);
                    float aas  = 1.0 / (2.0 * PI * max(length(p), 1.0));
                    float cov  = 1.0 - smoothstep(_Sweep - aas, _Sweep + aas, angT);
                    cov = (_Sweep >= 1.0) ? 1.0 : cov;
                    col.a *= cov * step(1e-4, _Sweep);
                }
                return float4(GammaToLinearSpace(col.rgb), col.a);
            }
            ENDCG
        }
    }
}
