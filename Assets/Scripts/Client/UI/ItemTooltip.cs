public static class ItemTooltip
{
    public static void Describe(ItemDefinition item, TooltipContent into)
    {
        into.Clear();
        into.title = item.displayName;
        into.titleColor = item.rarity.ToColor();
        into.rarity = item.rarity.ToString();
        into.type = item.TypeLabel;
        into.subtype = item.SubtypeLabel;
        ItemStatLines.Describe(item, into.lines);
        into.description = item.description;
    }
}
