using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LootTable", menuName = "Rebirth/Loot Table")]
public class LootTable : ScriptableObject
{
    [Serializable]
    public class ItemEntry
    {
        public ItemDefinition item;
        [Range(0.0f, 1.0f)] public float chance = 1.0f;
        public int minCount = 1;
        public int maxCount = 1;
    }

    [Header("Essence")]
    [SerializeField] private GameObject essenceMotePrefab;
    [SerializeField] private int minEssence = 1;
    [SerializeField] private int maxEssence = 3;

    [Header("Items")]
    [SerializeField] private List<ItemEntry> items = new List<ItemEntry>();

    [Header("Scatter")]
    [SerializeField] private float scatterRadius = 0.5f;

    public void Drop(Vector3 origin, Transform ignoreRoot)
    {
        int essence = UnityEngine.Random.Range(minEssence, maxEssence + 1);
        for (int i = 0; i < essence; i++)
        {
            EssencePickup.Spawn(essenceMotePrefab, origin, Scatter(), 1, ignoreRoot);
        }

        foreach (ItemEntry entry in items)
        {
            if (entry == null || entry.item == null) continue;
            if (UnityEngine.Random.value > entry.chance) continue;

            int count = UnityEngine.Random.Range(entry.minCount, entry.maxCount + 1);
            for (int i = 0; i < count; i++)
            {
                GroundItem.Spawn(origin, Scatter(), new Item(entry.item), ignoreRoot);
            }
        }
    }

    private Vector3 Scatter()
    {
        Vector2 offset = UnityEngine.Random.insideUnitCircle * scatterRadius;
        return new Vector3(offset.x, 0.0f, offset.y);
    }
}
