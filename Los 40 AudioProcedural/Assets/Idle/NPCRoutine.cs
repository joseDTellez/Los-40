using UnityEngine;
using System.Collections;

public class NPCRoutine : MonoBehaviour
{
    public Animator animator;

    void Start()
    {
        StartCoroutine(Rutina());
    }

    IEnumerator Rutina()
    {
        while (true)
        {
            // 1. Start Walking
            animator.Play("start_walking", 0, 0f);
            yield return new WaitForSeconds(2f);

            // 2. Walk in Circle
            animator.Play("walk_in_circle", 0, 0f);
            yield return new WaitForSeconds(6f);

            // 3. Looking
            animator.Play("looking", 0, 0f);
            yield return new WaitForSeconds(4f);
        }
    }
}