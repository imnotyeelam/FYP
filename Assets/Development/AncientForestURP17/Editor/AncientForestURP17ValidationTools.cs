// Development-owned validation and test-scene tool. It never writes into the source package.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AncientForestURP17ValidationTools
{
    private const string Root = "Assets/Development/AncientForestURP17";
    private const string Materials = Root + "/Materials";
    private const string Prefabs = Root + "/Prefabs";
    private const string TestScene = Root + "/Scenes/AncientForestURP17_Validation.unity";
    private const string MainMap = Root + "/Scenes/AncientForest_MainMap.unity";
    private const string TerrainData = Root + "/TerrainData";
    private const string OriginalPackage = "Assets/Fantastic Ancient Forest";
    private const string PropHeadPrefab = Prefabs + "/Props/P_PROP_head_ancient.prefab";
    private const string SourcePropHeadMaterial = OriginalPackage + "/Materials/M_PROP_head_ancient.mat";
    private const string CompatiblePropHeadMaterial = Materials + "/M_PROP_head_ancient_URP17.mat";

    [MenuItem("Tools/FYP/Ancient Forest URP17/Validate Migrated Assets")]
    public static void ValidateMigratedAssets()
    {
        var errors = 0;
        var warnings = 0;
        var materialGuids = AssetDatabase.FindAssets("t:Material", new[] { Materials });
        var migratedMaterialPaths = new HashSet<string>(StringComparer.Ordinal);

        foreach (var guid in materialGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            // These assets are generated outside the Editor. Ensure validation reads
            // their current YAML rather than an older importer-cache representation.
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            migratedMaterialPaths.Add(path);
            if (!material || !material.shader)
            {
                Debug.LogError("[AncientForestURP17] Missing material or shader: " + path);
                errors++;
                continue;
            }

            if (!material.shader.name.StartsWith("Development/AncientForest", StringComparison.Ordinal))
            {
                Debug.LogError("[AncientForestURP17] Material does not use a development-compatible shader: " + path);
                errors++;
            }
            else if (!material.shader.isSupported)
            {
                Debug.LogWarning("[AncientForestURP17] Shader reports unsupported on this platform: " + path);
                warnings++;
            }
        }

        var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { Prefabs });
        foreach (var guid in prefabGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    var rendererMaterials = renderer.sharedMaterials;
                    for (var slot = 0; slot < rendererMaterials.Length; slot++)
                    {
                        var material = rendererMaterials[slot];
                        if (!material)
                        {
                            Debug.LogError($"[AncientForestURP17] Missing renderer material: {path}; Renderer={HierarchyPath(renderer.transform)}; Slot={slot}");
                            errors++;
                            continue;
                        }

                        var materialPath = AssetDatabase.GetAssetPath(material);
                        if (materialPath.StartsWith(OriginalPackage, StringComparison.Ordinal))
                        {
                            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(material, out var materialGuid, out long _);
                            Debug.LogError($"[AncientForestURP17] Prefab still references a source-package material: {path} -> {materialPath}; Renderer={HierarchyPath(renderer.transform)}; Slot={slot}; GUID={materialGuid}");
                            errors++;
                        }
                        else if (materialPath.StartsWith(Materials, StringComparison.Ordinal) && !migratedMaterialPaths.Contains(materialPath))
                        {
                            Debug.LogError("[AncientForestURP17] Prefab references an untracked migrated material: " + path + " -> " + materialPath);
                            errors++;
                        }
                    }
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        Debug.Log($"[AncientForestURP17] Validation complete. Materials={materialGuids.Length}, Prefabs={prefabGuids.Length}, Errors={errors}, Warnings={warnings}. Visual pink/error-material inspection remains a manual Game View check.");
    }

    [MenuItem("Tools/FYP/Ancient Forest URP17/Audit Prop Head Loaded Renderer Materials")]
    public static void AuditPropHeadLoadedRendererMaterials()
    {
        AssetDatabase.ImportAsset(PropHeadPrefab, ImportAssetOptions.ForceSynchronousImport);
        var root = PrefabUtility.LoadPrefabContents(PropHeadPrefab);
        try
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (var slot = 0; slot < materials.Length; slot++)
                {
                    var material = materials[slot];
                    var path = material ? AssetDatabase.GetAssetPath(material) : "<missing>";
                    var guid = material && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(material, out var value, out long _) ? value : "<missing>";
                    Debug.Log($"[AncientForestURP17] PropHead Renderer={HierarchyPath(renderer.transform)}; Slot={slot}; GUID={guid}; Material={path}");
                }
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    [MenuItem("Tools/FYP/Ancient Forest URP17/Repair Prop Head Material Overrides")]
    public static void RepairPropHeadMaterialOverrides()
    {
        var compatibleMaterial = AssetDatabase.LoadAssetAtPath<Material>(CompatiblePropHeadMaterial);
        if (!compatibleMaterial)
            throw new InvalidOperationException("Compatible Prop Head material is missing: " + CompatiblePropHeadMaterial);

        AssetDatabase.ImportAsset(PropHeadPrefab, ImportAssetOptions.ForceSynchronousImport);
        var root = PrefabUtility.LoadPrefabContents(PropHeadPrefab);
        var replacements = 0;
        try
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                var changed = false;
                for (var slot = 0; slot < materials.Length; slot++)
                {
                    if (materials[slot] && AssetDatabase.GetAssetPath(materials[slot]) == SourcePropHeadMaterial)
                    {
                        materials[slot] = compatibleMaterial;
                        replacements++;
                        changed = true;
                    }
                }

                if (changed)
                    renderer.sharedMaterials = materials;
            }

            PrefabUtility.SaveAsPrefabAsset(root, PropHeadPrefab);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.ImportAsset(PropHeadPrefab, ImportAssetOptions.ForceSynchronousImport);
        Debug.Log($"[AncientForestURP17] Prop Head material override repair completed. Replacements={replacements}.");
    }

    [MenuItem("Tools/FYP/Ancient Forest URP17/Create Validation Scene")]
    public static void CreateValidationScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var prefabPaths = AssetDatabase.FindAssets("t:Prefab", new[] { Prefabs })
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        for (var i = 0; i < prefabPaths.Length; i++)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[i]);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = "Validation_" + prefab.name;
            instance.transform.position = new Vector3((i % 4) * 8f, 0f, (i / 4) * 8f);
        }

        EditorSceneManager.SaveScene(scene, TestScene);
        Debug.Log("[AncientForestURP17] Created isolated validation scene: " + TestScene);
    }

    // This is intentionally opt-in: it only modifies development-owned prefab copies,
    // the development-owned MainMap scene, and development-owned TerrainData copies.
    [MenuItem("Tools/FYP/Ancient Forest URP17/Migrate Main Map Prefab Variants")]
    public static void MigrateMainMapPrefabVariants()
    {
        var materialMap = CompatibleMaterialsBySourcePath();
        var sourcePrefabs = FindLegacyMainMapPrefabPaths(materialMap.Keys);
        var replacements = new Dictionary<string, GameObject>(StringComparer.Ordinal);

        foreach (var sourcePath in sourcePrefabs.OrderBy(path => path, StringComparer.Ordinal))
        {
            var relativePath = sourcePath.Substring((OriginalPackage + "/Prefabs/").Length);
            var compatiblePath = Prefabs + "/" + relativePath;
            EnsureFolder(Path.GetDirectoryName(compatiblePath)?.Replace('\\', '/'));
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(compatiblePath))
            {
                if (!AssetDatabase.CopyAsset(sourcePath, compatiblePath))
                    throw new InvalidOperationException("Could not create development prefab copy: " + sourcePath);
            }

            var root = PrefabUtility.LoadPrefabContents(compatiblePath);
            var materialReplacements = ReplaceRendererMaterials(root, materialMap);
            PrefabUtility.SaveAsPrefabAsset(root, compatiblePath);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log($"[AncientForestURP17] Prefab migrated: {compatiblePath}; material slots replaced={materialReplacements}.");
            replacements.Add(sourcePath, AssetDatabase.LoadAssetAtPath<GameObject>(compatiblePath));
        }

        var scene = EditorSceneManager.OpenScene(MainMap, OpenSceneMode.Additive);
        var sceneReplacements = 0;
        var saved = false;
        try
        {
            // Replacing a prefab instance destroys its root and every child transform.
            // Build a stable list of outermost roots before the first mutation; never
            // continue enumerating a Transform[] after ReplacePrefabAssetOfPrefabInstance.
            var candidates = CollectMainMapReplacementCandidates(scene, replacements);
            Debug.Log($"[AncientForestURP17] Main Map prefab replacement preflight: candidates={candidates.Count}, compatible prefabs={replacements.Count}.");

            foreach (var candidate in candidates)
            {
                if (!candidate.Root)
                {
                    Debug.LogError($"[AncientForestURP17] Skipped destroyed replacement candidate: Source={candidate.SourcePath}.");
                    continue;
                }

                if (!candidate.CompatiblePrefab)
                    throw new InvalidOperationException("Missing compatible prefab during Main Map migration: " + candidate.SourcePath);

                var currentSourcePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(candidate.Root);
                if (!string.Equals(currentSourcePath, candidate.SourcePath, StringComparison.Ordinal))
                {
                    Debug.LogWarning($"[AncientForestURP17] Skipped changed prefab candidate: Expected={candidate.SourcePath}; Actual={currentSourcePath}; Root={candidate.Root.name}.");
                    continue;
                }

                PrefabUtility.ReplacePrefabAssetOfPrefabInstance(candidate.Root, candidate.CompatiblePrefab, InteractionMode.AutomatedAction);
                sceneReplacements++;
            }

            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Unity could not save the development Main Map after prefab replacement.");
            saved = true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[AncientForestURP17] Main Map prefab replacement failed before save. In-memory scene changes will be discarded; no source package asset is modified. {exception}");
            throw;
        }
        finally
        {
            if (scene.isLoaded)
            {
                if (!saved && scene.isDirty)
                    Debug.LogWarning("[AncientForestURP17] Closing unsaved Main Map after a failed migration; partial scene-instance replacements are being discarded.");
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        var terrainPrototypeReplacements = ReplaceTerrainPrototypes(replacements);
        AssetDatabase.SaveAssets();
        Debug.Log($"[AncientForestURP17] Main Map prefab migration complete. Prefabs={replacements.Count}, scene instances={sceneReplacements}, terrain prototypes={terrainPrototypeReplacements}.");
    }

    [MenuItem("Tools/FYP/Ancient Forest URP17/Preflight Main Map Prefab Migration")]
    public static void PreflightMainMapPrefabMigration()
    {
        var materialMap = CompatibleMaterialsBySourcePath();
        var sourcePrefabs = FindLegacyMainMapPrefabPaths(materialMap.Keys);
        var existingVariants = 0;
        var missingVariants = 0;
        foreach (var sourcePath in sourcePrefabs)
        {
            var relativePath = sourcePath.Substring((OriginalPackage + "/Prefabs/").Length);
            var compatiblePath = Prefabs + "/" + relativePath;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(compatiblePath))
                existingVariants++;
            else
                missingVariants++;
        }

        Debug.Log($"[AncientForestURP17] Main Map prefab migration preflight complete. Legacy source prefabs={sourcePrefabs.Count}, existing development variants={existingVariants}, variants to create={missingVariants}, compatible materials={materialMap.Count}. No assets or scenes were modified.");
    }

    [MenuItem("Tools/FYP/Ancient Forest URP17/Audit Main Map Loaded References")]
    public static void AuditMainMapLoadedReferences()
    {
        var legacyMaterials = new HashSet<string>(CompatibleMaterialsBySourcePath().Keys, StringComparer.Ordinal);
        var scene = EditorSceneManager.OpenScene(MainMap, OpenSceneMode.Additive);
        var errors = 0;
        var renderers = 0;
        var terrains = 0;
        try
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    renderers++;
                    for (var slot = 0; slot < renderer.sharedMaterials.Length; slot++)
                    {
                        var material = renderer.sharedMaterials[slot];
                        var materialPath = material ? AssetDatabase.GetAssetPath(material) : string.Empty;
                        if (!legacyMaterials.Contains(materialPath))
                            continue;

                        Debug.LogError($"[AncientForestURP17] Main Map legacy renderer material: {HierarchyPath(renderer.transform)}; Slot={slot}; Material={materialPath}");
                        errors++;
                    }
                }

                foreach (var terrain in root.GetComponentsInChildren<Terrain>(true))
                {
                    terrains++;
                    var data = terrain.terrainData;
                    var dataPath = data ? AssetDatabase.GetAssetPath(data) : string.Empty;
                    if (!dataPath.StartsWith(TerrainData, StringComparison.Ordinal))
                    {
                        Debug.LogError($"[AncientForestURP17] Main Map Terrain does not use a development-owned TerrainData copy: {HierarchyPath(terrain.transform)} -> {dataPath}");
                        errors++;
                        continue;
                    }

                    errors += AuditTerrainPrototypes(terrain, legacyMaterials);
                }
            }
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }

        Debug.Log($"[AncientForestURP17] Main Map loaded-reference audit complete. Renderers={renderers}, Terrains={terrains}, Errors={errors}. This audit does not alter the scene or source package.");
    }

    private static Dictionary<string, Material> CompatibleMaterialsBySourcePath()
    {
        var map = new Dictionary<string, Material>(StringComparer.Ordinal);
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { Materials }))
        {
            var compatiblePath = AssetDatabase.GUIDToAssetPath(guid);
            var compatible = AssetDatabase.LoadAssetAtPath<Material>(compatiblePath);
            var fileName = Path.GetFileNameWithoutExtension(compatiblePath);
            const string suffix = "_URP17";
            if (!compatible || !fileName.EndsWith(suffix, StringComparison.Ordinal))
                continue;

            var sourcePath = OriginalPackage + "/Materials/" + fileName.Substring(0, fileName.Length - suffix.Length) + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(sourcePath))
                map[sourcePath] = compatible;
        }

        return map;
    }

    private static int ReplaceRendererMaterials(GameObject root, IReadOnlyDictionary<string, Material> materialMap)
    {
        var replacements = 0;
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            var materials = renderer.sharedMaterials;
            var changed = false;
            for (var slot = 0; slot < materials.Length; slot++)
            {
                var sourcePath = materials[slot] ? AssetDatabase.GetAssetPath(materials[slot]) : string.Empty;
                if (!materialMap.TryGetValue(sourcePath, out var compatible))
                    continue;

                materials[slot] = compatible;
                changed = true;
                replacements++;
            }

            if (changed)
                renderer.sharedMaterials = materials;
        }

        return replacements;
    }

    private static int ReplaceTerrainPrototypes(IReadOnlyDictionary<string, GameObject> prefabReplacements)
    {
        var replacements = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:TerrainData", new[] { TerrainData }))
        {
            var terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(AssetDatabase.GUIDToAssetPath(guid));
            var changed = false;
            var trees = terrainData.treePrototypes;
            for (var i = 0; i < trees.Length; i++)
            {
                var sourcePath = trees[i].prefab ? AssetDatabase.GetAssetPath(trees[i].prefab) : string.Empty;
                if (!prefabReplacements.TryGetValue(sourcePath, out var compatiblePrefab))
                    continue;

                trees[i].prefab = compatiblePrefab;
                replacements++;
                changed = true;
            }

            var details = terrainData.detailPrototypes;
            for (var i = 0; i < details.Length; i++)
            {
                var sourcePath = details[i].prototype ? AssetDatabase.GetAssetPath(details[i].prototype) : string.Empty;
                if (!prefabReplacements.TryGetValue(sourcePath, out var compatiblePrefab))
                    continue;

                details[i].prototype = compatiblePrefab;
                replacements++;
                changed = true;
            }

            if (!changed)
                continue;

            terrainData.treePrototypes = trees;
            terrainData.detailPrototypes = details;
            EditorUtility.SetDirty(terrainData);
        }

        return replacements;
    }

    private sealed class MainMapReplacementCandidate
    {
        public GameObject Root { get; }
        public string SourcePath { get; }
        public GameObject CompatiblePrefab { get; }

        public MainMapReplacementCandidate(GameObject root, string sourcePath, GameObject compatiblePrefab)
        {
            Root = root;
            SourcePath = sourcePath;
            CompatiblePrefab = compatiblePrefab;
        }
    }

    private static List<MainMapReplacementCandidate> CollectMainMapReplacementCandidates(Scene scene, IReadOnlyDictionary<string, GameObject> replacements)
    {
        var candidates = new List<MainMapReplacementCandidate>();
        var seenRootIds = new HashSet<int>();
        foreach (var sceneRoot in scene.GetRootGameObjects())
        {
            foreach (var transform in sceneRoot.GetComponentsInChildren<Transform>(true))
            {
                if (!transform)
                    continue;

                var prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(transform.gameObject);
                if (!prefabRoot || !seenRootIds.Add(prefabRoot.GetInstanceID()))
                    continue;

                var sourcePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefabRoot);
                if (replacements.TryGetValue(sourcePath, out var compatiblePrefab))
                    candidates.Add(new MainMapReplacementCandidate(prefabRoot, sourcePath, compatiblePrefab));
            }
        }

        return candidates;
    }

    private static HashSet<string> FindLegacyMainMapPrefabPaths(ICollection<string> legacyMaterialPaths)
    {
        var legacy = new HashSet<string>(legacyMaterialPaths, StringComparer.Ordinal);
        var paths = new HashSet<string>(StringComparer.Ordinal);
        var scene = EditorSceneManager.OpenScene(MainMap, OpenSceneMode.Additive);
        try
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    if (PrefabUtility.GetOutermostPrefabInstanceRoot(transform.gameObject) != transform.gameObject)
                        continue;

                    AddIfLegacyPrefab(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(transform.gameObject), legacy, paths);
                }

                foreach (var terrain in root.GetComponentsInChildren<Terrain>(true))
                {
                    foreach (var prototype in terrain.terrainData.treePrototypes)
                        AddIfLegacyPrefab(prototype.prefab ? AssetDatabase.GetAssetPath(prototype.prefab) : string.Empty, legacy, paths);
                    foreach (var prototype in terrain.terrainData.detailPrototypes)
                        AddIfLegacyPrefab(prototype.prototype ? AssetDatabase.GetAssetPath(prototype.prototype) : string.Empty, legacy, paths);
                }
            }
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }

        return paths;
    }

    private static void AddIfLegacyPrefab(string prefabPath, HashSet<string> legacyMaterials, HashSet<string> output)
    {
        if (string.IsNullOrEmpty(prefabPath) || !prefabPath.StartsWith(OriginalPackage + "/Prefabs/", StringComparison.Ordinal))
            return;

        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            if (root.GetComponentsInChildren<Renderer>(true)
                .SelectMany(renderer => renderer.sharedMaterials)
                .Any(material => material && legacyMaterials.Contains(AssetDatabase.GetAssetPath(material))))
                output.Add(prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static int AuditTerrainPrototypes(Terrain terrain, HashSet<string> legacyMaterials)
    {
        var errors = 0;
        var terrainData = terrain.terrainData;
        foreach (var prototype in terrainData.treePrototypes)
        {
            if (prototype.prefab)
                errors += AuditPrototypeRendererMaterials(terrain, "Tree", prototype.prefab, legacyMaterials);
        }

        foreach (var prototype in terrainData.detailPrototypes)
        {
            if (prototype.prototype)
                errors += AuditPrototypeRendererMaterials(terrain, "Detail", prototype.prototype, legacyMaterials);
        }

        return errors;
    }

    private static int AuditPrototypeRendererMaterials(Terrain terrain, string prototypeType, GameObject prefab, HashSet<string> legacyMaterials)
    {
        var errors = 0;
        var prefabPath = AssetDatabase.GetAssetPath(prefab);
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    var materialPath = material ? AssetDatabase.GetAssetPath(material) : string.Empty;
                    if (!legacyMaterials.Contains(materialPath))
                        continue;

                    Debug.LogError($"[AncientForestURP17] Main Map Terrain {prototypeType} prototype uses a legacy material: Terrain={HierarchyPath(terrain.transform)}; Prefab={prefabPath}; Renderer={HierarchyPath(renderer.transform)}; Material={materialPath}");
                    errors++;
                }
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return errors;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        var parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
        var name = Path.GetFileName(folder);
        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
            throw new InvalidOperationException("Invalid asset folder: " + folder);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static string HierarchyPath(Transform transform)
    {
        var path = transform.name;
        while (transform.parent)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }

        return path;
    }
}
