using UnityEngine;

[CreateAssetMenu(fileName = "Staff", menuName = "Rebirth/Items/Staff")]
public class Staff : MeleeWeapon
{
    protected override Tags InherentTags => base.InherentTags.Plus(Tag.Blunt, Tag.Magic, Tag.TwoHanded);
    protected override string WeaponType => "Staff";
}
