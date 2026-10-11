using UnityEngine;

[CreateAssetMenu(fileName = "Weapon", menuName = "Rebirth/Items/Weapon")]
public class Weapon : ItemDefinition
{
    [Header("Damage")]
    public Damage damage;

    protected override Tags InherentTags => new Tags(Tag.Weapon);

    protected override Damage DamageForTags => damage.Plus(base.DamageForTags);

    public override string TypeLabel => "Weapon";
    public override string SubtypeLabel => AllTags.Has(Tag.TwoHanded) ? "Two-Handed " + WeaponType : WeaponType;
    protected virtual string WeaponType => "Weapon";
}
