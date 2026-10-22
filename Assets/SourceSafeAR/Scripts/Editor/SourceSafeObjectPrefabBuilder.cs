using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor-hulpmiddel dat plaatsbare SourceSafe-prefabs genereert uit de
/// geïmporteerde 3D-modellen (.obj).
///
/// Werkwijze per model:
///   1. het interactie-sjabloon (de starter Cube-prefab) wordt geïnstantieerd
///      en losgemaakt tot een zelfstandig prefab;
///   2. de mesh + het materiaal van het model vervangen die van de cube op de
///      kinderen 'Visuals' (weergave) en 'Cube_COL' (collider);
///   3. het object wordt geschaald naar een realistische grootte;
///   4. het resultaat wordt opgeslagen als prefab.
///
/// De gegenereerde prefabs behouden zo automatisch alle XRI-interactie
/// (selecteren, verplaatsen, schalen, verwijderen) van het template, precies
/// zoals de standaard-primitieven in de balk.
///
/// Gebruik: menu 'Tools > SourceSafeAR > Build Object Prefabs from Models'.
/// Het script staat in een Editor-map en wordt dus niet meegebouwd in de app.
/// </summary>
public static class SourceSafeObjectPrefabBuilder
{
    // Interactie-sjabloon uit de XRI AR Starter Assets. Bevat op de root
    // XRGrabInteractable + ARTransformer + Rigidbody, met kinderen Visuals en Cube_COL.
    const string BasePrefabPath =
        "Assets/Samples/XR Interaction Toolkit/3.3.0/AR Starter Assets/ARDemoSceneAssets/Prefabs/Cube.prefab";

    const string ModelsFolder = "Assets/SourceSafeAR/Models";
    const string OutputFolder = "Assets/SourceSafeAR/Prefabs/SourceSafeObjects";

    // Doelgrootte = grootste afmeting van het object in meters (1 Unity-unit = 1 m).
    // Gebaseerd op realistische maten; pas gerust aan als iets te groot/klein oogt.
    static readonly Dictionary<string, float> ModelTargetSizes = new Dictionary<string, float>
    {
        { "vial",       0.15f },
        { "loodpot",    0.30f },
        { "doos_dicht", 0.40f },
        { "doos_open",  0.40f },
        { "kist_dicht", 0.60f },
        { "kist_open",  0.60f },
        { "drum",       0.85f },
        { "vat",        0.90f },
    };

    [MenuItem("Tools/SourceSafeAR/Build Object Prefabs from Models")]
    public static void BuildPrefabs()
    {
        var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefabPath);
        if (basePrefab == null)
        {
            Debug.LogError($"[PrefabBuilder] Basis-sjabloon niet gevonden op:\n{BasePrefabPath}");
            return;
        }

        if (!AssetDatabase.IsValidFolder(OutputFolder))
        {
            Debug.LogError($"[PrefabBuilder] Doelmap ontbreekt: {OutputFolder}");
            return;
        }

        int built = 0;
        foreach (var entry in ModelTargetSizes)
        {
            string modelName = entry.Key;
            float targetSize = entry.Value;
            string modelPath = $"{ModelsFolder}/{modelName}.obj";

            var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (modelPrefab == null)
            {
                Debug.LogWarning($"[PrefabBuilder] Model niet gevonden (overgeslagen): {modelPath}");
                continue;
            }

            // Mesh + materiaal rechtstreeks uit het geïmporteerde model halen.
            var meshFilters = modelPrefab.GetComponentsInChildren<MeshFilter>();
            if (meshFilters.Length == 0 || meshFilters[0].sharedMesh == null)
            {
                Debug.LogWarning($"[PrefabBuilder] Geen mesh in {modelName} (overgeslagen).");
                continue;
            }
            if (meshFilters.Length > 1)
            {
                Debug.LogWarning($"[PrefabBuilder] '{modelName}' bestaat uit {meshFilters.Length} " +
                                 "delen; alleen het eerste deel wordt gebruikt. Controleer dit prefab handmatig.");
            }

            Mesh mesh = meshFilters[0].sharedMesh;
            var srcRenderer = modelPrefab.GetComponentInChildren<MeshRenderer>();
            Material material = srcRenderer != null ? srcRenderer.sharedMaterial : null;

            // Sjabloon instantiëren en losmaken tot een op zichzelf staand prefab.
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            PrefabUtility.UnpackPrefabInstance(
                instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instance.name = modelName;

            ApplyModel(instance, mesh, material, targetSize);

            string outputPath = $"{OutputFolder}/{modelName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(instance, outputPath);
            Object.DestroyImmediate(instance);

            built++;
            Debug.Log($"[PrefabBuilder] Prefab gemaakt: {outputPath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[PrefabBuilder] Klaar: {built} prefab(s) in {OutputFolder}. " +
                  "Sleep ze in de 'Object Prefabs'-lijst van het Object Spawner-object.");
    }

    /// <summary>
    /// Vervangt mesh + materiaal op de kinderen 'Visuals' en 'Cube_COL' en
    /// schaalt beide zodat de grootste afmeting gelijk is aan <paramref name="targetSize"/>.
    /// </summary>
    static void ApplyModel(GameObject root, Mesh mesh, Material material, float targetSize)
    {
        // Schaal berekenen op basis van de mesh-grootte (in lokale eenheden).
        Vector3 boundsSize = mesh.bounds.size;
        float maxDimension = Mathf.Max(boundsSize.x, boundsSize.y, boundsSize.z);
        float scale = maxDimension > 0f ? targetSize / maxDimension : 1f;

        // 'Visuals' = wat je ziet.
        var visuals = root.transform.Find("Visuals");
        if (visuals != null)
        {
            if (visuals.TryGetComponent<MeshFilter>(out var visualFilter))
                visualFilter.sharedMesh = mesh;
            if (material != null && visuals.TryGetComponent<MeshRenderer>(out var visualRenderer))
                visualRenderer.sharedMaterial = material;
            visuals.localScale = Vector3.one * scale;
        }
        else
        {
            Debug.LogWarning("[PrefabBuilder] Kind 'Visuals' niet gevonden in sjabloon.");
        }

        // 'Cube_COL' = de collider die selectie/aanraken mogelijk maakt.
        var collider = root.transform.Find("Cube_COL");
        if (collider != null)
        {
            if (collider.TryGetComponent<MeshCollider>(out var meshCollider))
                meshCollider.sharedMesh = mesh;
            collider.localScale = Vector3.one * scale;
        }
        else
        {
            Debug.LogWarning("[PrefabBuilder] Kind 'Cube_COL' niet gevonden in sjabloon.");
        }
    }
}
