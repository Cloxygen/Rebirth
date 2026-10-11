using UnityEngine;

[CreateAssetMenu(fileName = "Bow", menuName = "Rebirth/Items/Bow")]
public class Bow : RangedWeapon
{
    protected override Tags InherentTags => base.InherentTags.Plus(Tag.TwoHanded);
    protected override string WeaponType => "Bow";
}
