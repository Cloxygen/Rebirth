using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public partial class ActionBar
{
    private readonly SyncList<string> boundAbilityNames = new SyncList<string>();

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsServerInitialized) ApplyBindings();
    }

    private void HandleBindingsChanged(SyncListOperation operation, int index, string previous,
                                       string next, bool asServer)
    {
        if (asServer) return;

        if (operation != SyncListOperation.Complete) return;

        ApplyBindings();
    }

    private void ApplyBindings()
    {
        for (int index = 0; index < slots.Length && index < boundAbilityNames.Count; index++)
        {
            if (slots[index] == null) continue;

            BindSlot(slots[index], LearnedAbilityNamed(boundAbilityNames[index]));
        }
    }

    private void WriteBindings()
    {
        boundAbilityNames.Clear();

        foreach (AbilitySlot slot in slots)
        {
            boundAbilityNames.Add(slot != null && slot.ability != null ? slot.ability.name : string.Empty);
        }
    }

    private AbilityDefinition LearnedAbilityNamed(string abilityName)
    {
        if (string.IsNullOrEmpty(abilityName) || learnedAbilities == null) return null;

        foreach (AbilityDefinition ability in learnedAbilities.Available)
        {
            if (ability != null && ability.name == abilityName) return ability;
        }

        return null;
    }

    public void RequestBindAbility(int slotIndex, string abilityName)
    {
        if (IsServerInitialized) BindOnServer(slotIndex, abilityName);
        else ServerBindAbility(slotIndex, abilityName);
    }

    [ServerRpc]
    private void ServerBindAbility(int slotIndex, string abilityName) => BindOnServer(slotIndex, abilityName);

    private void BindOnServer(int slotIndex, string abilityName)
    {
        BindAbility(slotIndex, LearnedAbilityNamed(abilityName));
    }

    public bool ReplaceAbility(AbilityDefinition oldAbility, AbilityDefinition newAbility)
    {
        if (!IsServerInitialized) return false;
        if (slots == null || oldAbility == null) return false;

        bool replaced = false;

        for (int index = 0; index < slots.Length; index++)
        {
            if (slots[index] == null || slots[index].ability != oldAbility) continue;

            BindAbility(index, newAbility);
            replaced = true;
        }

        return replaced;
    }

    public void BindAbility(int slotIndex, AbilityDefinition ability)
    {
        if (!IsServerInitialized)
        {
            Debug.LogWarning($"ActionBar: a client cannot bind a slot on {name} — ask through RequestBindAbility.", this);
            return;
        }

        if (slots == null || slotIndex < 0 || slotIndex >= slots.Length) return;

        AbilitySlot slot = slots[slotIndex];
        if (slot == null) return;

        BindSlot(slot, ability);
        WriteBindings();
    }

    private void BindSlot(AbilitySlot slot, AbilityDefinition ability)
    {
        if (slot.ability == ability) return;

        slot.ability = ability;
        slot.AwaitingPress = true;

        PointSlotAtLiveAbility(slot);
    }

    private void PointSlotAtLiveAbility(AbilitySlot slot)
    {
        slot.Instance = learnedAbilities != null ? learnedAbilities.LiveAbility(slot.ability) : null;
        slot.Usable = CanUseNow(slot);
    }
}
