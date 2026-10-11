using UnityEngine;

public abstract class AbilityDefinition : ScriptableObject
{
    [Header("Timing")]
    public float cooldown = 0.5f;
    public bool repeatsWhileHeld;
    [Range(0.0f, 1.0f)]
    public float movementSpeedMultiplier;

    public bool aimsAtCursor;
    public GameObject indicatorPrefab;
    public Sprite icon;
    public float turnDuration = 0.12f;

    public LayerMask hittableLayers = ~0;

    public float knockbackDistance;

    [Header("Cost")]
    public float manaCost;

    [Header("Damage")]
    [SerializeField] private Damage damage;

    [Header("Tags")]
    [SerializeField] private Tags extraTags;
    [SerializeField] private Tags requiredWeaponTags;
    [SerializeField] private Tags forbiddenWeaponTags;

    protected virtual Tags InherentTags => default;

    public Tags AllTags => InherentTags.Plus(extraTags);

    public Damage AuthoredDamage => damage;

    public bool CanBeUsedWith(Tags heldTags) =>
        heldTags.HasAll(requiredWeaponTags) && heldTags.HasNone(forbiddenWeaponTags);

    public abstract Ability CreateAbility(GameObject caster);
}
