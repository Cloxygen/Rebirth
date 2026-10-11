using UnityEngine;

public struct AimShot
{
    public BallisticCurve Curve;
    public float SpreadDegrees;
    public float GroundRadius;
    public float SweepRadius;
    public int SweepLayers;
    public float Duration;
    public float Charge;

    public Vector3 LaunchPosition => Curve.LaunchPosition;

    public BallisticCurve Fanned(float yawDegrees)
    {
        Vector3 turned = Quaternion.AngleAxis(yawDegrees, Vector3.up) * Curve.LaunchVelocity;

        return Ballistics.CurveFiredAlong(Curve.LaunchPosition, turned, turned.magnitude, Curve.Gravity);
    }
}

public abstract class AimIndicator : MonoBehaviour
{
    public abstract void Show(in AimShot shot);

    public abstract void Hide();
}
