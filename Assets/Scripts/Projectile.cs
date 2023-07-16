using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    private bool isShooted = false;
    public ulong realOwner = 1234;
    
    [ClientRpc]
    public void ShootClientRpc(Vector2 direction)
    {
        if (isShooted) return;
        isShooted = true;

        GetComponent<Rigidbody2D>().velocity = direction * 25;
    }
    
    private void OnTriggerEnter2D(Collider2D col)
    {
        if (!IsHost) return;

        Warrior warriorSc = null;
        if (col.gameObject.TryGetComponent(out warriorSc))
        {
            if (realOwner != 1234 && warriorSc.OwnerClientId != realOwner)
            {
                warriorSc.DecreaseHealthServerRpc(warriorSc.OwnerClientId);
                gameObject.GetComponent<NetworkObject>().Despawn(true);
            }
                
        }

        if (col.name == "Walls")
        {
            gameObject.GetComponent<NetworkObject>().Despawn(true);
        }
    }
}