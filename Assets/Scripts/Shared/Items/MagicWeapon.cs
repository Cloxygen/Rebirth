public abstract class MagicWeapon : Weapon
{
    protected override Tags InherentTags => base.InherentTags.Plus(Tag.Magic);
}
