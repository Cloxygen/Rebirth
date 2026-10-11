using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CharacterLoad
{
    private readonly Inventory inventory;
    private readonly EquipmentLoadout loadout;
    private readonly Wallet wallet;
    private readonly LearnedAbilities learnedAbilities;

    public CharacterLoad(GameObject body)
    {
        inventory = body.GetComponent<Inventory>();
        loadout = body.GetComponent<EquipmentLoadout>();
        wallet = body.GetComponent<Wallet>();
        learnedAbilities = body.GetComponent<LearnedAbilities>();
    }

    public void PutBack(CharacterRows rows)
    {
        PutBackItems(rows.Items.Values);
        wallet.SetEssence(rows.Essence);
        learnedAbilities.SetLearned(rows.AbilityNames);
    }

    private void PutBackItems(IEnumerable<ItemRow> items)
    {
        inventory.BeginBatch();
        EmptyBody();
        foreach (ItemRow row in items.OrderBy(IsHeld)) PutBackItem(row);
        inventory.EndBatch();
    }

    private void EmptyBody()
    {
        inventory.Clear();
        loadout.Clear();
        inventory.SetCursor(null);
    }

    private static bool IsHeld(ItemRow row) => row.Slot == null && row.BagX == null;

    private void PutBackItem(ItemRow row)
    {
        Item item = RebuildItem(row);
        if (item == null) return;

        if (row.Slot != null) Wear(item, row);
        else if (row.BagX != null) PutInBag(item, new Vector2Int(row.BagX.Value, row.BagY.Value));
        else Stow(item);
    }

    private void PutInBag(Item item, Vector2Int bagCell)
    {
        if (inventory.CanPlace(item, bagCell)) inventory.PlaceAt(item, bagCell);
        else Stow(item);
    }

    private void Wear(Item item, ItemRow row)
    {
        EquipSlot slot = Enum.TryParse(row.Slot, out SlotId slotId) ? loadout.SlotWithId(slotId) : null;
        if (slot == null) StowFromUnknownSlot(item, row);
        else WearIn(slot, item, row.Template);
    }

    private void StowFromUnknownSlot(Item item, ItemRow row)
    {
        Debug.LogWarning($"CharacterLoad: '{row.Slot}' is not an equipment slot; {row.Template} goes in the bag.", inventory);
        Stow(item);
    }

    private void WearIn(EquipSlot slot, Item item, string template)
    {
        Item refused = loadout.Equip(slot, item);
        if (refused == null) return;

        Debug.LogWarning($"CharacterLoad: {slot.Id} refused {template}; it goes in the bag.", inventory);
        Stow(refused);
    }

    private void Stow(Item item)
    {
        if (inventory.TryAddItem(item)) return;

        if (inventory.OnCursor == null) KeepOnCursor(item);
        else Debug.LogError($"CharacterLoad: no room anywhere for {item.DisplayName} ({item.InstanceId}); it is lost.", inventory);
    }

    private void KeepOnCursor(Item item)
    {
        Debug.LogWarning($"CharacterLoad: the bag is full, so {item.DisplayName} ({item.InstanceId}) stays held.", inventory);
        inventory.SetCursor(item);
    }

    private Item RebuildItem(ItemRow row)
    {
        ItemDefinition definition = ItemDatabase.Instance != null ? ItemDatabase.Instance.Find(row.Template) : null;
        if (definition == null) ReportMissingAsset(row);
        return definition == null ? null : new Item(definition, row.InstanceId);
    }

    private void ReportMissingAsset(ItemRow row)
    {
        Debug.LogError($"CharacterLoad: no item asset named '{row.Template}'. It was renamed, deleted, or the database needs rebuilding.", inventory);
    }
}
