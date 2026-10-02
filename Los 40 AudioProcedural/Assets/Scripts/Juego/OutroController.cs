using UnityEngine;
using System.Collections;

public class OutroController : MonoBehaviour
{
    public float outroDuration = 106f;
    public bool quitAfterOutro = true;

    void Start()
    {
        StartCoroutine(PlayOutro());
    }

    private IEnumerator PlayOutro()
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