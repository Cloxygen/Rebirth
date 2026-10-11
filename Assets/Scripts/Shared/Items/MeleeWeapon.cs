public abstract class MeleeWeapon : Weapon
{
    protected override Tags InherentTags => base.InherentTags.Plus(Tag.Melee);
}
