public abstract class RangedWeapon : Weapon
{
    protected override Tags InherentTags => base.InherentTags.Plus(Tag.Ranged);
}
