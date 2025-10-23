using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageSource : MonoBehaviour
{
     private int damageAmount;

    private void Start()
    {
        MonoBehaviour currentActiveWeapon = ActiveWeapon.Instance.CurrentActiveWeapon;
        damageAmount = (currentActiveWeapon as iWeapon).GetWeaponInfo().weaponDamage;
    }
    public void OnTriggerEnter2D(Collider2D other)
    {
        EnemyHealth enemyHealth = other.gameObject.GetComponent<EnemyHealth>();
        BossHealth bossHealth = other.gameObject.GetComponent<BossHealth>();
        enemyHealth?.TakeDamage(damageAmount);
        bossHealth?.TakeDamage(damageAmount);
    }
}
