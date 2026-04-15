using UnityEngine;
using UnityEngine.EventSystems;

public class CanvasPointerFix : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // Arrastra aquí el objeto (Periódico, Radio, etc.) que tiene el script principal
    // Asegúrate de que el script en el objeto sea el que acabamos de corregir
    public ObjectController mainController;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (mainController != null)
        {
            // Le avisa al panel que lo estamos mirando directamente
            mainController.OnPanelEnter();
            // También llamamos a OnPointerEnter para cancelar cualquier proceso de cierre
            mainController.OnPointerEnter();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (mainController != null)
        {
            // Avisa que la mirada salió del panel
            mainController.OnPanelExit();
            // Inicia el tiempo de gracia antes de considerar que el usuario dejó de mirar
            mainController.OnPointerExit();
        }
    }
}