using UnityEngine;
using UnityEngine.EventSystems; // Necesario si usas el sistema de VR de Unity

public class InteractableObject : MonoBehaviour
{
    private Outline outline;

    void Start()
    {
        outline = GetComponent<Outline>();
        // Empezamos respirando
        outline.SetState(Outline.InteractionState.Idle);
    }

    // Estos métodos funcionan con el Gaze de Google Cardboard, XR Interaction Toolkit o Mouse
    public void OnPointerEnter()
    {
        // Al mirarlo: Se queda fijo y aumenta de tamaño
        outline.SetState(Outline.InteractionState.Hover);
    }

    public void OnPointerExit()
    {
        // Al dejar de mirarlo: Vuelve a respirar suavemente
        outline.SetState(Outline.InteractionState.Idle);
    }

    public void OnSelect()
    {
        // Al interactuar/hacer click: Se apaga para no estorbar
        outline.SetState(Outline.InteractionState.Interacting);
    }
}