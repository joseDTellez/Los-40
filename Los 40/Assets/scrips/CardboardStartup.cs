using Google.XR.Cardboard;
using UnityEngine;

/// <summary>
/// Initializes Cardboard XR Plugin con optimización de estabilidad.
/// </summary>
public class CardboardStartup : MonoBehaviour
{
    [Header("Configuración de Estabilidad")]
    [Tooltip("Segundos que debe sostener el gatillo para recentrar la vista")]
    public float holdTimeForRecenter = 1.5f;
    private float _triggerTimer = 0f;

    public void Start()
    {
        // Evita que la pantalla se apague
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        // Brillo al máximo (funciona mejor en iOS, pero no estorba en Android)
        Screen.brightness = 1.0f;

        // Configuración inicial de parámetros del visor
        if (!Api.HasDeviceParams())
        {
            Api.ScanDeviceParams();
        }
    }

    public void Update()
    {
        // 1. Abrir configuración (Botón de engranaje)
        if (Api.IsGearButtonPressed)
        {
            Api.ScanDeviceParams();
        }

        // 2. Salir de la App (Botón X)
        if (Api.IsCloseButtonPressed)
        {
            Application.Quit();
        }

        // 3. RECENTRADO OPTIMIZADO:
        // En lugar de recentrar instantáneamente (que causa saltos), 
        // pedimos que el usuario mantenga presionado el botón.
        if (Api.IsTriggerHeldPressed)
        {
            _triggerTimer += Time.deltaTime;
            if (_triggerTimer >= holdTimeForRecenter)
            {
                Api.Recenter();
                _triggerTimer = 0f; // Evita recentros infinitos
                Debug.Log("Vista recentrada");
            }
        }
        else
        {
            _triggerTimer = 0f;
        }

        // 4. Actualización de parámetros del dispositivo
        if (Api.HasNewDeviceParams())
        {
            Api.ReloadDeviceParams();
        }

        // Importante para mantener la distorsión de lente correcta en cada frame
        Api.UpdateScreenParams();
    }
}