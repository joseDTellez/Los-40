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

        if (_brillo)
        {
            _brillo.SetState(OutlineVR.InteractionState.Idle);
        }
    }

    // Al quitarle los parámetros a esta función, el SendMessage del Cardboard 
    // Reticle la detectará perfectamente sin lanzar errores rojos.
    public void OnPointerEnter()
    {
        // 1. Activa el brillo sutil (Outline)
        if (_brillo)
        {
            _brillo.SetState(OutlineVR.InteractionState.Hover);
        }

        // 2. Le avisa al cerebro principal (RadioController) qué estamos mirando
        if (mainController)
        {
            if (parte == TipoParte.Cuerpo) mainController.MirarCuerpo();
            else if (parte == TipoParte.PerillaIzquierda) mainController.MirarIzquierda();
            else if (parte == TipoParte.PerillaDerecha) mainController.MirarDerecha();
        }
    }

    public void OnPointerExit()
    {
        // Apaga el brillo
        if (_brillo)
        {
            _brillo.SetState(OutlineVR.InteractionState.Idle);
        }

        // Le avisa al cerebro principal que dejamos de mirar
        if (mainController)
        {
            mainController.OnPointerExit();
        }
    }
}