using UnityEngine;

public struct ArcTrace
{
    public int PointCount;
    public bool Landed;
    public bool EndsOnTarget;
    public Vector3 LandingPoint;
    public Vector3 LandingNormal;
    public float Length;

    public static ArcTrace Sweep(in BallisticCurve curve, float duration, Vector3[] points,
                                 float sweepRadius, int layersExcludingShooter, int sweepSegments)
    {
        if (points == null || points.Length < 2) return default;

        FillPath(curve, duration, points);
        SyncCollidersForTracing();

        Sweeper sweeper = new Sweeper(points, sweepRadius, layersExcludingShooter);

        sweeper.StopAtFirstImpact(sweepSegments);
        sweeper.MeasureLength();

        return sweeper.Trace;
    }

    private static void FillPath(in BallisticCurve curve, float duration, Vector3[] points)
    {
        float timeStep = duration / (points.Length - 1);
        points[0] = curve.LaunchPosition;

        for (int index = 1; index < points.Length; index++)
            points[index] = curve.PositionAfterSeconds(timeStep * index);
    }

    private static int lastSyncedFrame = -1;

    private static void SyncCollidersForTracing()
    {
        if (lastSyncedFrame == Time.frameCount) return;

        lastSyncedFrame = Time.frameCount;
        Physics.SyncTransforms();
    }

    private readonly struct Chord
    {
        public readonly Vector3 Start;
        public readonly Vector3 Direction;
        public readonly float Length;

        private readonly int from;
        private readonly int to;

        public Chord(Vector3[] points, int from, int to)
        {
            this.from = from;
            this.to = to;

            Start = points[from];

            Vector3 span = points[to] - Start;
            Length = span.magnitude;
            Direction = Length > 0.0f ? span / Length : Vector3.forward;
        }

        public bool HasNoLength => Length <= 0.0f;

        public Vector3 PointAt(float distance) => Start + Direction * distance;

        public int IndexJustPast(float distance)
        {
            float fraction = distance / Length;

            return Mathf.Min(from + Mathf.FloorToInt(fraction * (to - from)) + 1, to);
        }
    }

    private struct Sweeper
    {
        public ArcTrace Trace;

        private readonly Vector3[] points;
        private readonly float radius;
        private readonly int layersExcludingShooter;

        public Sweeper(Vector3[] points, float radius, int layersExcludingShooter)
        {
            this.points = points;
            this.radius = radius;
            this.layersExcludingShooter = layersExcludingShooter;

            Trace = default;
            Trace.PointCount = points.Length;
        }

        public void StopAtFirstImpact(int segments)
        {
            int stride = Mathf.Max(1, (points.Length - 1) / Mathf.Max(1, segments));

            for (int from = 0; from < points.Length - 1; from += stride)
                if (ChordHitsSomething(from, Mathf.Min(from + stride, points.Length - 1))) return;
        }

        public void MeasureLength()
        {
            float length = 0.0f;

            for (int index = 1; index < Trace.PointCount; index++)
                length += Vector3.Distance(points[index - 1], points[index]);

            Trace.Length = length;
        }

        private bool ChordHitsSomething(int from, int to)
        {
            Chord chord = new Chord(points, from, to);
            if (chord.HasNoLength) return false;

            if (!SphereCastAlong(chord, out RaycastHit hit)) return false;

            EndPathAt(chord, hit);
            DescribeImpact(hit);
            return true;
        }

        private bool SphereCastAlong(Chord chord, out RaycastHit hit)
        {
            return Physics.SphereCast(chord.Start, radius, chord.Direction, out hit,
                chord.Length, layersExcludingShooter, QueryTriggerInteraction.Ignore);
        }

        private void EndPathAt(Chord chord, RaycastHit hit)
        {
            int endIndex = chord.IndexJustPast(hit.distance);

            points[endIndex] = chord.PointAt(hit.distance);

            Trace.PointCount = endIndex + 1;
        }

        private void DescribeImpact(RaycastHit hit)
        {
            Trace.Landed = true;
            Trace.LandingPoint = hit.point;
            Trace.LandingNormal = hit.normal;
            Trace.EndsOnTarget = Hurtbox.FindByCollider(hit.collider) != null;
        }
    }
}
