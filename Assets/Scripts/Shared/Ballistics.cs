using UnityEngine;

public readonly struct BallisticCurve
{
    public readonly Vector3 LaunchPosition;
    public readonly Vector3 LaunchVelocity;
    public readonly float Gravity;

    internal BallisticCurve(Vector3 launchPosition, Vector3 launchVelocity, float gravity)
    {
        LaunchPosition = launchPosition;
        LaunchVelocity = launchVelocity;
        Gravity = gravity;
    }

    public Vector3 PositionAfterSeconds(float seconds)
    {
        float fallDistance = 0.5f * Gravity * seconds * seconds;

        return LaunchPosition + LaunchVelocity * seconds + Vector3.down * fallDistance;
    }

    public Vector3 VelocityAfterSeconds(float seconds)
    {
        return LaunchVelocity + Vector3.down * (Gravity * seconds);
    }

    public BallisticCurve WithLaunchAngleBetween(float minimumDegrees, float maximumDegrees)
    {
        float speed = LaunchVelocity.magnitude;
        if (speed <= 0.0f) return this;

        float climb = Mathf.Asin(Mathf.Clamp(LaunchVelocity.y / speed, -1.0f, 1.0f)) * Mathf.Rad2Deg;
        float held = Mathf.Clamp(climb, minimumDegrees, maximumDegrees);
        if (held == climb) return this;

        Vector3 flat = new Vector3(LaunchVelocity.x, 0.0f, LaunchVelocity.z);
        Vector3 heading = flat.sqrMagnitude > 0.0f ? flat.normalized : Vector3.forward;
        float radians = held * Mathf.Deg2Rad;

        return new BallisticCurve(LaunchPosition,
            (heading * Mathf.Cos(radians) + Vector3.up * Mathf.Sin(radians)) * speed, Gravity);
    }
}

public static class Ballistics
{
    private const float NegligibleGravity = 0.0001f;
    private const float SlowestSpeed = 0.0001f;
    private const float StraightUpOrDownDistance = 0.001f;
    private const float ShortestUsableDistance = 0.0001f;

    public static BallisticCurve CurveThatLandsOn(Vector3 from, Vector3 target, float speed, float gravity)
    {
        speed = UsableSpeed(speed);

        Arc arc = new Arc(from, target, speed, gravity);
        Vector3 heading = HeadingOf(arc, gravity, speed);

        return new BallisticCurve(from, heading * speed, gravity);
    }

    public static BallisticCurve CurveFiredAlong(Vector3 from, Vector3 direction, float speed, float gravity)
    {
        Vector3 heading = direction.sqrMagnitude > 0.0f ? direction.normalized : Vector3.forward;

        return new BallisticCurve(from, heading * UsableSpeed(speed), gravity);
    }

    private static bool HasGravity(float gravity) => gravity > NegligibleGravity;

    private static float UsableSpeed(float speed) => Mathf.Max(SlowestSpeed, speed);

    private static Vector3 HeadingOf(Arc arc, float gravity, float speed)
    {
        if (arc.TargetIsDirectlyAboveOrBelow) return arc.StraightUpOrDown;

        return arc.Rising(ClimbDegrees(arc, gravity, speed));
    }

    private static float ClimbDegrees(Arc arc, float gravity, float speed)
    {
        if (!HasGravity(gravity)) return arc.ClimbStraightAtTarget;
        if (arc.TargetIsReachable) return arc.FlatterArcClimb;

        return SteepestUsefulClimb(arc.HeightDifference, speed, gravity);
    }

    private static float SteepestUsefulClimb(float heightDifference, float speed, float gravity)
    {
        float inverseSinSquared = 2.0f - 2.0f * gravity * heightDifference / (speed * speed);
        if (inverseSinSquared <= 0.0f) return 89.0f;

        return Mathf.Asin(Mathf.Clamp01(1.0f / Mathf.Sqrt(inverseSinSquared))) * Mathf.Rad2Deg;
    }

    private readonly struct Arc
    {
        public readonly float HeightDifference;

        private readonly Vector3 flatToTarget;
        private readonly float horizontalDistance;
        private readonly float fallOverRange;
        private readonly float discriminant;

        public Arc(Vector3 from, Vector3 target, float speed, float gravity)
        {
            Vector3 towardTarget = target - from;
            Vector3 flat = new Vector3(towardTarget.x, 0.0f, towardTarget.z);

            horizontalDistance = flat.magnitude;
            HeightDifference = towardTarget.y;
            flatToTarget = horizontalDistance > 0.0f ? flat / horizontalDistance : Vector3.forward;

            fallOverRange = gravity * horizontalDistance * horizontalDistance / (2.0f * speed * speed);
            discriminant = horizontalDistance * horizontalDistance
                - 4.0f * fallOverRange * (fallOverRange + HeightDifference);
        }

        public bool TargetIsDirectlyAboveOrBelow => horizontalDistance < StraightUpOrDownDistance;

        public bool TargetIsReachable => discriminant >= 0.0f;

        public Vector3 StraightUpOrDown => HeightDifference >= 0.0f ? Vector3.up : Vector3.down;

        public Vector3 Rising(float climbDegrees)
        {
            float radians = climbDegrees * Mathf.Deg2Rad;

            return (flatToTarget * Mathf.Cos(radians) + Vector3.up * Mathf.Sin(radians)).normalized;
        }

        public float ClimbStraightAtTarget =>
            Mathf.Atan2(HeightDifference, Mathf.Max(ShortestUsableDistance, horizontalDistance)) * Mathf.Rad2Deg;

        public float FlatterArcClimb
        {
            get
            {
                float root = Mathf.Sqrt(discriminant);

                return Mathf.Atan((horizontalDistance - root) / (2.0f * fallOverRange)) * Mathf.Rad2Deg;
            }
        }
    }
}
