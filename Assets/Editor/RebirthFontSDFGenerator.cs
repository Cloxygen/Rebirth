using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.TextCore.Text;

public static class RebirthFontSDFGenerator
{
    const int AtlasSize = 2048;

    static string Glyphs =>
        new string(Enumerable.Range(32, 95).Select(c => (char)c).ToArray()) + "·×–—‘’“”…";

    [MenuItem("Rebirth/Generate HUD Font SDFs")]
    public static void Generate()
    {
        foreach (string src in FindSourceFonts()) Bake(src, ChooseAssetPath(src));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static IEnumerable<string> FindSourceFonts() =>
        Directory.GetFiles("Assets/Fonts")
            .Where(f => f.EndsWith(".ttf") || f.EndsWith(".otf"))
            .Select(f => f.Replace('\\', '/'));

    static string ChooseAssetPath(string src)
    {
        string stem = Path.GetFileNameWithoutExtension(src).Replace("-Regular", "").Replace("-regular", "");
        if (stem == "texgyrepagella") stem = "TeX Gyre Pagella";
        if (stem == "selawik") stem = "Selawik";
        return $"Assets/Fonts/{stem} SDF.asset";
    }

    static void Bake(string src, string dst)
    {
        FontAsset fa = AssetDatabase.LoadAssetAtPath<FontAsset>(dst);
        if (fa == null) fa = CreateSDF(src, dst);
        if (fa == null) return;
        string missing = FillAtlas(fa);
        Seal(fa);
        Debug.Log($"[RebirthFonts] {dst}: {fa.glyphTable.Count} glyphs" + (missing.Length > 0 ? $", source lacks \"{missing}\"" : ""));
    }

    static string FillAtlas(FontAsset fa)
    {
        EmptyAtlas(fa);
        fa.TryAddCharacters(Glyphs, out string missing);
        return missing ?? "";
    }

    static void EmptyAtlas(FontAsset fa)
    {
        fa.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        fa.isMultiAtlasTexturesEnabled = false;
        SetAtlasSize(fa, AtlasSize);
        fa.ClearFontAssetData(true);
    }

    static void SetAtlasSize(FontAsset fa, int size)
    {
        SerializedObject so = new SerializedObject(fa);
        so.FindProperty("m_AtlasWidth").intValue = size;
        so.FindProperty("m_AtlasHeight").intValue = size;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void Seal(FontAsset fa)
    {
        fa.atlasPopulationMode = AtlasPopulationMode.Static;
        EditorUtility.SetDirty(fa);
        foreach (Texture2D atlas in fa.atlasTextures) EditorUtility.SetDirty(atlas);
    }

    static FontAsset CreateSDF(string src, string dst)
    {
        Font font = AssetDatabase.LoadAssetAtPath<Font>(src);
        if (font == null) { Debug.LogError($"[RebirthFonts] source font missing: {src}"); return null; }

        FontAsset fa = FontAsset.CreateFontAsset(
            font, 90, 9, GlyphRenderMode.SDFAA, AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic, true);
        if (fa == null) { Debug.LogError($"[RebirthFonts] CreateFontAsset failed: {src}"); return null; }

        fa.name = Path.GetFileNameWithoutExtension(dst);
        AssetDatabase.CreateAsset(fa, dst);
        EmbedParts(fa);
        LogUrl(dst, fa.name);
        return fa;
    }

    static void EmbedParts(FontAsset fa)
    {
        if (fa.atlasTextures != null && fa.atlasTextures.Length > 0 && fa.atlasTextures[0] != null)
        {
            fa.atlasTextures[0].name = fa.name + " Atlas";
            AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
        }
        if (fa.material == null) return;
        fa.material.name = fa.name + " Material";
        AssetDatabase.AddObjectToAsset(fa.material, fa);
    }

    static void LogUrl(string dst, string name)
    {
        string guid = AssetDatabase.AssetPathToGUID(dst);
        string enc = dst.Replace(" ", "%20");
        string url = $"project://database/{enc}?fileID=11400000&guid={guid}&type=2#{name.Replace(" ", "%20")}";
        Debug.Log($"[RebirthFonts] created \"{dst}\"\n  guid = {guid}\n  USS url = {url}");
    }
}
