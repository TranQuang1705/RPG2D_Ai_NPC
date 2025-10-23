using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Staff : MonoBehaviour, iWeapon
{
    [SerializeField] private WeaponInfo weaponInfo;
    [SerializeField] private GameObject magicLazer;
    [SerializeField] private Transform magicLazerSpawnPoint;

    [Header("Sound Settings")]
    [SerializeField] private AudioClip spellCastSound;
    private AudioSource audioSource;

    private Animator myAnimator;

    readonly int ATTACK_HASH = Animator.StringToHash("Attack");

    private void Awake()
    {
        myAnimator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    private void Update()
    {
        MouseFollowWithOffset();
    }
    public void Attack()
    {
        myAnimator.SetTrigger(ATTACK_HASH);
        if (spellCastSound != null)
        {
            audioSource.PlayOneShot(spellCastSound);
        }
    }
    public void SpawnStaffProjectileAnimEvent()
    {
        GameObject newLazer = Instantiate(magicLazer, magicLazerSpawnPoint.position, Quaternion.identity);
        newLazer.GetComponent<MagicLazer>().UpdateLazerRange(weaponInfo.weaponRange);
    }
    public WeaponInfo GetWeaponInfo()
    {
        return weaponInfo;
    }
    
    private void MouseFollowWithOffset()
    {
        Vector3 mousePos = Input.mousePosition;
        Vector3 playerSreenPoint = Camera.main.WorldToScreenPoint(PlayerController.Instance.transform.position);

        float angle = Mathf.Atan2(mousePos.y, mousePos.x) * Mathf.Rad2Deg;
        if(mousePos.x < playerSreenPoint.x)
        {
            ActiveWeapon.Instance.transform.rotation = Quaternion.Euler(0, -180, angle);
        }
        else
        {
            ActiveWeapon.Instance.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

}
