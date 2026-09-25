using UnityEngine;
using DialogueEditor;

public class DialogueUIFollower : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;

    [SerializeField] private float distance = 1.2f;
    [SerializeField] private float heightOffset = -0.25f;

    private void OnEnable()
    {
        ConversationManager.OnConversationStarted += OnConversationStart;
    }

    private void OnDisable()
    {
        ConversationManager.OnConversationStarted -= OnConversationStart;
    }

    private void OnConversationStart()
    {
        PosicionarDialogo();
    }

    void LateUpdate()
    {
        if (ConversationManager.Instance != null &&
            ConversationManager.Instance.IsConversationActive)
        {
            SeguirCamaraSuave();
        }
    }

    private void PosicionarDialogo()
    {
        if (cameraTransform == null) return;

        Vector3 targetPos =
            cameraTransform.position + cameraTransform.forward * distance;

        targetPos += cameraTransform.up * heightOffset;

        transform.position = targetPos;

        // 👇 ESTA ROTACIÓN SÍ es necesaria en World Space
        transform.rotation =
            Quaternion.LookRotation(transform.position - cameraTransform.position);
    }

    private void SeguirCamaraSuave()
    {
        if (cameraTransform == null) return;

        Vector3 targetPos =
            cameraTransform.position + cameraTransform.forward * distance;

        targetPos += cameraTransform.up * heightOffset;

        // 👇 IMPORTANTE: interpolación suave (evita glitch)
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 5f);

        transform.rotation =
            Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(transform.position - cameraTransform.position),
            Time.deltaTime * 5f);
    }
}