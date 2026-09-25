using UnityEngine;

public class NPCAnimationTrigger : MonoBehaviour
{
    private Animator anim;

    void Awake()
    {
        anim = GetComponent<Animator>();
    }

    public void StartTalking()
    {
        anim.SetBool("IsTalking", true);
    }

    public void StopTalking()
    {
        anim.SetBool("IsTalking", false);
    }
}