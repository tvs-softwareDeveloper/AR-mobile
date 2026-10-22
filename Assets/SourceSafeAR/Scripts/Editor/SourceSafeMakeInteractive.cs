using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

/// <summary>
/// Editor-hulpmiddel dat bestaande model-prefabs "plaatsbaar én bedienbaar"
/// maakt: het voegt dezelfde interactie-onderdelen toe die de standaard
/// template-objecten hebben, zodat je jouw eigen 3D-modellen kunt aanklikken,
/// verplaatsen en schalen — net als de Unity-primitieven.
///
/// Wat er per prefab wordt toegevoegd:
///   1. een kinematische Rigidbody (geen zwaartekracht; XR verplaatst het object);
///   2. een BoxCollider passend om het héle model (nodig om te kunnen klikken —
///      zonder collider gaat een tik dwars door het object heen naar de vloer,
///      waardoor er per ongeluk een nieuw object geplaatst wordt);
///   3. een XRGrabInteractable (selecteren + verplaatsen);
///   4. een ARTransformer (verplaatsen over vlakken + schalen met twee vingers).
///
/// De BoxCollider wordt berekend uit de gecombineerde bounds van alle mesh-delen,
/// dus meerdelige modellen (bijv. een open kist) werken ook.
///
/// Gebruik: selecteer je prefab(s) in het Project-venster en kies
/// 'Tools > SourceSafeAR > Make Selected Prefabs Interactive'.
/// </summary>
public static class SourceSafeMakeInteractive
{
    [MenuItem("Tools/SourceSafeAR/Make Selected Prefabs Interactive")]
    public static void MakeInteractive()
    {
        var selection = Selection.objects;
        if (selection == null || selection.Length == 0)
        {
            Debug.LogWarning("[MakeInteractive] Selecteer eerst één of meer prefabs in het Project-venster.");
            return;
        }

        int changed = 0;
        foreach (var selected in selection)
        {
            string path = AssetDatabase.GetAssetPath(selected);
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab"))
                continue;

            // Prefab-inhoud openen om veilig te bewerken, en na afloop weer sluiten.
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (MakeRootInteractive(root))
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    changed++;
                    Debug.Log($"[MakeInteractive] Interactief gemaakt: {path}");
                }
                else
                {
                    Debug.Log($"[MakeInteractive] Overgeslagen (al interactief of geen mesh): {path}");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[MakeInteractive] Klaar: {changed} prefab(s) aangepast.");
    }

    static bool MakeRootInteractive(GameObject root)
    {
        // Al interactief? Dan overslaan (zo kun je de tool veilig opnieuw draaien).
        if (root.GetComponent<XRGrabInteractable>() != null)
            return false;

        // Gecombineerde bounds van alle zichtbare delen (werkt ook meerdelig).
        var renderers = root.GetComponentsInChildren<MeshRenderer>();
        if (renderers.Length == 0)
            return false;

        Bounds worldBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            worldBounds.Encapsulate(renderers[i].bounds);

        // 1. Kinematische Rigidbody.
        var rigidbody = root.GetComponent<Rigidbody>();
        if (rigidbody == null)
            rigidbody = root.AddComponent<Rigidbody>();
        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;

        // 2. BoxCollider om het hele model. We rekenen de wereld-bounds om naar
        //    de lokale ruimte van de root (bij identiteitsrotatie is dit exact).
        var boxCollider = root.AddComponent<BoxCollider>();
        boxCollider.center = root.transform.InverseTransformPoint(worldBounds.center);
        Vector3 lossyScale = root.transform.lossyScale;
        boxCollider.size = new Vector3(
            Mathf.Abs(lossyScale.x) > 0f ? worldBounds.size.x / lossyScale.x : worldBounds.size.x,
            Mathf.Abs(lossyScale.y) > 0f ? worldBounds.size.y / lossyScale.y : worldBounds.size.y,
            Mathf.Abs(lossyScale.z) > 0f ? worldBounds.size.z / lossyScale.z : worldBounds.size.z);

        // 3. XRGrabInteractable: selecteren + verplaatsen. Instantaneous is nodig
        //    zodat het kinematische object direct meebeweegt (i.p.v. via fysica).
        var grabInteractable = root.AddComponent<XRGrabInteractable>();
        grabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;

        // 4. ARTransformer: verplaatsen over gedetecteerde vlakken + schalen.
        //    Schaalgrenzen relatief aan de huidige grootte van het prefab.
        var arTransformer = root.AddComponent<ARTransformer>();
        float baseScale = root.transform.localScale.x;
        if (baseScale <= 0f)
            baseScale = 1f;
        arTransformer.minScale = baseScale * 0.3f;
        arTransformer.maxScale = baseScale * 3f;

        return true;
    }
}
