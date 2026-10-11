using UnityEngine;

[CreateAssetMenu(fileName = "Wand", menuName = "Rebirth/Items/Wand")]
public class Wand : MagicWeapon
{
    protected override string WeaponType => "Wand";
}
