using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Rebirth/Items/Item Database")]
public class ItemDatabase : ScriptableObject
{
    private const string ResourcePath = "ItemDatabase";

    private static ItemDatabase loaded;

    [SerializeField] private List<ItemDefinition> items = new List<ItemDefinition>();

    public IReadOnlyList<ItemDefinition> Items => items;

    public static ItemDatabase Instance
    {
        get
        {
            if (loaded == null)
            {
                loaded = Resources.Load<ItemDatabase>(ResourcePath);
                if (loaded == null)
                {
                    Debug.LogError($"ItemDatabase: nothing at Resources/{ResourcePath}, so no saved or received item can be matched to an asset.");
                }
            }

            return loaded;
        }
    }

    public ItemDefinition Find(string template)
    {
        if (string.IsNullOrEmpty(template)) return null;

        foreach (ItemDefinition item in items)
        {
            if (item != null && item.name == template) return item;
        }

        return null;
    }
}
