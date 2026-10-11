using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class MinimapView : VisualElement
{
    static readonly CustomStyleProperty<float> s_Super = new CustomStyleProperty<float>("--supersample");

    float _super = 2f;

    Camera _cam;
    RenderTexture _rt;
    Transform _target;
    VisualElement _arrow;
    IVisualElementScheduledItem _tick;
    int _searchCooldown;

    public Camera Cam
    {
        get => _cam;
        set
        {
            if (_cam == value) return;
            if (_cam != null && _cam.targetTexture == _rt) _cam.targetTexture = null;
            _cam = value;
            Bind();
        }
    }

    public bool Follow { get; set; } = true;
    public float FollowHeight { get; set; } = 120f;
    public float RefreshHz { get; set; } = 15f;

    public float ZoomStep { get; set; } = 1.12f;
    public float ZoomMin { get; set; } = 10f;
    public float ZoomMax { get; set; } = 90f;

    float _lastRender;

    public bool SkipRender { get; set; }
    public double LastRenderMs { get; private set; }
    public int RenderCount { get; private set; }
    public bool CameraSelfRenders => _cam != null && _cam.enabled;
    public void ResetRenderCount() => RenderCount = 0;
    static readonly System.Diagnostics.Stopwatch s_Watch = new System.Diagnostics.Stopwatch();

    public MinimapView()
    {
        RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        RegisterCallback<GeometryChangedEvent>(_ => Rebuild());
        RegisterCallback<WheelEvent>(OnWheel);
        RegisterCallback<AttachToPanelEvent>(_ =>
        {
            _arrow = this.Q<VisualElement>(className: "mm-arrow");
            _tick ??= schedule.Execute(Track).Every(16);
            _tick.Resume();
        });
        RegisterCallback<DetachFromPanelEvent>(_ => Cleanup());
    }

    void OnCustomStyleResolved(CustomStyleResolvedEvent e)
    {
        if (!e.customStyle.TryGetValue(s_Super, out _super)) _super = 2f;
        Rebuild();
    }

    void Rebuild()
    {
        float w = resolvedStyle.width;
        float h = resolvedStyle.height;
        if (float.IsNaN(w) || float.IsNaN(h) || w < 1f || h < 1f) return;

        float ss = Mathf.Clamp(_super, 1f, 4f);
        int iw = Mathf.Max(4, Mathf.CeilToInt(w * ss));
        int ih = Mathf.Max(4, Mathf.CeilToInt(h * ss));
        if (_rt != null && _rt.width == iw && _rt.height == ih) return;

        if (_rt != null)
        {
            if (_cam != null && _cam.targetTexture == _rt) _cam.targetTexture = null;
            _rt.Release();
        }
        _rt = new RenderTexture(iw, ih, 24, RenderTextureFormat.ARGB32) { hideFlags = HideFlags.DontSave };
        _rt.Create();
        Bind();
    }

    void Bind()
    {
        if (_rt == null) return;
        style.backgroundImage = Background.FromRenderTexture(_rt);
        if (_cam != null) _cam.targetTexture = _rt;
    }

    void OnWheel(WheelEvent e)
    {
        if (_cam == null || !_cam.orthographic) return;

        float d = Mathf.Clamp(e.delta.y, -3f, 3f);
        if (Mathf.Approximately(d, 0f)) return;

        float lo = Mathf.Max(0.01f, Mathf.Min(ZoomMin, ZoomMax));
        float hi = Mathf.Max(lo, Mathf.Max(ZoomMin, ZoomMax));
        _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize * Mathf.Pow(Mathf.Max(ZoomStep, 1.001f), d), lo, hi);

        _lastRender = 0f;
        e.StopPropagation();
    }

    void Track()
    {
        if (_cam == null || _rt == null) return;
        if (_target == null)
        {
            if (--_searchCooldown > 0) return;
            _searchCooldown = 30;
            _target = FindTarget();
            if (_target == null) return;
        }

        if (Follow)
        {
            Vector3 p = _target.position;
            _cam.transform.SetPositionAndRotation(new Vector3(p.x, p.y + FollowHeight, p.z),
                                                  Quaternion.Euler(90f, 0f, 0f));
        }

        float now = Time.realtimeSinceStartup;
        if (!_cam.enabled && !SkipRender && now - _lastRender >= 1f / Mathf.Max(RefreshHz, 0.1f))
        {
            _lastRender = now;
            s_Watch.Restart();
            _cam.Render();
            s_Watch.Stop();
            LastRenderMs = s_Watch.Elapsed.TotalMilliseconds;
            RenderCount++;
        }

        if (_arrow != null)
            _arrow.style.rotate = new Rotate(new Angle(_target.eulerAngles.y, AngleUnit.Degree));
    }

    static Transform FindTarget() => LocalPlayer.Body;

    void Cleanup()
    {
        _tick?.Pause();
        if (_cam != null && _cam.targetTexture == _rt) _cam.targetTexture = null;
        if (_rt != null) { _rt.Release(); _rt = null; }
        _cam = null;
        _target = null;
    }
}
