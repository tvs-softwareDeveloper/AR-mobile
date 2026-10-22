using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Beheert de levensloop van de AR-sessie: controleert of het toestel AR
/// ondersteunt, houdt de huidige sessiestatus bij en biedt een nette manier
/// om de sessie te herstarten.
///
/// De ARSession-component zelf regelt pauze/hervat (app minimaliseren, scherm
/// uit) al automatisch. Deze controller voegt daar bovenop: een controle op
/// beschikbaarheid en logging van de status, zodat we later (Fase 8) hier een
/// nette melding aan kunnen hangen als AR niet werkt.
///
/// Hang dit script op hetzelfde GameObject als de ARSession-component
/// (het "AR Session"-object in de scene).
/// </summary>
[RequireComponent(typeof(ARSession))]
public class ARSessionController : MonoBehaviour
{
    // Verwijzing naar de ARSession-component op dit GameObject.
    private ARSession arSession;

    /// <summary>
    /// True zodra de AR-sessie draait of aan het opstarten is. Handig als
    /// andere scripts (plaatsing, UI) willen weten of AR bruikbaar is.
    /// </summary>
    public bool IsSessionReady =>
        ARSession.state == ARSessionState.SessionTracking
        || ARSession.state == ARSessionState.SessionInitializing;

    private void Awake()
    {
        // ARSession staat gegarandeerd op dit GameObject (zie RequireComponent).
        arSession = GetComponent<ARSession>();
    }

    private void OnEnable()
    {
        // stateChanged is een statisch event: het geldt voor de hele AR-sessie,
        // niet per component. We volgen het om de status te kunnen loggen.
        ARSession.stateChanged += OnSessionStateChanged;
    }

    private void OnDisable()
    {
        // Altijd netjes afmelden om "ghost"-callbacks na uitschakelen te voorkomen.
        ARSession.stateChanged -= OnSessionStateChanged;
    }

    private IEnumerator Start()
    {
        // Op sommige toestellen weet Unity nog niet meteen of AR werkt.
        // We wachten dan tot de beschikbaarheidscontrole klaar is.
        if (ARSession.state == ARSessionState.None
            || ARSession.state == ARSessionState.CheckingAvailability)
        {
            yield return ARSession.CheckAvailability();
        }

        if (ARSession.state == ARSessionState.Unsupported)
        {
            // Toestel ondersteunt geen ARCore. In Fase 8 tonen we hier een
            // duidelijke boodschap in de UI; voor nu volstaat een waarschuwing.
            Debug.LogWarning("[ARSessionController] Dit toestel ondersteunt geen AR (ARCore).");
        }
        else
        {
            // AR wordt ondersteund: zorg dat de sessie actief is.
            arSession.enabled = true;
        }
    }

    /// <summary>
    /// Herstart de AR-sessie volledig. Dit wist alle tracking en gevonden
    /// vlakken. We gebruiken dit later bij de reset-functie (Fase 7).
    /// </summary>
    public void RestartSession()
    {
        arSession.Reset();
    }

    private void OnSessionStateChanged(ARSessionStateChangedEventArgs args)
    {
        // Tijdens het testen kun je deze status volgen via Android logcat.
        Debug.Log($"[ARSessionController] AR-sessiestatus: {args.state}");
    }
}
