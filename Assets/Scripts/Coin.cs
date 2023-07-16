using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coin : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D col)
    {
        Warrior warriorSc = null;
        if (col.gameObject.TryGetComponent<Warrior>(out warriorSc))
        {
            warriorSc.Coins += 1;
        }
        Destroy(gameObject);
    }
}
