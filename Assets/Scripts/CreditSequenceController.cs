using UnityEngine;
using UnityEngine.SceneManagement;

public class CreditSequenceController : MonoBehaviour
{
    public Animator[] creditAnimators; 
    public float delayAfterCredits = 2.0f; 

    private int completedAnimations = 0;

    private void Start()
    {
        foreach (var animator in creditAnimators)
        {
            animator.GetComponent<AnimationEventHandler>().OnAnimationEnd += HandleAnimationEnd;
        }
    }

    private void HandleAnimationEnd()
    {
        completedAnimations++;
        if (completedAnimations >= creditAnimators.Length)
        {
            Invoke("LoadFirstScene", delayAfterCredits);
        }
    }

    private void LoadFirstScene()
    {
        SceneManager.LoadScene(0); 
    }
}

