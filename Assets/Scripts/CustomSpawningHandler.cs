using Unity.Netcode;
using UnityEngine;

public class CustomSpawningHandler : NetworkBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private Vector3[] points;
    [SerializeField] private Sprite[] idleSprites;
    [SerializeField] private Sprite[] shootingSprites;

    private int objectsSpawned = 0;

    public override void OnNetworkSpawn()
    {
        RequestPlayerSpawnServerRpc(OwnerClientId);
    }

    [ServerRpc]
    private void RequestPlayerSpawnServerRpc(ulong ownerClientId)
    {
        Debug.Log("Custom player spawn");
        if (objectsSpawned >= points.Length)
        {
            Debug.LogWarning("Too many spawned objects, or incorrect amount of points.");
            return;
        }

        GameObject obj = Instantiate(player, points[objectsSpawned], Quaternion.identity);
        SetPlayerColor(obj);
        objectsSpawned += 1;
        obj.GetComponent<NetworkObject>().SpawnWithOwnership(ownerClientId, true);
    }
    
    private void SetPlayerColor(GameObject obj)
    {
        Warrior warrior;
        SpriteRenderer renderer;
        if (obj.TryGetComponent(out warrior) && obj.TryGetComponent(out renderer))
        {
            renderer.sprite = idleSprites[objectsSpawned];
            warrior.idleSprite = idleSprites[objectsSpawned];
            warrior.shootingSprite = shootingSprites[objectsSpawned];
        }
        else
        {
            Debug.LogWarning("Object do not have Warrior script or SpriteRenderer");
        }
    }
}
