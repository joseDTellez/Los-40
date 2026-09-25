using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ObjectiveTriggerUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject panelRoot;
    public float fadeDuration = 0.8f;
    public float displayDuration = 3f;

    [Header("Configuración")]
    public bool triggerOnce = true;

    private bool _activated = false;
    private CanvasGroup _canvasGroup;
    private Coroutine _routine;

    void Start()
    {
        // Forzar Is Trigger en el collider
        GetComponent<Collider>().isTrigger = true;

        if (panelRoot == null) return;

        _canvasGroup = panelRoot.GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = panelRoot.AddComponent<CanvasGroup>();

        _canvasGroup.alpha = 0f;
        panelRoot.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (_activated && triggerOnce) return;

        _activated = true;

        if (_routine != null)
            StopCoroutine(_routine);

        _routine = StartCoroutine(MostrarUI());
    }

    private IEnumerator MostrarUI()
    {
        if (_canvasGroup == null) yield break;

        // Fade IN
        panelRoot.SetActive(true);
        yield return StartCoroutine(Fade(0f, 1f));

        // Espera visible
        yield return new WaitForSeconds(displayDuration);

        // Fade OUT
        yield return StartCoroutine(Fade(1f, 0f));
        panelRoot.SetActive(false);

        // Si no es triggerOnce, resetear para que pueda volver a activarse
        if (!triggerOnce)
            _activated = false;
    }

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        _canvasGroup.alpha = from;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }

        _canvasGroup.alpha = to;
    }

    // Para debug: resetear desde el inspector en runtime
    [ContextMenu("Resetear Trigger")]
    public void ResetTrigger()
    {
        _activated = false;

        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        if (_canvasGroup != null) _canvasGroup.alpha = 0f;
        if (panelRoot != null) panelRoot.SetActive(false);
    }
}