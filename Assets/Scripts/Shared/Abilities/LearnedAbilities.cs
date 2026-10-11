using System;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class LearnedAbilities : NetworkBehaviour
{
    [SerializeField] private AbilityDefinition[] innate;

    private readonly SyncList<string> learned = new SyncList<string>();

    private readonly Dictionary<UnityEngine.Object, List<AbilityDefinition>> grants =
        new Dictionary<UnityEngine.Object, List<AbilityDefinition>>();
    private readonly List<AbilityDefinition> available = new List<AbilityDefinition>();

    public IReadOnlyList<AbilityDefinition> Available => available;

    private readonly List<Ability> liveAbilities = new List<Ability>();

    public IReadOnlyList<Ability> LiveAbilities => liveAbilities;

    public event Action Changed;

    public event Action LearnedChanged;

    public IEnumerable<string> LearnedNames => learned;

    private void Awake()
    {
        learned.OnChange += HandleLearnedChanged;
        Rebuild();
    }

    private void OnDestroy()
    {
        learned.OnChange -= HandleLearnedChanged;
        DiscardLiveAbilities();
    }

    public override void OnStartClient()
    {
        if (!IsServerInitialized) Rebuild();
    }

    private void HandleLearnedChanged(SyncListOperation operation, int index, string previous,
                                      string next, bool asServer)
    {
        if (asServer) return;
        if (operation != SyncListOperation.Complete) return;

        Rebuild();
    }

    public void Learn(AbilityDefinition ability)
    {
        if (!ServerOwnsThis("learn an ability") || ability == null) return;
        if (learned.Contains(ability.name)) return;

        learned.Add(ability.name);
        Rebuild();
        LearnedChanged?.Invoke();
    }

    public void Forget(AbilityDefinition ability)
    {
        if (!ServerOwnsThis("forget an ability") || ability == null) return;
        if (!learned.Remove(ability.name)) return;

        Rebuild();
        LearnedChanged?.Invoke();
    }

    public void SetLearned(IEnumerable<string> abilityNames)
    {
        if (!ServerOwnsThis("set what is known")) return;

        learned.Clear();
        foreach (string abilityName in abilityNames)
        {
            if (!string.IsNullOrEmpty(abilityName) && !learned.Contains(abilityName)) learned.Add(abilityName);
        }

        Rebuild();
        LearnedChanged?.Invoke();
    }

    private bool ServerOwnsThis(string what)
    {
        if (IsServerInitialized) return true;

        Debug.LogWarning($"LearnedAbilities: a client cannot {what} for {name}; the call did nothing.", this);
        return false;
    }

    public void Grant(UnityEngine.Object source, AbilityDefinition ability)
    {
        if (source == null || ability == null) return;

        if (!grants.TryGetValue(source, out List<AbilityDefinition> list))
        {
            list = new List<AbilityDefinition>();
            grants[source] = list;
        }

        if (list.Contains(ability)) return;

        list.Add(ability);
        Rebuild();
    }

    public void RevokeFrom(UnityEngine.Object source)
    {
        if (source == null || !grants.Remove(source)) return;

        Rebuild();
    }

    public bool Has(AbilityDefinition ability)
    {
        return ability != null && available.Contains(ability);
    }

    public Ability LiveAbility(AbilityDefinition ability)
    {
        if (ability == null) return null;

        return FindLiveAbility(ability) ?? CreateLiveAbility(ability);
    }

    private Ability FindLiveAbility(AbilityDefinition ability)
    {
        foreach (Ability liveAbility in liveAbilities)
        {
            if (liveAbility.Definition == ability) return liveAbility;
        }

        return null;
    }

    private Ability CreateLiveAbility(AbilityDefinition ability)
    {
        Ability liveAbility = ability.CreateAbility(gameObject);
        liveAbilities.Add(liveAbility);
        return liveAbility;
    }

    private void DiscardLiveAbilities()
    {
        foreach (Ability liveAbility in liveAbilities) liveAbility.Discard();

        liveAbilities.Clear();
    }

    private void Rebuild()
    {
        available.Clear();

        if (innate != null)
        {
            foreach (AbilityDefinition ability in innate)
            {
                if (ability != null && !available.Contains(ability)) available.Add(ability);
            }
        }

        foreach (string abilityName in learned)
        {
            AbilityDefinition ability = AbilityDatabase.Instance != null
                ? AbilityDatabase.Instance.Find(abilityName)
                : null;

            if (ability == null)
            {
                Debug.LogWarning($"LearnedAbilities: no ability asset named '{abilityName}', so it is not known.");
                continue;
            }

            if (!available.Contains(ability)) available.Add(ability);
        }

        foreach (List<AbilityDefinition> list in grants.Values)
        {
            foreach (AbilityDefinition ability in list)
            {
                if (ability != null && !available.Contains(ability)) available.Add(ability);
            }
        }

        Changed?.Invoke();
    }
}
