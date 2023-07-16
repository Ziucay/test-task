using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Coin : NetworkBehaviour
{
    private void OnTriggerEnter2D(Collider2D col)
    {
        if (!IsHost) return;

        Warrior warriorSc = null;
        if (col.gameObject.TryGetComponent(out warriorSc))
        {
            warriorSc.AddCoinServerRpc(warriorSc.OwnerClientId);
            gameObject.GetComponent<NetworkObject>().Despawn(true);
        }
    }
}
