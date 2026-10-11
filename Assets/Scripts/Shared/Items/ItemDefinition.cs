using System;
using UnityEngine;
using UnityEngine.Serialization;

public enum ItemRarity
{
    Common,
    Magic,
    Rare,
    Epic,
    Legendary,
}

public static class ItemRarityColors
{
    public static Color ToColor(this ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Magic: return new Color(0.38f, 0.58f, 0.98f, 1.0f);
            case ItemRarity.Rare: return new Color(0.96f, 0.82f, 0.30f, 1.0f);
            case ItemRarity.Epic: return new Color(0.68f, 0.38f, 0.96f, 1.0f);
            case ItemRarity.Legendary: return new Color(1.00f, 0.48f, 0.14f, 1.0f);
            default: return new Color(0.66f, 0.66f, 0.70f, 1.0f);
        }
    }
}

[CreateAssetMenu(fileName = "Item", menuName = "Rebirth/Items/Item")]
public class ItemDefinition : ScriptableObject
{
    public string displayName;

    public Vector2Int gridSize = Vector2Int.one;
    [FormerlySerializedAs("equipSlot")]
    public EquipmentType equipmentType = EquipmentType.None;
    public ItemRarity rarity = ItemRarity.Common;
    [TextArea] public string description;

    [Header("Look")]
    public Texture2D icon;
    public Color tint = new Color(0.6f, 0.6f, 0.62f, 1.0f);
    public GameObject groundPrefab;

    [Header("Equipped")]
    public AbilityDefinition ability;

    public ModifierSet modifiers = new ModifierSet();

    [Header("Tags")]
    [SerializeField] private Tags extraTags;

    protected virtual Tags InherentTags => default;

    public Tags AllTags => InherentTags.Plus(extraTags).Plus(DamageTags);

    protected virtual Damage DamageForTags => modifiers != null ? modifiers.addedDamage : default;

    private Tags DamageTags
    {
        get
        {
            Tags found = default;
            foreach (DamageAmount entry in DamageForTags.Amounts) found = found.Plus(entry.type.ToTag());
            return found;
        }
    }

    public bool IsEquippable => equipmentType != EquipmentType.None;

    public virtual string TypeLabel => IsEquippable ? "Equipment" : "Item";
    public virtual string SubtypeLabel => IsEquippable ? EquipmentTypes.Label(equipmentType) : null;
}

public class Item
{
    public readonly ItemDefinition Definition;

    public readonly string InstanceId;

    public Item(ItemDefinition definition) : this(definition, Guid.NewGuid().ToString("N"))
    {
    }

    public Item(ItemDefinition definition, string instanceId)
    {
        Definition = definition;
        InstanceId = string.IsNullOrEmpty(instanceId) ? Guid.NewGuid().ToString("N") : instanceId;
    }

    public Vector2Int Size => Definition.gridSize;
    public EquipmentType EquipmentType => Definition.equipmentType;
    public string DisplayName => Definition.displayName;
}
