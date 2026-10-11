using UnityEngine;

[CreateAssetMenu(menuName = "Rebirth/Attacks/Arrow Shot", fileName = "ArrowShot")]
public class ArrowShotAttack : AbilityDefinition
{
    [Header("Timing")]
    public float attackDuration = 0.35f;

    [Header("Power")]
    public float damageMultiplier = 1.0f;

    [Header("Accuracy")]
    public float spreadDegrees = 0.0f;
    public float degreesBetweenProjectiles = 9.0f;

    [Header("Projectile")]
    public float launchSpeed = 30.0f;
    public float launchHeightAboveGround = 1.4f;
    public float launchForwardOffset = 0.6f;
    public Projectile.Settings projectile = new Projectile.Settings();

    [Header("Aim")]
    public float ledgeClearance = 0.5f;
    public float maximumLedgeSearchHeight = 6.0f;
    public float ledgeEdgeInset = 0.15f;
    [Range(-89.0f, 0.0f)] public float minimumLaunchAngle = -60.0f;
    [Range(0.0f, 89.0f)] public float maximumLaunchAngle = 89.0f;
    public float maximumPreviewDuration = 6.0f;

    public override Ability CreateAbility(GameObject caster) => new ArrowShot(this, caster);
}
