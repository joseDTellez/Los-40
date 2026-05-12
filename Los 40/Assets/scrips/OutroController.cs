using UnityEngine;
using System.Collections;

public class OutroController : MonoBehaviour
{
    public float outroDuration = 5f;
    public string nextScene = "CreditsScene";

    // ? Llama este método desde VerificarProgreso() en GameManagerNew
    public void PlayOutro()
    {
        StartCoroutine(OutroCoroutine());
    }

    IEnumerator OutroCoroutine()
    {
        yield return new WaitForSeconds(outroDuration);
        SceneFadeTransition.Instance.LoadScene(nextScene);
    }
}