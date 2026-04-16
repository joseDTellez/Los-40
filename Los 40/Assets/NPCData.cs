using UnityEngine;
using DialogueEditor;

[CreateAssetMenu(menuName = "NPC/NPC Data")]
public class NPCData : ScriptableObject
{
    [Header("Dialogue")]
    public NPCConversation conversation;
}