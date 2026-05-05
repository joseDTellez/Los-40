using UnityEngine;
using UnityEngine.UI;

public class ObjectiveProgressManager : MonoBehaviour
{
    public static ObjectiveProgressManager Instance;

    public Text progressText;
    public Text messageText; // texto principal del panel
    public FindPanelController panel;

    public int totalClues = 5;
    int currentClues = 0;

    void Awake()
    {
        Instance = this;
        UpdateUI();
    }

    public void AddClue(string message)
    {
        currentClues++;
        currentClues = Mathf.Clamp(currentClues, 0, totalClues);

        // Actualiza contador
        progressText.text = "Pistas: " + currentClues + "/" + totalClues;

        // Mensaje contextual
        messageText.text = message;

        panel.ShowPanel();
    }
    public void RegisterClue()
    {
        ObjectiveProgressManager.Instance.AddClue("El tendero habló sobre la situación");
    }

    void UpdateUI()
    {
        progressText.text = "Pistas: " + currentClues + "/" + totalClues;
    }
}