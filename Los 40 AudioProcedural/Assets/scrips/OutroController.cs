using UnityEngine;
using System.Collections;
using System;

public class OutroController : MonoBehaviour
{
    public float outroDuration = 106f;
    //public string nextScene = "CreditsScene";
    public bool quitAfterOutro = true;

    //  Llama este método desde VerificarProgreso() en GameManagerNew
   void Start()
    {
        StartCoroutine(PlayOutro());
    }

    public IEnumerator PlayOutro()
    {
        yield return new WaitForSeconds(outroDuration);

        if (quitAfterOutro)
        {
            QuitGame();
        }
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Detiene el Play Mode en el Editor
#else
        Application.Quit(); // Cierra el juego en build
#endif
    }
}