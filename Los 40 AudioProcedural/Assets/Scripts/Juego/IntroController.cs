using UnityEngine;
using System.Collections;

public class IntroController : MonoBehaviour
{
    public float introDuration = 5f;
    public string nextScene = "PCArregloUI";

    void Start()
    {
        StartCoroutine(PlayIntro());
    }

    IEnumerator PlayIntro()
    {
        yield return new WaitForSeconds(introDuration);

        SceneFadeTransition.Cargar(nextScene);
    }
}