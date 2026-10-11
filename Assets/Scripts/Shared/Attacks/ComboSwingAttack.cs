using UnityEngine;

[CreateAssetMenu(menuName = "Rebirth/Attacks/Combo Swing", fileName = "ComboSwing")]
public class ComboSwingAttack : AbilityDefinition
{
    [System.Serializable]
    public class Swing
    {
        public float damageMultiplier = 1.0f;
        public float swingArcDegrees = 100.0f;
        public float swingDirection = 1.0f;
        public float windupDuration = 0.08f;
        public float swingDuration = 0.15f;
        public float lungeDistance = 1.0f;
        public float recoveryDuration = 0.2f;
    }

    [Header("Hitbox")]
    public Vector3 hitboxSize = new Vector3(1.2f, 1.5f, 1.0f);
    public float hitboxForwardOffset = 1.0f;
    public Color windupColor = new Color(1.0f, 0.85f, 0.4f, 0.16f);
    public Color hitboxColor = new Color(1.0f, 0.0f, 0.0f, 0.35f);

    [Header("Swings")]
    public Swing[] swings = new Swing[]
    {
        new Swing { damageMultiplier = 0.75f, swingArcDegrees = 90.0f,  swingDirection =  1.0f, windupDuration = 0.08f, swingDuration = 0.15f, lungeDistance = 0.5f,  recoveryDuration = 0.15f },
        new Swing { damageMultiplier = 0.9f,  swingArcDegrees = 100.0f, swingDirection = -1.0f, windupDuration = 0.08f, swingDuration = 0.15f, lungeDistance = 0.75f, recoveryDuration = 0.18f },
        new Swing { damageMultiplier = 1.5f,  swingArcDegrees = 0.0f,   swingDirection =  1.0f, windupDuration = 0.2f,  swingDuration = 0.22f, lungeDistance = 1.75f, recoveryDuration = 0.35f },
    };

    public float comboWindow = 0.6f;

    public override Ability CreateAbility(GameObject caster) => new ComboSwing(this, caster);
}
