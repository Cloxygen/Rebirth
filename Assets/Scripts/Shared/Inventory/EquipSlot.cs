public enum SlotId
{
    None,
    Head,
    Amulet,
    Trinket,
    Cape,
    Weapon,
    Offhand,
    Chest,
    Pants,
    Gloves,
    Ring1,
    Ring2,
    Belt,
    Boots,
}

public class EquipSlot
{
    public readonly SlotId Id;
    public readonly EquipmentType AcceptedType;

    public Item Item { get; private set; }

    public EquipSlot(SlotId id, EquipmentType acceptedType)
    {
        Id = id;
        AcceptedType = acceptedType;
    }

    public bool IsEmpty => Item == null;
    public ItemDefinition Definition => Item != null ? Item.Definition : null;
    public AbilityDefinition Ability => Definition != null ? Definition.ability : null;
    public ModifierSet Modifiers => Definition != null ? Definition.modifiers : null;

    public bool Accepts(Item item) =>
        item != null && AcceptedType != EquipmentType.None && item.EquipmentType == AcceptedType;

    public Item Equip(Item item)
    {
        if (!Accepts(item)) return item;

        Item displaced = Item;
        Item = item;
        return displaced;
    }

    public Item Unequip()
    {
        Item removed = Item;
        Item = null;
        return removed;
    }
}
