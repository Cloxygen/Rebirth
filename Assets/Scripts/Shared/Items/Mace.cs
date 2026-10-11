using UnityEngine;

[CreateAssetMenu(fileName = "Mace", menuName = "Rebirth/Items/Mace")]
public class Mace : MeleeWeapon
{
    protected override Tags InherentTags => base.InherentTags.Plus(Tag.Blunt);
    protected override string WeaponType => "Mace";
}
