using UnityEngine;

public class MeleeSwing
{
    public readonly struct Arc
    {
        public readonly Vector3 BoxSize;
        public readonly float ForwardOffset;
        public readonly float Degrees;
        public readonly float Direction;
        public readonly Color TelegraphColor;
        public readonly Color BladeColor;

        public Arc(Vector3 boxSize, float forwardOffset, float degrees, float direction,
                   Color telegraphColor, Color bladeColor)
        {
            BoxSize = boxSize;
            ForwardOffset = forwardOffset;
            Degrees = degrees;
            Direction = direction;
            TelegraphColor = telegraphColor;
            BladeColor = bladeColor;
        }
    }

    private const float TelegraphStartScale = 0.4f;

    private readonly Transform attacker;
    private readonly Hitbox hitbox;
    private readonly Arc arc;
    private readonly float telegraphDuration;
    private readonly float bladeDuration;

    private GameObject box;
    private bool boxIsBlade;
    private float elapsed;
    private float drawnElapsed;

    public bool IsFinished { get; private set; }

    public bool BladeIsOut { get; private set; }

    public float BladeProgress { get; private set; }

    public MeleeSwing(Transform attacker, Hitbox hitbox, Arc arc,
                      float telegraphDuration, float bladeDuration)
    {
        this.attacker = attacker;
        this.hitbox = hitbox;
        this.arc = arc;
        this.telegraphDuration = telegraphDuration;
        this.bladeDuration = bladeDuration;
    }

    public void Advance(float seconds)
    {
        if (IsFinished) return;

        elapsed += seconds;
        if (elapsed < telegraphDuration) return;

        BladeIsOut = true;
        BladeProgress = BladeProgressAt(elapsed);

        SweepHitboxThrough(elapsed);

        if (BladeProgress >= 1.0f) Stop();
    }

    public void Draw(float seconds)
    {
        if (IsFinished) return;

        drawnElapsed += seconds;

        if (drawnElapsed < telegraphDuration) DrawTelegraph();
        else DrawBlade();
    }

    public void Stop()
    {
        DestroyBox();
        IsFinished = true;
    }

    private void SweepHitboxThrough(float seconds)
    {
        if (hitbox == null) return;

        PoseAt(seconds, out Vector3 position, out Quaternion rotation);
        hitbox.SweepTo(position, rotation);
    }

    private void DrawTelegraph()
    {
        ShowBox("SwingTelegraph", arc.TelegraphColor, isBlade: false);

        float grown = Mathf.Lerp(TelegraphStartScale, 1.0f, Mathf.Clamp01(drawnElapsed / telegraphDuration));

        PlaceBoxAt(drawnElapsed);
        BoxMesh.SetWorldSize(box.transform, attacker, arc.BoxSize * grown);
    }

    private void DrawBlade()
    {
        ShowBox("SwingBlade", arc.BladeColor, isBlade: true);

        PlaceBoxAt(drawnElapsed);
    }

    private void ShowBox(string name, Color color, bool isBlade)
    {
        if (box != null && boxIsBlade == isBlade) return;

        DestroyBox();
        box = BoxMesh.Create(attacker, name, arc.BoxSize, color);
        boxIsBlade = isBlade;
    }

    private void PlaceBoxAt(float seconds)
    {
        PoseAt(seconds, out Vector3 position, out Quaternion rotation);
        box.transform.SetPositionAndRotation(position, rotation);
    }

    private void PoseAt(float seconds, out Vector3 position, out Quaternion rotation)
    {
        rotation = attacker.rotation * Quaternion.AngleAxis(AngleAt(seconds), Vector3.up);
        position = attacker.position + rotation * Vector3.forward * arc.ForwardOffset;
    }

    private float AngleAt(float seconds)
    {
        float halfArc = arc.Degrees * 0.5f;

        if (seconds < telegraphDuration) return -halfArc * arc.Direction;

        return Mathf.Lerp(-halfArc, halfArc, BladeProgressAt(seconds)) * arc.Direction;
    }

    private float BladeProgressAt(float seconds)
    {
        return bladeDuration > 0.0f
            ? Mathf.Clamp01((seconds - telegraphDuration) / bladeDuration)
            : 1.0f;
    }

    private void DestroyBox()
    {
        if (box == null) return;

        Object.Destroy(box);
        box = null;
    }
}
