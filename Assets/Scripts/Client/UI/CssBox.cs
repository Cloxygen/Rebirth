using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class CssBox : VisualElement
{
    static readonly CustomStyleProperty<float> s_Radius      = new CustomStyleProperty<float>("--radius");
    static readonly CustomStyleProperty<float> s_ClipRadius  = new CustomStyleProperty<float>("--clip-radius");
    static readonly CustomStyleProperty<int>   s_Shape       = new CustomStyleProperty<int>("--shape");
    static readonly CustomStyleProperty<float> s_Hole        = new CustomStyleProperty<float>("--hole");
    static readonly CustomStyleProperty<float> s_Level       = new CustomStyleProperty<float>("--level");
    static readonly CustomStyleProperty<int>   s_LevelAxis   = new CustomStyleProperty<int>("--level-axis");
    static readonly CustomStyleProperty<float> s_Sweep       = new CustomStyleProperty<float>("--sweep");
    static readonly CustomStyleProperty<int>   s_FillMode    = new CustomStyleProperty<int>("--fill-mode");
    static readonly CustomStyleProperty<Color> s_FillA       = new CustomStyleProperty<Color>("--fill-a");
    static readonly CustomStyleProperty<Color> s_FillB       = new CustomStyleProperty<Color>("--fill-b");
    static readonly CustomStyleProperty<float> s_GradAngle   = new CustomStyleProperty<float>("--gradient-angle");
    static readonly CustomStyleProperty<float> s_GradCenterX = new CustomStyleProperty<float>("--gradient-center-x");
    static readonly CustomStyleProperty<float> s_GradCenterY = new CustomStyleProperty<float>("--gradient-center-y");
    static readonly CustomStyleProperty<float> s_GradRadiusX = new CustomStyleProperty<float>("--gradient-radius-x");
    static readonly CustomStyleProperty<float> s_GradRadiusY = new CustomStyleProperty<float>("--gradient-radius-y");
    static readonly CustomStyleProperty<float> s_GradStart   = new CustomStyleProperty<float>("--gradient-start");
    static readonly CustomStyleProperty<float> s_GradEnd     = new CustomStyleProperty<float>("--gradient-end");
    static readonly CustomStyleProperty<float> s_Pad         = new CustomStyleProperty<float>("--pad");
    static readonly CustomStyleProperty<float> s_ShadowY     = new CustomStyleProperty<float>("--shadow-y");
    static readonly CustomStyleProperty<float> s_ShadowBlur  = new CustomStyleProperty<float>("--shadow-blur");
    static readonly CustomStyleProperty<Color> s_ShadowCol   = new CustomStyleProperty<Color>("--shadow-color");
    static readonly CustomStyleProperty<float> s_InsetY      = new CustomStyleProperty<float>("--inset-y");
    static readonly CustomStyleProperty<float> s_InsetBlur   = new CustomStyleProperty<float>("--inset-blur");
    static readonly CustomStyleProperty<float> s_InsetSpread = new CustomStyleProperty<float>("--inset-spread");
    static readonly CustomStyleProperty<Color> s_InsetCol    = new CustomStyleProperty<Color>("--inset-color");
    static readonly CustomStyleProperty<float> s_Inset2Y     = new CustomStyleProperty<float>("--inset2-y");
    static readonly CustomStyleProperty<float> s_Inset2Blur  = new CustomStyleProperty<float>("--inset2-blur");
    static readonly CustomStyleProperty<float> s_Inset2Spread= new CustomStyleProperty<float>("--inset2-spread");
    static readonly CustomStyleProperty<Color> s_Inset2Col   = new CustomStyleProperty<Color>("--inset2-color");
    static readonly CustomStyleProperty<float> s_Inset3Y     = new CustomStyleProperty<float>("--inset3-y");
    static readonly CustomStyleProperty<float> s_Inset3Blur  = new CustomStyleProperty<float>("--inset3-blur");
    static readonly CustomStyleProperty<float> s_Inset3Spread= new CustomStyleProperty<float>("--inset3-spread");
    static readonly CustomStyleProperty<Color> s_Inset3Col   = new CustomStyleProperty<Color>("--inset3-color");
    static readonly CustomStyleProperty<int>   s_Ov1Mode     = new CustomStyleProperty<int>("--ov1-mode");
    static readonly CustomStyleProperty<Color> s_Ov1A        = new CustomStyleProperty<Color>("--ov1-a");
    static readonly CustomStyleProperty<Color> s_Ov1B        = new CustomStyleProperty<Color>("--ov1-b");
    static readonly CustomStyleProperty<float> s_Ov1Angle    = new CustomStyleProperty<float>("--ov1-angle");
    static readonly CustomStyleProperty<float> s_Ov1CenterX  = new CustomStyleProperty<float>("--ov1-center-x");
    static readonly CustomStyleProperty<float> s_Ov1CenterY  = new CustomStyleProperty<float>("--ov1-center-y");
    static readonly CustomStyleProperty<float> s_Ov1RadiusX  = new CustomStyleProperty<float>("--ov1-radius-x");
    static readonly CustomStyleProperty<float> s_Ov1RadiusY  = new CustomStyleProperty<float>("--ov1-radius-y");
    static readonly CustomStyleProperty<float> s_Ov1Start    = new CustomStyleProperty<float>("--ov1-start");
    static readonly CustomStyleProperty<float> s_Ov1End      = new CustomStyleProperty<float>("--ov1-end");
    static readonly CustomStyleProperty<int>   s_Ov2Mode     = new CustomStyleProperty<int>("--ov2-mode");
    static readonly CustomStyleProperty<Color> s_Ov2A        = new CustomStyleProperty<Color>("--ov2-a");
    static readonly CustomStyleProperty<Color> s_Ov2B        = new CustomStyleProperty<Color>("--ov2-b");
    static readonly CustomStyleProperty<float> s_Ov2Angle    = new CustomStyleProperty<float>("--ov2-angle");
    static readonly CustomStyleProperty<float> s_Ov2CenterX  = new CustomStyleProperty<float>("--ov2-center-x");
    static readonly CustomStyleProperty<float> s_Ov2CenterY  = new CustomStyleProperty<float>("--ov2-center-y");
    static readonly CustomStyleProperty<float> s_Ov2RadiusX  = new CustomStyleProperty<float>("--ov2-radius-x");
    static readonly CustomStyleProperty<float> s_Ov2RadiusY  = new CustomStyleProperty<float>("--ov2-radius-y");
    static readonly CustomStyleProperty<float> s_Ov2Start    = new CustomStyleProperty<float>("--ov2-start");
    static readonly CustomStyleProperty<float> s_Ov2End      = new CustomStyleProperty<float>("--ov2-end");
    static readonly CustomStyleProperty<int>   s_SwirlOn       = new CustomStyleProperty<int>("--swirl-on");
    static readonly CustomStyleProperty<float> s_SwirlStrength = new CustomStyleProperty<float>("--swirl-strength");
    static readonly CustomStyleProperty<float> s_SwirlScale    = new CustomStyleProperty<float>("--swirl-scale");
    static readonly CustomStyleProperty<float> s_SwirlSpeed    = new CustomStyleProperty<float>("--swirl-speed");
    static readonly CustomStyleProperty<float> s_SwirlParallax = new CustomStyleProperty<float>("--swirl-parallax");
    static readonly CustomStyleProperty<float> s_SwirlSpherical = new CustomStyleProperty<float>("--swirl-spherical");
    static readonly CustomStyleProperty<float> s_SwirlLatitude  = new CustomStyleProperty<float>("--swirl-latitude");
    static readonly CustomStyleProperty<float> s_SwirlWarp      = new CustomStyleProperty<float>("--swirl-warp");
    static readonly CustomStyleProperty<float> s_RimStrength  = new CustomStyleProperty<float>("--rim-strength");
    static readonly CustomStyleProperty<float> s_RimWidth     = new CustomStyleProperty<float>("--rim-width");
    static readonly CustomStyleProperty<float> s_SurfStrength = new CustomStyleProperty<float>("--surface-strength");
    static readonly CustomStyleProperty<float> s_SurfWidth    = new CustomStyleProperty<float>("--surface-width");

    float _radius = -1f;
    float _clipRadius;
    int   _shape;
    float _hole;
    bool  _holeOverride;
    float _level = 1f;
    float _levelMin;
    int   _levelAxis;
    bool  _levelOn;
    bool  _levelOverride;
    float _sweep;
    bool  _sweepOn;
    bool  _sweepOverride;
    int   _fillMode;
    Color _fillA = Color.clear, _fillB = Color.clear;
    bool  _fillOverride;
    bool  _ov1Override;
    bool  _ov2Override;
    bool  _shapeOverride;
    float _gradAngle = 90f, _pad;
    float _gradCX = 0.5f, _gradCY = 0.5f, _gradRX = 0.5f, _gradRY = 0.5f, _gradStart = 0f, _gradEnd = 1f;
    float _shadowY, _shadowBlur; Color _shadowCol = Color.clear;
    bool  _shadowColOverride;
    float _insetY, _insetBlur, _insetSpread; Color _insetCol = Color.clear;
    float _inset2Y, _inset2Blur, _inset2Spread; Color _inset2Col = Color.clear;
    float _inset3Y, _inset3Blur, _inset3Spread; Color _inset3Col = Color.clear;
    int   _ov1Mode;
    Color _ov1A = Color.clear, _ov1B = Color.clear;
    float _ov1Angle = 90f, _ov1CX = 0.5f, _ov1CY = 0.5f, _ov1RX = 0.5f, _ov1RY = 0.5f, _ov1Start = 0f, _ov1End = 1f;
    int   _ov2Mode;
    Color _ov2A = Color.clear, _ov2B = Color.clear;
    float _ov2Angle = 90f, _ov2CX = 0.5f, _ov2CY = 0.5f, _ov2RX = 0.5f, _ov2RY = 0.5f, _ov2Start = 0f, _ov2End = 1f;
    int   _swirlOn;
    float _swirlStrength = 0.6f, _swirlScale = 3f, _swirlSpeed = 3.5f, _swirlParallax = 0.8f, _swirlSpherical = 1f, _swirlLatitude = 1f, _swirlTime;
    float _swirlWarp = 0.45f;
    float _swirlStrengthOverride = -1f;
    float _swirlWarpOverride = -1f;
    int   _swirlOnOverride = -1;
    float _rimStrength, _rimWidth = 16f;
    float _surfStrength, _surfWidth = 3f;
    float _surfStrengthOverride = -1f;
    float _surfWidthOverride = -1f;

    Material _mat;
    RenderTexture _rt;

    public CssBox()
    {
        RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        RegisterCallback<GeometryChangedEvent>(_ => Rebuild());
        RegisterCallback<DetachFromPanelEvent>(_ => Cleanup());
    }

    void OnCustomStyleResolved(CustomStyleResolvedEvent e)
    {
        ICustomStyle cs = e.customStyle;
        if (_shapeOverride) {  }
        else if (!cs.TryGetValue(s_Radius, out _radius)) _radius = -1f;
        cs.TryGetValue(s_ClipRadius, out _clipRadius);
        cs.TryGetValue(s_Shape, out _shape);
        if (!_holeOverride) cs.TryGetValue(s_Hole, out _hole);
        if (!_levelOverride && cs.TryGetValue(s_Level, out float lv)) { _level = lv; _levelOn = true; }
        cs.TryGetValue(s_LevelAxis, out _levelAxis);
        if (!_sweepOverride && cs.TryGetValue(s_Sweep, out float sw)) { _sweep = sw; _sweepOn = true; }
        cs.TryGetValue(s_FillMode, out _fillMode);
        if (!_fillOverride)
        {
            cs.TryGetValue(s_FillA, out _fillA);
            cs.TryGetValue(s_FillB, out _fillB);
        }
        if (!cs.TryGetValue(s_GradAngle,   out _gradAngle)) _gradAngle = 90f;
        if (!cs.TryGetValue(s_GradCenterX, out _gradCX))    _gradCX    = 0.5f;
        if (!cs.TryGetValue(s_GradCenterY, out _gradCY))    _gradCY    = 0.5f;
        if (!cs.TryGetValue(s_GradRadiusX, out _gradRX))    _gradRX    = 0.5f;
        if (!cs.TryGetValue(s_GradRadiusY, out _gradRY))    _gradRY    = 0.5f;
        if (!cs.TryGetValue(s_GradStart,   out _gradStart)) _gradStart = 0f;
        if (!cs.TryGetValue(s_GradEnd,     out _gradEnd))   _gradEnd   = 1f;
        if (!_shapeOverride) cs.TryGetValue(s_Pad, out _pad);
        cs.TryGetValue(s_ShadowY, out _shadowY);
        cs.TryGetValue(s_ShadowBlur, out _shadowBlur);
        if (!_shadowColOverride) cs.TryGetValue(s_ShadowCol, out _shadowCol);
        cs.TryGetValue(s_InsetY, out _insetY);
        cs.TryGetValue(s_InsetBlur, out _insetBlur);
        cs.TryGetValue(s_InsetSpread, out _insetSpread);
        cs.TryGetValue(s_InsetCol, out _insetCol);
        cs.TryGetValue(s_Inset2Y, out _inset2Y);
        cs.TryGetValue(s_Inset2Blur, out _inset2Blur);
        cs.TryGetValue(s_Inset2Spread, out _inset2Spread);
        cs.TryGetValue(s_Inset2Col, out _inset2Col);
        cs.TryGetValue(s_Inset3Y, out _inset3Y);
        cs.TryGetValue(s_Inset3Blur, out _inset3Blur);
        cs.TryGetValue(s_Inset3Spread, out _inset3Spread);
        cs.TryGetValue(s_Inset3Col, out _inset3Col);
        cs.TryGetValue(s_Ov1Mode, out _ov1Mode);
        if (!_ov1Override)
        {
            cs.TryGetValue(s_Ov1A, out _ov1A);
            cs.TryGetValue(s_Ov1B, out _ov1B);
        }
        if (!cs.TryGetValue(s_Ov1Angle,   out _ov1Angle)) _ov1Angle = 90f;
        if (!cs.TryGetValue(s_Ov1CenterX, out _ov1CX))    _ov1CX    = 0.5f;
        if (!cs.TryGetValue(s_Ov1CenterY, out _ov1CY))    _ov1CY    = 0.5f;
        if (!cs.TryGetValue(s_Ov1RadiusX, out _ov1RX))    _ov1RX    = 0.5f;
        if (!cs.TryGetValue(s_Ov1RadiusY, out _ov1RY))    _ov1RY    = 0.5f;
        if (!cs.TryGetValue(s_Ov1Start,   out _ov1Start)) _ov1Start = 0f;
        if (!cs.TryGetValue(s_Ov1End,     out _ov1End))   _ov1End   = 1f;
        cs.TryGetValue(s_Ov2Mode, out _ov2Mode);
        if (!_ov2Override)
        {
            cs.TryGetValue(s_Ov2A, out _ov2A);
            cs.TryGetValue(s_Ov2B, out _ov2B);
        }
        if (!cs.TryGetValue(s_Ov2Angle,   out _ov2Angle)) _ov2Angle = 90f;
        if (!cs.TryGetValue(s_Ov2CenterX, out _ov2CX))    _ov2CX    = 0.5f;
        if (!cs.TryGetValue(s_Ov2CenterY, out _ov2CY))    _ov2CY    = 0.5f;
        if (!cs.TryGetValue(s_Ov2RadiusX, out _ov2RX))    _ov2RX    = 0.5f;
        if (!cs.TryGetValue(s_Ov2RadiusY, out _ov2RY))    _ov2RY    = 0.5f;
        if (!cs.TryGetValue(s_Ov2Start,   out _ov2Start)) _ov2Start = 0f;
        if (!cs.TryGetValue(s_Ov2End,     out _ov2End))   _ov2End   = 1f;
        if (_swirlOnOverride < 0) cs.TryGetValue(s_SwirlOn, out _swirlOn);
        else                      _swirlOn = _swirlOnOverride;
        if (_swirlStrengthOverride < 0f && !cs.TryGetValue(s_SwirlStrength, out _swirlStrength)) _swirlStrength = 0.6f;
        if (!cs.TryGetValue(s_SwirlScale, out _swirlScale)) _swirlScale = 3f;
        if (!cs.TryGetValue(s_SwirlSpeed, out _swirlSpeed)) _swirlSpeed = 3.5f;
        if (!cs.TryGetValue(s_SwirlParallax, out _swirlParallax)) _swirlParallax = 0.8f;
        if (!cs.TryGetValue(s_SwirlSpherical, out _swirlSpherical)) _swirlSpherical = 1f;
        if (!cs.TryGetValue(s_SwirlLatitude, out _swirlLatitude)) _swirlLatitude = 1f;
        if (_swirlWarpOverride < 0f && !cs.TryGetValue(s_SwirlWarp, out _swirlWarp)) _swirlWarp = 0.45f;
        cs.TryGetValue(s_RimStrength, out _rimStrength);
        if (!cs.TryGetValue(s_RimWidth, out _rimWidth)) _rimWidth = 16f;
        if (_surfStrengthOverride < 0f) cs.TryGetValue(s_SurfStrength, out _surfStrength);
        if (_surfWidthOverride < 0f && !cs.TryGetValue(s_SurfWidth, out _surfWidth)) _surfWidth = 3f;
        Rebuild();
    }

    void Rebuild()
    {
        float w = resolvedStyle.width;
        float h = resolvedStyle.height;
        if (float.IsNaN(w) || float.IsNaN(h) || w < 1f || h < 1f) return;

        if (_mat == null)
        {
            var sh = Shader.Find("RebirthUI/CssBox");
            if (sh == null) return;
            _mat = new Material(sh) { hideFlags = HideFlags.DontSave };
        }

        int iw = Mathf.Max(4, Mathf.CeilToInt(w));
        int ih = Mathf.Max(4, Mathf.CeilToInt(h));
        if (_rt == null || _rt.width != iw || _rt.height != ih)
        {
            if (_rt != null) _rt.Release();
            _rt = new RenderTexture(iw, ih, 0, RenderTextureFormat.ARGB32) { hideFlags = HideFlags.DontSave };
            _rt.Create();
        }

        float radius = _radius >= 0f ? _radius : resolvedStyle.borderTopLeftRadius;
        Vector2 widget = new Vector2(Mathf.Max(1f, w - 2f * _pad), Mathf.Max(1f, h - 2f * _pad));

        _mat.SetVector("_WidgetSize", widget);
        _mat.SetFloat("_Pad", _pad);
        _mat.SetFloat("_Radius", radius);
        _mat.SetFloat("_ClipRadius", _clipRadius);
        _mat.SetFloat("_Shape", _shape);
        _mat.SetFloat("_Hole", _hole);
        _mat.SetFloat("_LevelOn", _levelOn ? 1f : 0f);
        _mat.SetFloat("_Level", _level);
        _mat.SetFloat("_LevelAxis", _levelAxis);
        _mat.SetFloat("_LevelMin", _levelMin);
        _mat.SetFloat("_SweepOn", _sweepOn ? 1f : 0f);
        _mat.SetFloat("_Sweep", _sweep);
        _mat.SetFloat("_FillMode", _fillMode);
        _mat.SetColor("_FillColor", _fillA);
        _mat.SetColor("_FillColor2", _fillB);
        _mat.SetFloat("_GradAngle", _gradAngle);
        _mat.SetVector("_GradCenter", new Vector4(_gradCX, _gradCY, 0f, 0f));
        _mat.SetVector("_GradRadius", new Vector4(_gradRX, _gradRY, 0f, 0f));
        _mat.SetFloat("_GradStart", _gradStart);
        _mat.SetFloat("_GradEnd", _gradEnd);
        _mat.SetVector("_Offset", new Vector2(0f, _shadowY));
        _mat.SetFloat("_Blur", _shadowBlur);
        _mat.SetColor("_ShadowColor", _shadowCol);
        _mat.SetVector("_InsetOffset", new Vector2(0f, _insetY));
        _mat.SetFloat("_InsetBlur", _insetBlur);
        _mat.SetFloat("_InsetSpread", _insetSpread);
        _mat.SetColor("_InsetColor", _insetCol);

        _mat.SetVector("_Inset2Offset", new Vector2(0f, _inset2Y));
        _mat.SetFloat("_Inset2Blur", _inset2Blur);
        _mat.SetFloat("_Inset2Spread", _inset2Spread);
        _mat.SetColor("_Inset2Color", _inset2Col);

        _mat.SetVector("_Inset3Offset", new Vector2(0f, _inset3Y));
        _mat.SetFloat("_Inset3Blur", _inset3Blur);
        _mat.SetFloat("_Inset3Spread", _inset3Spread);
        _mat.SetColor("_Inset3Color", _inset3Col);

        _mat.SetFloat("_Ov1On", _ov1Mode > 0 ? 1f : 0f);
        _mat.SetFloat("_Ov1Mode", _ov1Mode);
        _mat.SetColor("_Ov1ColA", _ov1A);
        _mat.SetColor("_Ov1ColB", _ov1B);
        _mat.SetVector("_Ov1Geo", _ov1Mode >= 2 ? new Vector4(_ov1CX, _ov1CY, _ov1RX, _ov1RY)
                                                : new Vector4(_ov1Angle, 0f, 0f, 0f));
        _mat.SetFloat("_Ov1Start", _ov1Start);
        _mat.SetFloat("_Ov1End", _ov1End);

        _mat.SetFloat("_Ov2On", _ov2Mode > 0 ? 1f : 0f);
        _mat.SetFloat("_Ov2Mode", _ov2Mode);
        _mat.SetColor("_Ov2ColA", _ov2A);
        _mat.SetColor("_Ov2ColB", _ov2B);
        _mat.SetVector("_Ov2Geo", _ov2Mode >= 2 ? new Vector4(_ov2CX, _ov2CY, _ov2RX, _ov2RY)
                                                : new Vector4(_ov2Angle, 0f, 0f, 0f));
        _mat.SetFloat("_Ov2Start", _ov2Start);
        _mat.SetFloat("_Ov2End", _ov2End);

        _mat.SetFloat("_SwirlOn", _swirlOn);
        _mat.SetFloat("_SwirlStrength", _swirlStrength);
        _mat.SetFloat("_SwirlScale", _swirlScale);
        _mat.SetFloat("_SwirlSpeed", _swirlSpeed);
        _mat.SetFloat("_SwirlParallax", _swirlParallax);
        _mat.SetFloat("_SwirlSpherical", _swirlSpherical);
        _mat.SetFloat("_SwirlLatitude", _swirlLatitude);
        _mat.SetFloat("_SwirlTime", _swirlTime);
        _mat.SetFloat("_SwirlWarp", _swirlWarp);
        _mat.SetFloat("_RimStrength", _rimStrength);
        _mat.SetFloat("_RimWidth", _rimWidth);
        _mat.SetFloat("_SurfaceStrength", _surfStrength);
        _mat.SetFloat("_SurfaceWidth", _surfWidth);

        RenderTexture previous = RenderTexture.active;
        Graphics.Blit(Texture2D.whiteTexture, _rt, _mat);
        RenderTexture.active = previous;

        style.backgroundImage = Background.FromRenderTexture(_rt);
    }

    public void SetBaseGradient(Color a, Color b)
    {
        _fillOverride = true;
        _fillA = a;
        _fillB = b;
        Rebuild();
    }

    public void SetOverlay1Colors(Color a, Color b)
    {
        _ov1Override = true;
        _ov1A = a;
        _ov1B = b;
        Rebuild();
    }

    public void SetOverlay2Colors(Color a, Color b)
    {
        _ov2Override = true;
        _ov2A = a;
        _ov2B = b;
        Rebuild();
    }

    public void SetShadowShape(float radius, float pad)
    {
        _shapeOverride = true;
        _radius = radius;
        _pad = pad;
        Rebuild();
    }

    public void SetShadowColor(Color c)
    {
        _shadowColOverride = true;
        _shadowCol = c;
        Rebuild();
    }

    public void SetLevel(float fill01)
    {
        _levelOverride = true;
        float v = Mathf.Clamp01(fill01);
        if (_levelOn && Mathf.Approximately(v, _level)) return;
        _levelOn = true;
        _level = v;
        Rebuild();
    }

    public void SetLevelRange(float from, float to)
    {
        float lo = Mathf.Clamp01(Mathf.Min(from, to));
        float hi = Mathf.Clamp01(Mathf.Max(from, to));
        if (_levelOn && Mathf.Approximately(lo, _levelMin) && Mathf.Approximately(hi, _level)) return;
        _levelOverride = true;
        _levelOn = true;
        _levelMin = lo;
        _level = hi;
        Rebuild();
    }

    public void SetSweep(float turns01)
    {
        _sweepOverride = true;
        float v = Mathf.Clamp01(turns01);
        if (_sweepOn && Mathf.Approximately(v, _sweep)) return;
        _sweepOn = true;
        _sweep = v;
        Rebuild();
    }

    public void SetHole(float innerRadiusPx)
    {
        float v = Mathf.Max(0f, innerRadiusPx);
        _holeOverride = true;
        if (Mathf.Approximately(v, _hole)) return;
        _hole = v;
        Rebuild();
    }

    public void SetSwirlTime(float seconds)
    {
        if (_swirlOn == 0) return;
        _swirlTime = seconds;
        Rebuild();
    }

    public void SetSwirlStrength(float strength01)
    {
        float v = Mathf.Max(0f, strength01);
        if (_swirlStrengthOverride >= 0f && Mathf.Approximately(v, _swirlStrength)) return;
        _swirlStrengthOverride = v;
        _swirlStrength = v;
        Rebuild();
    }

    public void SetSwirlOn(bool on)
    {
        int v = on ? 1 : 0;
        if (_swirlOnOverride == v) return;
        _swirlOnOverride = v;
        _swirlOn = v;
        Rebuild();
    }

    public void SetSwirlWarp(float warp)
    {
        float v = Mathf.Max(0f, warp);
        if (_swirlWarpOverride >= 0f && Mathf.Approximately(v, _swirlWarp)) return;
        _swirlWarpOverride = v;
        _swirlWarp = v;
        Rebuild();
    }

    public void SetSurface(float strength, float widthPx)
    {
        float s = Mathf.Max(0f, strength);
        float w = Mathf.Max(0.01f, widthPx);
        if (_surfStrengthOverride >= 0f && Mathf.Approximately(s, _surfStrength)
            && Mathf.Approximately(w, _surfWidth)) return;
        _surfStrengthOverride = s;
        _surfWidthOverride = w;
        _surfStrength = s;
        _surfWidth = w;
        Rebuild();
    }

    void Cleanup()
    {
        if (_rt != null)
        {
            if (RenderTexture.active == _rt) RenderTexture.active = null;
            _rt.Release();
            _rt = null;
        }
        if (_mat != null)
        {
            if (Application.isPlaying) Object.Destroy(_mat); else Object.DestroyImmediate(_mat);
            _mat = null;
        }
    }
}
