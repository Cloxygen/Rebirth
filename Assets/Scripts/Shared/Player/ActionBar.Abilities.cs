using System.Collections.Generic;

public partial class ActionBar
{
    private Ability runningAbility;
    private AbilitySlot slotThatStartedIt;

    public bool IsAttacking => runningAbility != null;

    public void AdvanceAbilities(float seconds)
    {
        if (!IsOwner && !IsServerInitialized) return;

        ClaimFreshPresses();
        ActOnPresses();
        AdvanceLiveAbilities(seconds);

        receivedPressedBits = 0;
    }

    private void ActOnPresses()
    {
        if (health != null && health.IsDead) StopRunningAbility();
        else StartRequestedAbility();
    }

    private void AdvanceLiveAbilities(float seconds)
    {
        if (learnedAbilities == null) return;

        AdvanceEach(learnedAbilities.LiveAbilities, seconds);
        ForgetFinishedAbility();
    }

    private void AdvanceEach(IReadOnlyList<Ability> abilities, float seconds)
    {
        for (int index = 0; index < abilities.Count; index++)
        {
            abilities[index].Advance(seconds, ButtonFor(abilities[index]));
        }
    }

    private AbilityButton ButtonFor(Ability ability)
    {
        if (ability != runningAbility) return default;
        if (slotThatStartedIt.Instance != ability) return default;

        return new AbilityButton(IsSlotHeld(slotThatStartedIt), WasSlotPressed(slotThatStartedIt));
    }

    private void ForgetFinishedAbility()
    {
        if (runningAbility == null || runningAbility.IsRunning) return;

        runningAbility = null;
        slotThatStartedIt = null;
    }

    public void DrawAbilities(float seconds)
    {
        if (!IsOwner && !IsServerInitialized) return;

        runningAbility?.Draw(seconds);
    }

    private void ClaimFreshPresses()
    {
        foreach (AbilitySlot slot in slots)
        {
            if (slot != null && slot.AwaitingPress && WasSlotPressed(slot)) slot.AwaitingPress = false;
        }
    }

    private void StartRequestedAbility()
    {
        if (IsAttacking) return;

        AbilitySlot requested = FirstSlotWantingToFire();
        if (requested != null) StartAbilityIn(requested);
    }

    private AbilitySlot FirstSlotWantingToFire()
    {
        foreach (AbilitySlot slot in slots)
        {
            if (WantsToFire(slot)) return slot;
        }

        return null;
    }

    private void StartAbilityIn(AbilitySlot slot)
    {
        runningAbility = slot.Instance;
        slotThatStartedIt = slot;

        if (IsServerInitialized && resourcePool != null) resourcePool.Spend(slot.ability.manaCost);

        runningAbility.Begin();
    }

    private bool WantsToFire(AbilitySlot slot)
    {
        if (slot == null || slot.Instance == null) return false;
        if (!slot.Usable || slot.AwaitingPress || !slot.Instance.IsReady) return false;
        if (playerBlock != null && playerBlock.Guarding) return false;
        if (resourcePool != null && !resourcePool.CanAfford(slot.ability.manaCost)) return false;

        return slot.ability.repeatsWhileHeld ? IsSlotHeld(slot) : WasSlotPressed(slot);
    }

    private void RefreshUsableSlots()
    {
        foreach (AbilitySlot slot in slots)
        {
            if (slot != null) slot.Usable = CanUseNow(slot);
        }
    }

    private bool CanUseNow(AbilitySlot slot)
    {
        if (slot.ability == null || equipmentLoadout == null) return true;

        return slot.ability.CanBeUsedWith(equipmentLoadout.HeldTags);
    }

    private void StopRunningAbility()
    {
        runningAbility?.Stop();
        runningAbility = null;
        slotThatStartedIt = null;
    }
}
