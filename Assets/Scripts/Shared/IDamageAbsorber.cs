using UnityEngine;

public interface IDamageAbsorber
{
    Damage Absorb(Damage damage, Vector3 hitPoint);
}
