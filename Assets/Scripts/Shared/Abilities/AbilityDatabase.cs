using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AbilityDatabase", menuName = "Rebirth/Abilities/Ability Database")]
public class AbilityDatabase : ScriptableObject
{
    private const string ResourcePath = "AbilityDatabase";

    private static AbilityDatabase loaded;

    [SerializeField] private List<AbilityDefinition> abilities = new List<AbilityDefinition>();

    public IReadOnlyList<AbilityDefinition> Abilities => abilities;

    public static AbilityDatabase Instance
    {
        get
        {
            if (loaded == null)
            {
                loaded = Resources.Load<AbilityDatabase>(ResourcePath);
                if (loaded == null)
                {
                    Debug.LogError($"AbilityDatabase: nothing at Resources/{ResourcePath}, so no learned ability can be matched to an asset.");
                }
            }

            return loaded;
        }
    }

    public AbilityDefinition Find(string abilityName)
    {
        if (string.IsNullOrEmpty(abilityName)) return null;

        foreach (AbilityDefinition ability in abilities)
        {
            if (ability != null && ability.name == abilityName) return ability;
        }

        return null;
    }
}
