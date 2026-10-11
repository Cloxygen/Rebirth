using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LearnedAbilities))]
public partial class ActionBar : NetworkBehaviour
{
    [System.Serializable]
    public class AbilitySlot
    {
        public string label = "Primary";
        public string keyLabel = "1";
        public InputAction action;
        public bool usesEquippedWeapon;
        public AbilityDefinition ability;

        [System.NonSerialized] public Ability Instance;
        [System.NonSerialized] public bool Usable = true;

        [System.NonSerialized] public bool AwaitingPress;

        public bool IsFilled => Instance != null;
        public Sprite Icon => ability != null ? ability.icon : null;
        public float CooldownFraction => Instance != null ? Instance.CooldownFraction : 0.0f;
    }

    [Header("Slots")]
    [SerializeField]
    private AbilitySlot[] slots = new AbilitySlot[8];

    private Health health;
    private PlayerBlock playerBlock;
    private PlayerInteract playerPickup;
    private LearnedAbilities learnedAbilities;
    private EquipmentLoadout equipmentLoadout;
    private ResourcePool resourcePool;

    private byte heldSlotBits;
    private byte pressedSlotBits;

    private byte receivedHeldBits;
    private byte receivedPressedBits;

    public IReadOnlyList<AbilitySlot> Slots => slots;

    private void Awake()
    {
        health = GetComponent<Health>();
        playerBlock = GetComponent<PlayerBlock>();
        playerPickup = GetComponent<PlayerInteract>();
        learnedAbilities = GetComponent<LearnedAbilities>();
        equipmentLoadout = GetComponent<EquipmentLoadout>();
        resourcePool = GetComponent<ResourcePool>();

        boundAbilityNames.OnChange += HandleBindingsChanged;

        foreach (AbilitySlot slot in slots)
        {
            if (slot == null) continue;

            if (slot.usesEquippedWeapon && slot.action.bindings.Count == 0)
            {
                slot.action.AddBinding("<Mouse>/leftButton");
                slot.action.AddBinding("<Gamepad>/rightShoulder");
            }

            PointSlotAtLiveAbility(slot);
        }
    }

    private void OnDestroy()
    {
        boundAbilityNames.OnChange -= HandleBindingsChanged;
    }

    private void OnEnable()
    {
        foreach (AbilitySlot slot in slots)
        {
            if (slot != null) slot.action.Enable();
        }

        if (equipmentLoadout != null) equipmentLoadout.onChanged.AddListener(RefreshUsableSlots);
    }

    private void OnDisable()
    {
        if (equipmentLoadout != null) equipmentLoadout.onChanged.RemoveListener(RefreshUsableSlots);

        foreach (AbilitySlot slot in slots)
        {
            if (slot != null) slot.action.Disable();
        }

        StopRunningAbility();
    }

    private void Update()
    {
        if (IsOwner) CollectSlotInput();

        DrawAbilities(Time.deltaTime);
    }

    public void TakeSlotInput(out byte held, out byte pressed)
    {
        held = heldSlotBits;
        pressed = pressedSlotBits;

        pressedSlotBits = 0;
    }

    public void ReceiveSlotInput(byte held, byte pressed)
    {
        receivedHeldBits = held;

        receivedPressedBits |= pressed;
    }

    private bool IsSlotHeld(AbilitySlot slot) => (receivedHeldBits & SlotBit(slot)) != 0;

    private bool WasSlotPressed(AbilitySlot slot) => (receivedPressedBits & SlotBit(slot)) != 0;

    private int SlotBit(AbilitySlot slot)
    {
        int index = IndexOf(slot);

        return index >= 0 && index < 8 ? 1 << index : 0;
    }

    private void CollectSlotInput()
    {
        heldSlotBits = 0;

        bool overInterface = UIPointer.IsOverBlockingUI();

        for (int index = 0; index < slots.Length && index < 8; index++)
        {
            AbilitySlot slot = slots[index];
            if (slot == null) continue;

            bool claimedByPickup = playerPickup != null && playerPickup.ConsumesClick
                                   && IsPointerBound(slot);

            if (overInterface || claimedByPickup) continue;

            if (slot.action.IsPressed()) heldSlotBits |= (byte)(1 << index);
            if (slot.action.WasPressedThisFrame()) pressedSlotBits |= (byte)(1 << index);
        }
    }

    private static bool IsPointerBound(AbilitySlot slot)
    {
        foreach (InputControl control in slot.action.controls)
        {
            if (control.device is Mouse) return true;
        }

        return false;
    }

    private int IndexOf(AbilitySlot slot)
    {
        if (slots == null) return -1;

        for (int index = 0; index < slots.Length; index++)
        {
            if (slots[index] == slot) return index;
        }

        return -1;
    }
}
