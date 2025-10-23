using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bow : MonoBehaviour, iWeapon
{
    [SerializeField] private WeaponInfo weaponInfo;
    [SerializeField] private GameObject arrowPerfab;
    [SerializeField] private Transform arrowSpawnPoint;

    [Header("Bow Sounds")]
    [SerializeField] private AudioClip shootSound; 
    private AudioSource audioSource;

    readonly int FIRE_HASH = Animator.StringToHash("Fire");
    private Animator myAnimator;

    private void Awake()
    {
        myAnimator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    public void Attack()
    {
        PlaySound(shootSound);

        myAnimator.SetTrigger(FIRE_HASH);
        GameObject newArrow = Instantiate(arrowPerfab, arrowSpawnPoint.position, ActiveWeapon.Instance.transform.rotation);  
        newArrow.GetComponent<ProjectTile>().UpdateProjectileRange(weaponInfo.weaponRange);
       
    }
    public WeaponInfo GetWeaponInfo()
    {
        return weaponInfo;
    }
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
