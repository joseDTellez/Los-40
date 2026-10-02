using UnityEngine;

public class NPCAnimationTrigger : MonoBehaviour
{
    private static readonly int IsTalking = Animator.StringToHash("IsTalking");
    private Animator anim;

    void Awake()
    {
        anim = GetComponent<Animator>();
    }

    public void StartTalking()
    {
        if (anim != null) anim.SetBool(IsTalking, true);
    }

    public void StopTalking()
    {
        if (anim != null) anim.SetBool(IsTalking, false);
    }
}