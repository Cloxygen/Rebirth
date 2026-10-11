using FishNet.Object;
using UnityEngine;

[RequireComponent(typeof(Inventory))]
[RequireComponent(typeof(EquipmentLoadout))]
[RequireComponent(typeof(Wallet))]
[RequireComponent(typeof(LearnedAbilities))]
public class PlayerSave : NetworkBehaviour
{
    private const int NoCharacter = -1;
    private const float SecondsBetweenSaves = 1.0f;
    private const float SecondsAfterFailedSave = 30.0f;

    private Inventory inventory;
    private EquipmentLoadout loadout;
    private Wallet wallet;
    private LearnedAbilities learnedAbilities;

    private CharacterStore savedCharacter;
    private int characterId = NoCharacter;
    private bool changedSinceSave;
    private float secondsUntilSave;

    private bool NeedsSave => changedSinceSave || wallet.Essence != savedCharacter.SavedEssence;

    private void Awake()
    {
        inventory = GetComponent<Inventory>();
        loadout = GetComponent<EquipmentLoadout>();
        wallet = GetComponent<Wallet>();
        learnedAbilities = GetComponent<LearnedAbilities>();
    }

    public override void OnStartServer()
    {
        ServerDatabase database = (NetworkManager.ServerManager.GetAuthenticator() as LoginHandler)?.Database;
        int ownedCharacterId = Owner.CustomData is int idFromLogin ? idFromLogin : NoCharacter;
        if (database == null || ownedCharacterId == NoCharacter) return;
        StartSaving(database, ownedCharacterId);
    }

    private void StartSaving(ServerDatabase database, int ownedCharacterId)
    {
        characterId = ownedCharacterId;
        savedCharacter = new CharacterStore(database.Connection, ownedCharacterId);
        secondsUntilSave = Random.Range(0.0f, SecondsBetweenSaves);
        LoadCharacter();
        ListenForChanges();
    }

    private void LoadCharacter()
    {
        CharacterRows rows = savedCharacter.Load();
        if (rows == null) savedCharacter = null;
        else PutBack(rows);
    }

    private void PutBack(CharacterRows rows)
    {
        new CharacterLoad(gameObject).PutBack(rows);
        changedSinceSave = true;
    }

    private void ListenForChanges()
    {
        inventory.onChanged.AddListener(MarkChanged);
        loadout.onChanged.AddListener(MarkChanged);
        learnedAbilities.LearnedChanged += MarkChanged;
    }

    private void StopListening()
    {
        inventory.onChanged.RemoveListener(MarkChanged);
        loadout.onChanged.RemoveListener(MarkChanged);
        learnedAbilities.LearnedChanged -= MarkChanged;
    }

    private void MarkChanged()
    {
        changedSinceSave = true;
    }

    private void Update()
    {
        if (savedCharacter == null) return;
        secondsUntilSave -= Time.deltaTime;
        if (secondsUntilSave > 0.0f) return;
        secondsUntilSave += SecondsBetweenSaves;
        SaveIfChanged();
    }

    private void SaveIfChanged()
    {
        if (savedCharacter == null || !NeedsSave) return;
        if (savedCharacter.Write(TakeRows())) changedSinceSave = false;
        else WaitLongerAfterFailure();
    }

    private void WaitLongerAfterFailure()
    {
        secondsUntilSave = SecondsAfterFailedSave;
    }

    private CharacterRows TakeRows()
    {
        var rows = new CharacterRows { Essence = wallet.Essence };
        rows.AbilityNames.UnionWith(learnedAbilities.LearnedNames);
        AddItems(rows);
        return rows;
    }

    private void AddItems(CharacterRows rows)
    {
        AddBagItems(rows);
        AddWornItems(rows);
        AddHeldItem(rows);
    }

    private void AddBagItems(CharacterRows rows)
    {
        foreach (Inventory.PlacedItem placedItem in inventory.Placed)
            rows.AddItem(BagRow(placedItem));
    }

    private void AddWornItems(CharacterRows rows)
    {
        foreach (EquipSlot slot in loadout.Slots)
            if (!slot.IsEmpty) rows.AddItem(WornRow(slot));
    }

    private void AddHeldItem(CharacterRows rows)
    {
        Item heldItem = inventory.OnCursor;
        if (heldItem != null) rows.AddItem(RowFor(heldItem));
    }

    private ItemRow BagRow(Inventory.PlacedItem placedItem)
    {
        ItemRow row = RowFor(placedItem.Item);
        row.BagX = placedItem.Origin.x;
        row.BagY = placedItem.Origin.y;
        return row;
    }

    private ItemRow WornRow(EquipSlot slot)
    {
        ItemRow row = RowFor(slot.Item);
        row.Slot = slot.Id.ToString();
        return row;
    }

    private ItemRow RowFor(Item item)
    {
        return new ItemRow { InstanceId = item.InstanceId, CharacterId = characterId, Template = item.Definition.name };
    }

    public override void OnStopServer()
    {
        SaveIfChanged();
        StopListening();
        savedCharacter = null;
    }

    private void OnApplicationQuit()
    {
        SaveIfChanged();
        savedCharacter = null;
    }
}
