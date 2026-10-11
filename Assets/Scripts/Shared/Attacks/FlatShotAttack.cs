using UnityEngine;

[CreateAssetMenu(menuName = "Rebirth/Attacks/Flat Shot", fileName = "FlatShot")]
public class FlatShotAttack : AbilityDefinition
{
    [Header("Timing")]
    public float attackDuration = 0.35f;

    [Header("Accuracy")]
    public float spreadDegrees = 0.0f;
    public float degreesBetweenProjectiles = 9.0f;

    [Header("Projectile")]
    public float launchSpeed = 30.0f;
    public float launchHeightAboveGround = 1.4f;
    public float launchForwardOffset = 0.6f;
    public Projectile.Settings projectile = new Projectile.Settings();

    [Header("Aim")]
    [Range(-89.0f, 89.0f)] public float launchPitchDegrees = 0.0f;

    public override Ability CreateAbility(GameObject caster) => new FlatShot(this, caster);
}
