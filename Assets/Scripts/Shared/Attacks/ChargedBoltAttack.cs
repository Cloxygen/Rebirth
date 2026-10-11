using UnityEngine;

[CreateAssetMenu(menuName = "Rebirth/Attacks/Charged Bolt", fileName = "ChargedBolt")]
public class ChargedBoltAttack : AbilityDefinition
{
    [Header("Charge")]
    public float chargeDuration = 1.1f;

    [Header("Power")]
    public float damageMultiplierAtZeroCharge = 0.4f;
    public float damageMultiplierAtFullCharge = 1.8f;
    public float minimumBlastRadius = 0.6f;
    public float maximumBlastRadius = 2.6f;

    [Header("Bolt")]
    public float boltSpeed = 22.0f;
    public float launchHeightAboveGround = 1.4f;
    public float launchForwardOffset = 0.6f;
    public float maximumCastRange = 22.0f;
    public float degreesBetweenProjectiles = 9.0f;
    public Projectile.Settings bolt = new Projectile.Settings
    {
        size = new Vector3(0.3f, 0.3f, 0.5f),
        color = new Color(0.722f, 0.306f, 0.878f, 0.9f),
        gravity = 0.0f,
        maximumLifetime = 4.0f
    };

    public override Ability CreateAbility(GameObject caster) => new ChargedBolt(this, caster);
}
