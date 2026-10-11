using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class AbilityDatabaseBuilder : AssetPostprocessor
{
    private const string DatabasePath = "Assets/Resources/AbilityDatabase.asset";

    [MenuItem("Tools/Rebirth/Rebuild Ability Database")]
    private static void RebuildFromMenu()
    {
        AbilityDatabase database = Rebuild();
        if (database != null) Selection.activeObject = database;
    }

    [InitializeOnLoadMethod]
    private static void RebuildOnLoad()
    {
        EditorApplication.delayCall += () => Rebuild();
    }

    private static void OnPostprocessAllAssets(string[] imported, string[] deleted,
                                               string[] moved, string[] movedFrom)
    {
        bool touchesAbilities = imported.Concat(deleted).Concat(moved).Concat(movedFrom)
            .Any(path => path != DatabasePath && path.EndsWith(".asset") &&
                         AssetDatabase.LoadAssetAtPath<AbilityDefinition>(path) != null);

        if (!touchesAbilities && deleted.Length == 0) return;

        Rebuild();
    }

    private static AbilityDatabase Rebuild()
    {
        AbilityDatabase database = AssetDatabase.LoadAssetAtPath<AbilityDatabase>(DatabasePath);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<AbilityDatabase>();
            AssetDatabase.CreateAsset(database, DatabasePath);
        }

        List<AbilityDefinition> found = AssetDatabase.FindAssets("t:AbilityDefinition")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<AbilityDefinition>)
            .ToList();

        if (found.Any(ability => ability == null))
        {
            Debug.LogWarning("AbilityDatabase left as it was: an ability asset could not be loaded, so rebuilding now would drop it.");
            return database;
        }

        List<AbilityDefinition> abilities = found.OrderBy(ability => ability.name).ToList();

        SerializedObject serialized = new SerializedObject(database);
        SerializedProperty list = serialized.FindProperty("abilities");
        list.arraySize = abilities.Count;
        for (int index = 0; index < abilities.Count; index++)
        {
            list.GetArrayElementAtIndex(index).objectReferenceValue = abilities[index];
        }

        if (serialized.ApplyModifiedPropertiesWithoutUndo())
        {
            AssetDatabase.SaveAssetIfDirty(database);
        }

        return database;
    }
}
