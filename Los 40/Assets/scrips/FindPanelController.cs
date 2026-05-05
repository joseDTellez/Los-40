using System.Collections;
using UnityEngine;

public class FindPanelController : MonoBehaviour
{
    public CanvasGroup canvasGroup;

    public float fadeDuration = 0.5f;
    public float visibleTime = 3f;

    Coroutine currentRoutine;

    public void ShowPanel()
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(FadeSequence());
    }

    IEnumerator FadeSequence()
    {
        yield return StartCoroutine(Fade(0, 1));
        yield return new WaitForSeconds(visibleTime);
        yield return StartCoroutine(Fade(1, 0));
    }

    IEnumerator Fade(float start, float end)
    {
        float time = 0;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, end, time / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = end;
    }
}