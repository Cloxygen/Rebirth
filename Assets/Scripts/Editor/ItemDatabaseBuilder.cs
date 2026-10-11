using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ItemDatabaseBuilder : AssetPostprocessor
{
    private const string DatabasePath = "Assets/Resources/ItemDatabase.asset";

    [MenuItem("Tools/Rebirth/Rebuild Item Database")]
    private static void RebuildFromMenu()
    {
        ItemDatabase database = Rebuild();
        if (database != null) Selection.activeObject = database;
    }

    private static void OnPostprocessAllAssets(string[] imported, string[] deleted,
                                               string[] moved, string[] movedFrom)
    {
        bool touchesItems = imported.Concat(deleted).Concat(moved).Concat(movedFrom)
            .Any(path => path != DatabasePath && path.EndsWith(".asset") &&
                         AssetDatabase.LoadAssetAtPath<ItemDefinition>(path) != null);

        if (!touchesItems && deleted.Length == 0) return;

        Rebuild();
    }

    private static ItemDatabase Rebuild()
    {
        ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(DatabasePath);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<ItemDatabase>();
            AssetDatabase.CreateAsset(database, DatabasePath);
        }

        List<ItemDefinition> found = AssetDatabase.FindAssets("t:ItemDefinition")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<ItemDefinition>)
            .ToList();

        if (found.Any(item => item == null))
        {
            Debug.LogWarning("ItemDatabase left as it was: an item asset could not be loaded, so rebuilding now would drop it.");
            return database;
        }

        List<ItemDefinition> items = found.OrderBy(item => item.name).ToList();

        SerializedObject serialized = new SerializedObject(database);
        SerializedProperty list = serialized.FindProperty("items");
        list.arraySize = items.Count;
        for (int i = 0; i < items.Count; i++)
        {
            list.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }

        if (serialized.ApplyModifiedPropertiesWithoutUndo())
        {
            AssetDatabase.SaveAssetIfDirty(database);
        }

        return database;
    }
}
