using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PickUpSpawner : MonoBehaviour
{
    [SerializeField] private GameObject goldCoin, heath, stamina;

    public void DropItems()
    {
        int randonNum = Random.Range(1, 4);

        if(randonNum == 1)
        {
            Instantiate(heath, transform.position, Quaternion.identity);
        }
        if(randonNum == 2)
        {
            Instantiate(stamina, transform.position, Quaternion.identity);  
        }
        if (randonNum == 3)
        {
            int randomAmountOfGold = Random.Range(1, 5);
            for( int i = 0; i < randomAmountOfGold; i++ )
            {
                Instantiate(goldCoin, transform.position, Quaternion.identity);
            }
        }
    }
}
