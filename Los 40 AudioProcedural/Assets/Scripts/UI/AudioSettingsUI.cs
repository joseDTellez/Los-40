using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSettingsUI : MonoBehaviour
{
    [Header("Mixer")]
    public AudioMixer audioMixer;

    [Header("Sliders")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    // Parámetros expuestos en MainAudioMixer. El slider de SFX controla todos los grupos
    // que no son música: ambiente, voces, efectos y sonidos del jugador (pasos, respiración).
    private static readonly string[] ParametrosSFX = { "SFXVol", "VocesVol", "EfectosVol", "JugadorVol" };

    void Start()
    {
        Inicializar(masterSlider, "MasterVol", SetMasterVolume);
        Inicializar(musicSlider, "MusicVol", SetMusicVolume);
        Inicializar(sfxSlider, "SFXVol", SetSFXVolume);
    }

    static void Inicializar(Slider slider, string clave, System.Action<float> aplicar)
    {
        float valor = PlayerPrefs.GetFloat(clave, 0.8f);
        // Asignar el slider dispara su evento OnValueChanged; si no hay slider, aplicamos directo
        if (slider != null) slider.SetValueWithoutNotify(valor);
        aplicar(valor);
    }

    public void SetMasterVolume(float value) => Aplicar("MasterVol", value, "MasterVol");
    public void SetMusicVolume(float value) => Aplicar("MusicVol", value, "MusicVol");
    public void SetSFXVolume(float value) => Aplicar("SFXVol", value, ParametrosSFX);

    void Aplicar(string clave, float value, params string[] parametros)
    {
        value = Mathf.Clamp(value, 0.0001f, 1f);
        float dB = Mathf.Log10(value) * 20f;

        if (audioMixer != null)
            foreach (string p in parametros)
                audioMixer.SetFloat(p, dB);

        PlayerPrefs.SetFloat(clave, value);
    }

    void OnDisable()
    {
        // Asegura que los volúmenes se guarden aunque el juego se cierre de golpe
        PlayerPrefs.Save();
    }
}
