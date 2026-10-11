public enum EquipmentType
{
    None,
    Helmet,
    Amulet,
    Trinket,
    Cape,
    Weapon,
    Offhand,
    Chest,
    Pants,
    Gloves,
    Ring,
    Belt,
    Boots,
}

public static class EquipmentTypes
{
    public static string Label(EquipmentType type)
    {
        switch (type)
        {
            case EquipmentType.Offhand: return "Off Hand";
            case EquipmentType.None: return string.Empty;
            default: return type.ToString();
        }
    }
}
