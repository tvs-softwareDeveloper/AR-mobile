using UnityEngine;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Zet de visualisatie van gedetecteerde AR-vlakken (planes) aan of uit.
///
/// Belangrijk onderscheid: de ARPlaneManager blijft áltijd doordetecteren.
/// We verbergen hier alleen de visuele weergave (de gekleurde mesh + rand),
/// niet de vlakken zelf. Zo houden we de tracking en de plaatsing-raycasts
/// (Fase 2) volledig intact, terwijl de AR-weergave rustiger oogt.
///
/// Hang dit script op hetzelfde GameObject als de ARPlaneManager
/// (in AR Foundation 6 is dat het "XR Origin"-object).
/// </summary>
[RequireComponent(typeof(ARPlaneManager))]
public class ARPlaneVisibilityController : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Zijn de vlakken zichtbaar zodra de app start?")]
    private bool planesVisibleOnStart = true;

    // Verwijzing naar de ARPlaneManager op dit GameObject.
    private ARPlaneManager planeManager;

    // Onthoudt de huidige keuze, zodat nieuw ontdekte vlakken die ook volgen.
    private bool planesVisible;

    private void Awake()
    {
        planeManager = GetComponent<ARPlaneManager>();
    }

    private void OnEnable()
    {
        // trackablesChanged vuurt elke keer als er vlakken bijkomen, wijzigen
        // of verdwijnen. We luisteren zodat elk nieuw vlak meteen de juiste
        // zichtbaarheid krijgt (anders zou een net ontdekt vlak toch oplichten).
        planeManager.trackablesChanged.AddListener(OnPlanesChanged);
    }

    private void OnDisable()
    {
        planeManager.trackablesChanged.RemoveListener(OnPlanesChanged);
    }

    private void Start()
    {
        SetPlanesVisible(planesVisibleOnStart);
    }

    /// <summary>Wisselt tussen zichtbaar en verborgen. Later te koppelen aan een knop.</summary>
    public void TogglePlaneVisibility()
    {
        SetPlanesVisible(!planesVisible);
    }

    /// <summary>Zet alle op dit moment bekende vlakken zichtbaar of onzichtbaar.</summary>
    public void SetPlanesVisible(bool visible)
    {
        planesVisible = visible;

        foreach (ARPlane plane in planeManager.trackables)
        {
            ApplyVisibility(plane, visible);
        }
    }

    private void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> args)
    {
        // Alleen nieuw toegevoegde vlakken hoeven bijgewerkt te worden;
        // bestaande vlakken hebben hun instelling al.
        foreach (ARPlane plane in args.added)
        {
            ApplyVisibility(plane, planesVisible);
        }
    }

    private void ApplyVisibility(ARPlane plane, bool visible)
    {
        // We schakelen de Renderers uit in plaats van het hele GameObject,
        // zodat de plane-data (die de raycasts gebruiken) beschikbaar blijft.
        foreach (Renderer planeRenderer in plane.GetComponentsInChildren<Renderer>())
        {
            planeRenderer.enabled = visible;
        }
    }
}
