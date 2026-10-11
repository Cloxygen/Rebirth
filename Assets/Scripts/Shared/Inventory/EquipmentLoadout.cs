using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Inventory))]
public class EquipmentLoadout : NetworkBehaviour
{
    public struct EquippedItem
    {
        public SlotId Slot;
        public string Template;
        public string InstanceId;
    }

    [SerializeField] private AbilityDefinition unarmedAbility;

    [SerializeField] private Tags unarmedTags = new Tags(Tag.Melee, Tag.Blunt);

    [SerializeField] private Damage unarmedDamage = new Damage(DamageType.Blunt, 1.0f);

    public UnityEvent onChanged = new UnityEvent();

    private static readonly (SlotId Id, EquipmentType Accepts)[] Doll =
    {
        (SlotId.Head, EquipmentType.Helmet),
        (SlotId.Amulet, EquipmentType.Amulet),
        (SlotId.Trinket, EquipmentType.Trinket),
        (SlotId.Cape, EquipmentType.Cape),
        (SlotId.Weapon, EquipmentType.Weapon),
        (SlotId.Offhand, EquipmentType.Offhand),
        (SlotId.Chest, EquipmentType.Chest),
        (SlotId.Pants, EquipmentType.Pants),
        (SlotId.Gloves, EquipmentType.Gloves),
        (SlotId.Ring1, EquipmentType.Ring),
        (SlotId.Ring2, EquipmentType.Ring),
        (SlotId.Belt, EquipmentType.Belt),
        (SlotId.Boots, EquipmentType.Boots),
    };

    private readonly SyncList<EquippedItem> worn = new SyncList<EquippedItem>();

    private readonly List<EquipSlot> slots = new List<EquipSlot>();
    private ItemDefinition[] appliedDefinitions;
    private ModifierSet[] appliedSets;

    private Inventory inventory;
    private Modifiers modifiers;
    private LearnedAbilities learnedAbilities;
    private ActionBar actionBar;

    private readonly List<AbilityDefinition> wornAbilities = new List<AbilityDefinition>();
    private readonly List<AbilityDefinition> grantedAbilities = new List<AbilityDefinition>();
    private AbilityDefinition grantedHandsAbility;
    private bool hasStarted;

    public IReadOnlyList<EquipSlot> Slots => slots;

    private void Awake()
    {
        BuildSlots();
        appliedDefinitions = new ItemDefinition[slots.Count];
        appliedSets = new ModifierSet[slots.Count];
        modifiers = GetComponent<Modifiers>();
        learnedAbilities = GetComponent<LearnedAbilities>();
        actionBar = GetComponent<ActionBar>();
        inventory = GetComponent<Inventory>();

        worn.OnChange += HandleWornChanged;
    }

    private void OnDestroy()
    {
        worn.OnChange -= HandleWornChanged;
    }

    public override void OnStartClient()
    {
        if (!IsServerInitialized) RebuildFromWorn();
    }

    private void HandleWornChanged(SyncListOperation operation, int index, EquippedItem previous,
                                   EquippedItem next, bool asServer)
    {
        if (asServer) return;

        if (operation != SyncListOperation.Complete) return;

        RebuildFromWorn();
    }

    private void RebuildFromWorn()
    {
        foreach (EquipSlot slot in slots) slot.Unequip();

        foreach (EquippedItem entry in worn)
        {
            EquipSlot slot = SlotWithId(entry.Slot);
            if (slot == null) continue;

            ItemDefinition definition = ItemDatabase.Instance != null
                ? ItemDatabase.Instance.Find(entry.Template)
                : null;

            if (definition == null)
            {
                Debug.LogWarning($"EquipmentLoadout: no item asset named '{entry.Template}', so it is not worn.");
                continue;
            }

            slot.Equip(new Item(definition, entry.InstanceId));
        }

        onChanged.Invoke();
    }

    private void WriteWorn()
    {
        worn.Clear();

        foreach (EquipSlot slot in slots)
        {
            if (slot.IsEmpty) continue;

            worn.Add(new EquippedItem
            {
                Slot = slot.Id,
                Template = slot.Item.Definition.name,
                InstanceId = slot.Item.InstanceId
            });
        }
    }

    private void BuildSlots()
    {
        foreach ((SlotId id, EquipmentType accepts) in Doll) slots.Add(new EquipSlot(id, accepts));
    }

    private void OnEnable()
    {
        onChanged.AddListener(ApplyEquipment);

        if (hasStarted) ApplyEquipment();
    }

    private void OnDisable()
    {
        onChanged.RemoveListener(ApplyEquipment);
    }

    private void Start()
    {
        hasStarted = true;
        ApplyEquipment();
    }

    public EquipSlot SlotWithId(SlotId id)
    {
        foreach (EquipSlot slot in slots)
        {
            if (slot.Id == id) return slot;
        }
        return null;
    }

    public EquipSlot SlotToEquipInto(Item item)
    {
        EquipSlot empty = FirstEmptySlotFor(item);
        return empty != null ? empty : SlotToReplaceFor(item);
    }

    private EquipSlot FirstEmptySlotFor(Item item)
    {
        foreach (EquipSlot slot in slots)
        {
            if (slot.IsEmpty && slot.Accepts(item)) return slot;
        }
        return null;
    }

    private EquipSlot SlotToReplaceFor(Item item)
    {
        foreach (EquipSlot slot in slots)
        {
            if (slot.Accepts(item)) return slot;
        }
        return null;
    }

    public void RequestEquipFromBag(string instanceId)
    {
        if (IsServerInitialized) EquipFromBagOnServer(instanceId);
        else ServerEquipFromBag(instanceId);
    }

    public void RequestUnequipToBag(SlotId id)
    {
        if (IsServerInitialized) UnequipToBagOnServer(id);
        else ServerUnequipToBag(id);
    }

    public void RequestEquipFromCursor(SlotId id)
    {
        if (IsServerInitialized) EquipFromCursorOnServer(id);
        else ServerEquipFromCursor(id);
    }

    public void RequestUnequipToCursor(SlotId id)
    {
        if (IsServerInitialized) UnequipToCursorOnServer(id);
        else ServerUnequipToCursor(id);
    }

    [ServerRpc]
    private void ServerEquipFromBag(string instanceId) => EquipFromBagOnServer(instanceId);

    [ServerRpc]
    private void ServerUnequipToBag(SlotId id) => UnequipToBagOnServer(id);

    [ServerRpc]
    private void ServerEquipFromCursor(SlotId id) => EquipFromCursorOnServer(id);

    [ServerRpc]
    private void ServerUnequipToCursor(SlotId id) => UnequipToCursorOnServer(id);

    private void EquipFromCursorOnServer(SlotId id)
    {
        Item carried = inventory.OnCursor;
        if (carried == null) return;

        EquipSlot slot = SlotWithId(id);

        if (slot == null || !slot.Accepts(carried)) return;

        inventory.SetCursor(Equip(slot, carried));
    }

    private void UnequipToCursorOnServer(SlotId id)
    {
        if (inventory.OnCursor != null) return;

        EquipSlot slot = SlotWithId(id);
        if (slot == null || slot.IsEmpty) return;

        inventory.SetCursor(Unequip(slot));
    }

    private void EquipFromBagOnServer(string instanceId)
    {
        Inventory.PlacedItem inBag = inventory.FindByInstanceId(instanceId);
        if (inBag == null) return;

        EquipSlot slot = SlotToEquipInto(inBag.Item);
        if (slot == null) return;

        Item wanted = inBag.Item;

        inventory.BeginBatch();
        inventory.RemovePlaced(inBag);

        Item displaced = Equip(slot, wanted);

        if (displaced != null && !inventory.TryAddItem(displaced))
        {
            Equip(slot, displaced);
            inventory.TryAddItem(wanted);
        }

        inventory.EndBatch();
    }

    private void UnequipToBagOnServer(SlotId id)
    {
        EquipSlot slot = SlotWithId(id);
        if (slot == null || slot.IsEmpty) return;

        Item removed = slot.Item;

        inventory.BeginBatch();
        Unequip(slot);

        if (!inventory.TryAddItem(removed)) Equip(slot, removed);

        inventory.EndBatch();
    }

    public Item Equip(EquipSlot slot, Item item)
    {
        if (!ServerOwnsThis("equip an item")) return item;
        if (slot == null || !slot.Accepts(item)) return item;

        Item displaced = slot.Equip(item);
        Announce();
        return displaced;
    }

    public Item Unequip(EquipSlot slot)
    {
        if (!ServerOwnsThis("unequip an item")) return null;
        if (slot == null || slot.IsEmpty) return null;

        Item removed = slot.Unequip();
        Announce();
        return removed;
    }

    public void Clear()
    {
        if (!ServerOwnsThis("strip the character")) return;

        foreach (EquipSlot slot in slots) slot.Unequip();
        Announce();
    }

    private bool ServerOwnsThis(string what)
    {
        if (IsServerInitialized) return true;

        Debug.LogWarning($"EquipmentLoadout: a client cannot {what} for {name} - ask through the Request methods.", this);
        return false;
    }

    private void Announce()
    {
        WriteWorn();
        onChanged.Invoke();
    }

    public Tags HeldTags
    {
        get
        {
            ItemDefinition held = HeldWeapon();
            return held != null ? held.AllTags : unarmedTags;
        }
    }

    public Damage HeldDamage
    {
        get
        {
            Weapon weapon = HeldWeapon() as Weapon;
            return weapon != null ? weapon.damage : unarmedDamage;
        }
    }

    private ItemDefinition HeldWeapon()
    {
        EquipSlot weaponSlot = SlotWithId(SlotId.Weapon);
        return weaponSlot != null ? weaponSlot.Definition : null;
    }

    private void ApplyEquipment()
    {
        ApplyWornModifiers();
        ApplyWornAbilities();
    }

    private void ApplyWornModifiers()
    {
        if (modifiers == null) return;

        for (int index = 0; index < slots.Count; index++) ApplySlotModifiers(index);
    }

    private void ApplySlotModifiers(int index)
    {
        ItemDefinition worn = slots[index].Definition;
        if (worn == appliedDefinitions[index]) return;

        appliedDefinitions[index] = worn;

        if (appliedSets[index] != null) modifiers.Unregister(appliedSets[index]);
        appliedSets[index] = worn != null && worn.modifiers != null ? worn.modifiers.Copy() : null;
        if (appliedSets[index] != null) modifiers.Register(appliedSets[index]);
    }

    private void ApplyWornAbilities()
    {
        AbilityDefinition hands = HandsAbility();
        CollectWornAbilities(hands);
        if (MatchesGranted(wornAbilities)) return;

        AbilityDefinition previousHands = grantedHandsAbility;
        grantedHandsAbility = hands;
        Regrant(wornAbilities);

        if (actionBar != null && previousHands != null && previousHands != hands)
        {
            actionBar.ReplaceAbility(previousHands, hands);
        }
    }

    private AbilityDefinition HandsAbility()
    {
        Weapon weapon = HeldWeapon() as Weapon;
        AbilityDefinition granted = weapon != null ? weapon.ability : null;
        return granted != null ? granted : unarmedAbility;
    }

    private void CollectWornAbilities(AbilityDefinition hands)
    {
        wornAbilities.Clear();
        if (hands != null) wornAbilities.Add(hands);

        foreach (EquipSlot slot in slots)
        {
            if (slot.Ability == null) continue;
            if (!wornAbilities.Contains(slot.Ability)) wornAbilities.Add(slot.Ability);
        }
    }

    private bool MatchesGranted(List<AbilityDefinition> abilities)
    {
        if (abilities.Count != grantedAbilities.Count) return false;

        for (int i = 0; i < abilities.Count; i++)
        {
            if (abilities[i] != grantedAbilities[i]) return false;
        }
        return true;
    }

    private void Regrant(List<AbilityDefinition> abilities)
    {
        grantedAbilities.Clear();
        grantedAbilities.AddRange(abilities);

        if (learnedAbilities == null) return;

        learnedAbilities.RevokeFrom(this);
        foreach (AbilityDefinition ability in abilities) learnedAbilities.Grant(this, ability);
    }
}
