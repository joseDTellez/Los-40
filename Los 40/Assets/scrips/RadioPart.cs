using UnityEngine;

public class RadioPart : MonoBehaviour
{
    public enum TipoParte { Cuerpo, PerillaIzquierda, PerillaDerecha }

    [Header("Configuración")]
    public RadioController mainController;
    public TipoParte parte;

    private OutlineVR _brillo;

    void Start()
    {
        _brillo = GetComponent<OutlineVR>();
        if (_brillo) _brillo.enabled = false;
    }

    public void OnPointerEnter()
    {
        // 1. Efecto visual de esta pieza
        if (_brillo)
        {
            _brillo.enabled = true;
            _brillo.SetState(OutlineVR.InteractionState.Hover);
        }

        // 2. Comunicar al controlador exactamente qué estamos mirando
        if (mainController)
        {
            switch (parte)
            {
                case TipoParte.Cuerpo:
                    mainController.OnPointerEnter();
                    break;
                case TipoParte.PerillaIzquierda:
                    mainController.OnPointerEnterLeft();
                    break;
                case TipoParte.PerillaDerecha:
                    mainController.OnPointerEnterRight();
                    break;
            }
        }
    }

    public void OnPointerExit()
    {
        if (_brillo) _brillo.enabled = false;

        if (mainController)
        {
            mainController.OnPointerExit();
        }
    }
}