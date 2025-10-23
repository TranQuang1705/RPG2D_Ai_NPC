using UnityEngine;

public class AnimationEventHandler : MonoBehaviour
{
    public delegate void AnimationEndEvent();
    public event AnimationEndEvent OnAnimationEnd;

    public void AnimationComplete()
    {
        OnAnimationEnd?.Invoke();
    }
}
