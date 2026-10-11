using UnityEngine;

[CreateAssetMenu(fileName = "Sword", menuName = "Rebirth/Items/Sword")]
public class Sword : MeleeWeapon
{
    protected override Tags InherentTags => base.InherentTags.Plus(Tag.Slashing);
    protected override string WeaponType => "Sword";
}
