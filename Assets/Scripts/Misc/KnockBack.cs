using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KnockBack : MonoBehaviour
{
    public bool GettingKnockBack { get; private set; }

    [SerializeField] private float knockBackTime = .2f;
    [SerializeField] private AudioClip knockBackSound;


    private Rigidbody2D rb;
    private AudioSource audioSource;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

    }
    public void GetKnockBack(Transform damageSource, float knockBackThrust)
    {
        GettingKnockBack = true;
        Vector2 difference = (transform.position - damageSource.position).normalized * knockBackThrust * rb.mass;
        rb.AddForce(difference, ForceMode2D.Impulse);
        PlayKnockBackSound();
        StartCoroutine(KnockRoutine());
    }
    private IEnumerator KnockRoutine()
    {
        yield return new WaitForSeconds(knockBackTime);
        rb.velocity = Vector2.zero;
        GettingKnockBack = false;
    }
    private void PlayKnockBackSound()
    {
        if (knockBackSound != null)
        {
            audioSource.PlayOneShot(knockBackSound); 
        }
    }

}
