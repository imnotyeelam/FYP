using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FW01URPValidationTools
{
    const string Source = "Assets/TriForge Assets/Fantasy Worlds - Forest/Scenes/S_FW01_Demo_Summer.unity";
    const string Root = "Assets/Development/FW01_URP";
    const string Target = Root + "/Scenes/FW01_MainMap_Summer_URP.unity";
    const string AuditFile = Root + "/Reports/FW01_URP_Audit.txt";
    const string SummaryFile = Root + "/Reports/FW01_URP_Summary.txt";
    static readonly Dictionary<Material, Material> migrated = new Dictionary<Material, Material>();
    static readonly Dictionary<GameObject, GameObject> prefabCopies = new Dictionary<GameObject, GameObject>();
    static readonly HashSet<Material> used = new HashSet<Material>();
    static readonly HashSet<GameObject> prefabs = new HashSet<GameObject>();
    static readonly List<string> warnings = new List<string>();
    static int rendererCount, terrainCount, treeCount, detailCount;

    [MenuItem("Tools/FYP/FW01 URP/Build Summer Development Copy")]
    public static void Build()
    {
        EnsureFolders();
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Source) == null)
            throw new Exception("Summer source scene missing: " + Source);
        if (Shader.Find("Universal Render Pipeline/Lit") == null)
            throw new Exception("URP/Lit shader not available. No source assets changed.");
        migrated.Clear(); prefabCopies.Clear(); used.Clear(); prefabs.Clear(); warnings.Clear();
        var scene = EditorSceneManager.OpenScene(Source, OpenSceneMode.Single);
        Collect(scene);
        WriteAudit();
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Target) != null)
            throw new Exception("Development scene already exists; refusing to overwrite: " + Target);
        if (!AssetDatabase.CopyAsset(Source, Target))
            throw new Exception("Could not copy Summer scene to development folder.");
        AssetDatabase.Refresh();
        scene = EditorSceneManager.OpenScene(Target, OpenSceneMode.Single);
        // Renderers in loaded scene include nested prefab instances. Their material assignments
        // become scene-level overrides; source prefab and material files remain untouched.
        foreach (var renderer in All<Renderer>(scene))
        {
            var sourceMaterials = renderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < sourceMaterials.Length; i++)
            {
                var replacement = Recover(sourceMaterials[i]);
                if (replacement != sourceMaterials[i]) { sourceMaterials[i] = replacement; changed = true; }
            }
            if (changed)
            {
                renderer.sharedMaterials = sourceMaterials;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                EditorUtility.SetDirty(renderer);
            }
        }
        foreach (var terrain in All<Terrain>(scene))
        {
            if (terrain.materialTemplate != null)
            {
                var replacement = Recover(terrain.materialTemplate);
                if (replacement != terrain.materialTemplate)
                {
                    terrain.materialTemplate = replacement;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(terrain);
                }
            }
            var sourceData = terrain.terrainData;
            if (sourceData == null) { warnings.Add("Terrain without TerrainData: " + PathOf(terrain.transform)); continue; }
            string dataPath = Root + "/TerrainData/" + Safe(sourceData.name) + "_URP.asset";
            var copy = AssetDatabase.LoadAssetAtPath<TerrainData>(dataPath);
            if (copy == null)
            {
                copy = UnityEngine.Object.Instantiate(sourceData);
                copy.name = sourceData.name + "_URP";
                AssetDatabase.CreateAsset(copy, dataPath);
                var trees = copy.treePrototypes;
                for (int i = 0; i < trees.Length; i++)
                    if (trees[i].prefab != null) trees[i].prefab = RecoverPrefab(trees[i].prefab);
                copy.treePrototypes = trees;
                var details = copy.detailPrototypes;
                for (int i = 0; i < details.Length; i++)
                    if (details[i].prototype != null) details[i].prototype = RecoverPrefab(details[i].prototype);
                copy.detailPrototypes = details;
                EditorUtility.SetDirty(copy);
            }
            terrain.terrainData = copy;
            PrefabUtility.RecordPrefabInstancePropertyModifications(terrain);
            var collider = terrain.GetComponent<TerrainCollider>();
            if (collider != null) { collider.terrainData = copy; PrefabUtility.RecordPrefabInstancePropertyModifications(collider); }
        }
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Could not save development scene.");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidateInternal(false);
        WriteSummary();
        Debug.Log("FW01 URP development copy created: " + Target + "; materials=" + migrated.Count + "; prefabs=" + prefabCopies.Count);
    }

    [MenuItem("Tools/FYP/FW01 URP/Validate Summer Scene References")]
    public static void Validate() { ValidateInternal(true); }

    [MenuItem("Tools/FYP/FW01 URP/Audit Pink Risk In Summer Scene")]
    public static void AuditPink() { ValidateInternal(true); }

    static void ValidateInternal(bool writeReport)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Target) == null) { Debug.LogError("Missing development scene: " + Target); return; }
        var scene = EditorSceneManager.OpenScene(Target, OpenSceneMode.Single);
        var risk = new List<string>();
        int slots = 0, errors = 0, terrainPrototypes = 0;
        foreach (var renderer in All<Renderer>(scene))
        {
            var mats = renderer.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                slots++;
                if (Risk(mats[i])) risk.Add("Renderer " + PathOf(renderer.transform) + " slot " + i + " | " + AssetDatabase.GetAssetPath(mats[i]) + " | " + ShaderName(mats[i]));
                if (mats[i] == null) errors++;
            }
        }
        foreach (var terrain in All<Terrain>(scene))
        {
            if (Risk(terrain.materialTemplate)) risk.Add("Terrain " + PathOf(terrain.transform) + " material | " + AssetDatabase.GetAssetPath(terrain.materialTemplate) + " | " + ShaderName(terrain.materialTemplate));
            var data = terrain.terrainData;
            if (data == null) { errors++; risk.Add("Terrain " + PathOf(terrain.transform) + " has no TerrainData"); continue; }
            if (!AssetDatabase.GetAssetPath(data).StartsWith(Root + "/")) risk.Add("Terrain " + PathOf(terrain.transform) + " references source TerrainData: " + AssetDatabase.GetAssetPath(data));
            foreach (var p in data.treePrototypes) { terrainPrototypes++; CheckPrefab(p.prefab, "Tree prototype", risk, ref errors); }
            foreach (var p in data.detailPrototypes) { terrainPrototypes++; if (p.prototype != null) CheckPrefab(p.prototype, "Detail prototype", risk, ref errors); }
        }
        int materialCount = AssetDatabase.FindAssets("t:Material", new[] { Root + "/Materials" }).Length;
        int prefabCount = AssetDatabase.FindAssets("t:Prefab", new[] { Root + "/Prefabs" }).Length;
        var report = new StringBuilder();
        report.AppendLine("FW01 Summer URP scene validation");
        report.AppendLine("Scene: " + Target);
        report.AppendLine("Migrated materials: " + materialCount);
        report.AppendLine("Migrated prefabs: " + prefabCount);
        report.AppendLine("Renderer material slots: " + slots);
        report.AppendLine("Terrain prototypes: " + terrainPrototypes);
        report.AppendLine("Remaining risky references: " + risk.Count);
        report.AppendLine("Errors: " + errors);
        report.AppendLine("Warnings: " + risk.Count);
        foreach (var line in risk) report.AppendLine(line);
        if (writeReport) File.WriteAllText(Root + "/Reports/FW01_URP_Validation.txt", report.ToString());
        Debug.Log(report.ToString());
    }

    static void CheckPrefab(GameObject prefab, string label, List<string> risk, ref int errors)
    {
        if (prefab == null) { errors++; risk.Add(label + " missing prefab"); return; }
        foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
        {
            var mats = renderer.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
                if (Risk(mats[i])) risk.Add(label + " " + AssetDatabase.GetAssetPath(prefab) + " | " + PathOf(renderer.transform) + " slot " + i + " | " + AssetDatabase.GetAssetPath(mats[i]) + " | " + ShaderName(mats[i]));
        }
    }

    static void Collect(Scene scene)
    {
        rendererCount = terrainCount = treeCount = detailCount = 0;
        foreach (var renderer in All<Renderer>(scene)) { rendererCount++; foreach (var m in renderer.sharedMaterials) if (m != null) used.Add(m); }
        foreach (var terrain in All<Terrain>(scene))
        {
            terrainCount++;
            if (terrain.materialTemplate != null) used.Add(terrain.materialTemplate);
            if (terrain.terrainData == null) continue;
            foreach (var p in terrain.terrainData.treePrototypes) { treeCount++; CollectPrefab(p.prefab); }
            foreach (var p in terrain.terrainData.detailPrototypes) { detailCount++; CollectPrefab(p.prototype); }
        }
        foreach (var dependency in AssetDatabase.GetDependencies(Source, true))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(dependency);
            if (prefab != null && dependency.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)) prefabs.Add(prefab);
        }
    }

    static void CollectPrefab(GameObject prefab)
    {
        if (prefab == null) return;
        prefabs.Add(prefab);
        foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            foreach (var m in renderer.sharedMaterials) if (m != null) used.Add(m);
    }

    static void WriteAudit()
    {
        var families = used.GroupBy(m => Family(ShaderName(m))).OrderByDescending(g => g.Count());
        var report = new StringBuilder();
        report.AppendLine("FW01 Summer URP audit (source read only)");
        report.AppendLine("Scene: " + Source);
        report.AppendLine("Direct/indirect used materials (scene renderers + Terrain prototypes): " + used.Count);
        report.AppendLine("Scene renderers: " + rendererCount + "; terrains: " + terrainCount + "; tree prototypes: " + treeCount + "; detail prototypes: " + detailCount);
        report.AppendLine("Prefab dependencies: " + prefabs.Count);
        report.AppendLine("Already URP: " + used.Count(m => IsUrp(m)));
        report.AppendLine("Pink high risk: " + used.Count(Risk));
        report.AppendLine("Shader families:");
        foreach (var f in families) report.AppendLine("  " + f.Key + ": " + f.Count());
        report.AppendLine("Likely priority: tree bark, leaves, flowers, grass, bushes, rocks, ruins, ground, Terrain trees/details.");
        report.AppendLine("High-risk materials:");
        foreach (var m in used.Where(Risk).OrderBy(m => AssetDatabase.GetAssetPath(m)))
            report.AppendLine("  " + AssetDatabase.GetAssetPath(m) + " | " + ShaderName(m) + " | " + Category(m.name));
        File.WriteAllText(AuditFile, report.ToString());
        Debug.Log("FW01 source audit: materials=" + used.Count + ", pink risk=" + used.Count(Risk) + ", report=" + AuditFile);
    }

    static void WriteSummary()
    {
        var report = new StringBuilder();
        report.AppendLine("FW01 Summer URP development recovery");
        report.AppendLine("Development scene: " + Target);
        report.AppendLine("Migrated materials: " + migrated.Count);
        report.AppendLine("Migrated prefabs: " + prefabCopies.Count);
        report.AppendLine("Recovered shader families: " + string.Join(", ", migrated.Keys.Select(m => Family(ShaderName(m))).Distinct().OrderBy(x => x)));
        report.AppendLine("Likely visible: terrain, roads, ruins, rocks, bark, leaves, flowers, grass, bushes whose source maps were available.");
        report.AppendLine("Manual checks: vegetation alpha silhouettes, Terrain prototype density, transparent/VFX objects, skybox, lighting and baked data.");
        report.AppendLine("Known limits: custom wind/fancy shader effects are not preserved; original baked lighting and post-processing are retained as reference, not rebuilt.");
        foreach (var warning in warnings) report.AppendLine("Warning: " + warning);
        File.WriteAllText(SummaryFile, report.ToString());
    }

    static Material Recover(Material source)
    {
        if (source == null || !Risk(source)) return source;
        if (migrated.TryGetValue(source, out var existing)) return existing;
        string path = Root + "/Materials/" + Safe(source.name) + "_" + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source)).Substring(0, 8) + "_URP.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.name = source.name + "_URP";
            var tex = GetTexture(source, "_MainTex", "_BaseMap", "_Albedo", "_AlbedoMap", "_Diffuse", "_DiffuseMap", "_ColorMap");
            if (tex != null) { mat.SetTexture("_BaseMap", tex); mat.SetTextureScale("_BaseMap", GetScale(source, tex)); mat.SetTextureOffset("_BaseMap", GetOffset(source, tex)); }
            mat.SetColor("_BaseColor", GetColor(source, Color.white, "_Color", "_BaseColor", "_TintColor"));
            var normal = GetTexture(source, "_BumpMap", "_NormalMap", "_NormalTex");
            if (normal != null) { mat.SetTexture("_BumpMap", normal); mat.EnableKeyword("_NORMALMAP"); }
            var mask = GetTexture(source, "_MetallicGlossMap", "_MaskMap");
            if (mask != null) { mat.SetTexture("_MetallicGlossMap", mask); mat.EnableKeyword("_METALLICSPECGLOSSMAP"); }
            mat.SetFloat("_Metallic", GetFloat(source, 0, "_Metallic"));
            mat.SetFloat("_Smoothness", GetFloat(source, 0.35f, "_Smoothness", "_Glossiness"));
            var emission = GetTexture(source, "_EmissionMap");
            var emissionColor = GetColor(source, Color.black, "_EmissionColor");
            if (emission != null || emissionColor.maxColorComponent > 0.01f)
            { if (emission != null) mat.SetTexture("_EmissionMap", emission); mat.SetColor("_EmissionColor", emissionColor); mat.EnableKeyword("_EMISSION"); }
            if (Category(source.name) == "vegetation")
            {
                mat.SetFloat("_AlphaClip", 1);
                mat.SetFloat("_Cutoff", Mathf.Clamp(GetFloat(source, 0.4f, "_Cutoff", "_AlphaCutoff", "_Threshold"), 0.05f, 0.95f));
                mat.SetFloat("_Cull", 0);
                mat.EnableKeyword("_ALPHATEST_ON");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            }
            AssetDatabase.CreateAsset(mat, path);
        }
        migrated[source] = mat;
        return mat;
    }

    static GameObject RecoverPrefab(GameObject source)
    {
        if (source == null) return null;
        if (prefabCopies.TryGetValue(source, out var copy)) return copy;
        bool hasRisk = source.GetComponentsInChildren<Renderer>(true).Any(r => r.sharedMaterials.Any(Risk));
        if (!hasRisk) return source;
        string path = Root + "/Prefabs/" + Safe(source.name) + "_" + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source)).Substring(0, 8) + "_URP.prefab";
        copy = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (copy == null)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            try
            {
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                {
                    var mats = renderer.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++) mats[i] = Recover(mats[i]);
                    renderer.sharedMaterials = mats;
                }
                copy = PrefabUtility.SaveAsPrefabAsset(instance, path);
                if (copy == null) throw new Exception("Could not save prefab variant " + path);
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }
        prefabCopies[source] = copy;
        return copy;
    }

    static bool Risk(Material m) => m == null || m.shader == null || m.shader.name == "Hidden/InternalErrorShader" || (!IsUrp(m) && !m.shader.name.StartsWith("Skybox/"));
    static bool IsUrp(Material m) => m != null && m.shader != null && m.shader.name.StartsWith("Universal Render Pipeline/");
    static string ShaderName(Material m) => m == null ? "<missing material>" : m.shader == null ? "<missing shader>" : m.shader.name;
    static string Family(string shader) { int i = shader.IndexOf('/'); return i < 0 ? shader : shader.Substring(0, i); }
    static string Category(string name)
    {
        string n = name.ToLowerInvariant();
        return new[] { "leaf", "leaves", "flower", "grass", "bush", "plant", "fern", "weed", "foliage", "shrub", "sapling", "ivy", "moss" }.Any(n.Contains) ? "vegetation" : "hard surface";
    }
    static string Safe(string s) => string.Concat(s.Select(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_'));
    static string PathOf(Transform t) { var names = new List<string>(); for (; t != null; t = t.parent) names.Add(t.name); names.Reverse(); return string.Join("/", names); }
    static IEnumerable<T> All<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true));
    static void EnsureFolders()
    {
        string[] folders = { Root, Root + "/Scenes", Root + "/Materials", Root + "/Prefabs", Root + "/Reports", Root + "/Editor", Root + "/TerrainData" };
        foreach (var folder in folders) if (!AssetDatabase.IsValidFolder(folder)) { var parent = folder.Substring(0, folder.LastIndexOf('/')); AssetDatabase.CreateFolder(parent, folder.Substring(folder.LastIndexOf('/') + 1)); }
    }
    static Texture GetTexture(Material m, params string[] names)
    {
        foreach (var name in names) { if (m.HasProperty(name) && m.GetTexture(name) != null) return m.GetTexture(name); var p = Saved(m, "m_TexEnvs", name); if (p != null) { var value = p.FindPropertyRelative("second.m_Texture"); if (value != null && value.objectReferenceValue is Texture t) return t; } }
        return null;
    }
    static Vector2 GetScale(Material m, Texture tex) { foreach (var n in new[] { "_MainTex", "_BaseMap" }) if (m.HasProperty(n) && m.GetTexture(n) == tex) return m.GetTextureScale(n); return Vector2.one; }
    static Vector2 GetOffset(Material m, Texture tex) { foreach (var n in new[] { "_MainTex", "_BaseMap" }) if (m.HasProperty(n) && m.GetTexture(n) == tex) return m.GetTextureOffset(n); return Vector2.zero; }
    static Color GetColor(Material m, Color fallback, params string[] names) { foreach (var n in names) { if (m.HasProperty(n)) return m.GetColor(n); var p = Saved(m, "m_Colors", n); if (p != null) return p.FindPropertyRelative("second").colorValue; } return fallback; }
    static float GetFloat(Material m, float fallback, params string[] names) { foreach (var n in names) { if (m.HasProperty(n)) return m.GetFloat(n); var p = Saved(m, "m_Floats", n); if (p != null) return p.FindPropertyRelative("second").floatValue; } return fallback; }
    static SerializedProperty Saved(Material m, string collection, string name)
    {
        var so = new SerializedObject(m); var a = so.FindProperty("m_SavedProperties." + collection);
        if (a == null) return null;
        for (int i = 0; i < a.arraySize; i++) { var p = a.GetArrayElementAtIndex(i); if (p.FindPropertyRelative("first").stringValue == name) return p; }
        return null;
    }
}
