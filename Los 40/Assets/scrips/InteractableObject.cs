using UnityEngine;
using UnityEngine.EventSystems; // Necesario si usas el sistema de VR de Unity

public class InteractableObject : MonoBehaviour
{
    private OutlineVR outline;

    void Start()
    {
        outline = GetComponent<OutlineVR>();
        // Empezamos respirando
        outline.SetState(OutlineVR.InteractionState.Idle);
    }

    // Estos métodos funcionan con el Gaze de Google Cardboard, XR Interaction Toolkit o Mouse
    public void OnPointerEnter()
    {
        // Al mirarlo: Se queda fijo y aumenta de tamaño
        outline.SetState(OutlineVR.InteractionState.Hover);
    }

    public void OnPointerExit()
    {
        // Al dejar de mirarlo: Vuelve a respirar suavemente
        outline.SetState(OutlineVR.InteractionState.Idle);
    }

    public void OnSelect()
    {
        // Al interactuar/hacer click: Se apaga para no estorbar
        outline.SetState(OutlineVR.InteractionState.Interacting);
    }
}