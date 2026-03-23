using UnityEngine;

public class RadioPart : MonoBehaviour
{
    public RadioController mainController;
    public enum Lado { Izquierda, Derecha }
    public Lado lado;
    private Outline _brillo;

    void Start() { _brillo = GetComponent<Outline>(); if (_brillo) _brillo.enabled = false; }

    public void OnPointerEnter()
    {
        if (_brillo) _brillo.enabled = true;
        if (mainController)
        {
            if (lado == Lado.Izquierda) mainController.OnPointerEnterLeft();
            else mainController.OnPointerEnterRight();
        }
    }

    public void OnPointerExit()
    {
        if (_brillo) _brillo.enabled = false;
        if (mainController) mainController.OnPointerExit();
    }
}