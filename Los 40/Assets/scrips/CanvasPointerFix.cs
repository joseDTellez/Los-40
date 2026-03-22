using UnityEngine;
using UnityEngine.EventSystems;

public class CanvasPointerFix : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // Arrastra aquí el objeto Periódico que tiene el script principal
    public ObjectController mainController;

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Le dice al controlador que no se apague porque estamos viendo el canvas
        mainController.OnPointerEnter();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        mainController.OnPointerExit();
    }
}