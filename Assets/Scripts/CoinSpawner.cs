using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CoinSpawner : NetworkBehaviour
{
    [SerializeField] private Transform[] coinSpawnPoints;
    [SerializeField] private GameObject coin;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        Debug.Log("CoinSpawner created!");

        foreach (Transform point in coinSpawnPoints)
        {
            GameObject obj = Instantiate(coin, point.transform.position, Quaternion.identity);
            obj.GetComponent<NetworkObject>().Spawn(true);
        }
    }
}
