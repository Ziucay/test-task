using System.Threading.Tasks;
using TMPro;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Warrior : NetworkBehaviour
{
    public Sprite idleSprite;
    public Sprite shootingSprite;
    [SerializeField] private GameObject projectile;
    [SerializeField] private float speed;
    [SerializeField] private TextMeshProUGUI coins;
    [SerializeField] private Slider healthbar;

    private Rigidbody2D _rigidbody;
    private SpriteRenderer _spriteRenderer;

    public InputAction movement;
    public InputAction attack;

    private bool canAttack;
    
    private int _coins = 0;

    public int Coins
    {
        get => _coins;
        set
        {
            _coins = value;
            if (IsOwner)
                coins.text = "Coins: " + _coins;
        }
    }

    private bool _isDead = false;
    private int _health = 100;
    
    public int Health
    {
        get => _health;
        private set
        {
            _health = value;
            if (IsOwner)
            {
                healthbar.value = _health;
                if (_health <= 0)
                {
                    healthbar.gameObject.SetActive(false);
                    coins.gameObject.SetActive(false);
                    _isDead = true;
                    //TODO: death sequence
                }
            }
                
            
            
        }
    }

    [ServerRpc]
    private void DespawnServerRpc()
    {
        gameObject.GetComponent<NetworkObject>().Despawn(true);
    }

    public override void OnNetworkSpawn()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        movement.Enable();
        attack.Enable();
        
        canAttack = true;
        //TODO: init coins
        coins = GameObject.Find("Coins").GetComponent<TextMeshProUGUI>();
        healthbar = GameObject.Find("HealthBar").GetComponent<Slider>();
        Debug.Log("I spawned! I am a " + (IsHost ? "Host" : "Client"));
    }

    public override void OnNetworkDespawn()
    {
        movement.Disable();
        attack.Disable();
    }

    private void Update()
    {
        if (!IsOwner) return;

        Vector2 movementVector = movement.ReadValue<Vector2>(); 
        if (!(Mathf.Approximately(movementVector.x, 0f) && Mathf.Approximately(movementVector.y, 0f)))
            Movement(movementVector);
        
        if (attack.WasPressedThisFrame() && canAttack)
            Shoot();
    }

    private void Movement(Vector2 moveVector)
    {
        MovementServerRpc(moveVector.x, moveVector.y, OwnerClientId);
    }

    [ServerRpc]
    private void MovementServerRpc(float moveVectorX, float moveVectorY, ulong clientId )
    {
        ApplyMovementClientRpc(moveVectorX, moveVectorY, clientId);
    }

    [ClientRpc]
    private void ApplyMovementClientRpc(float moveVectorX, float moveVectorY, ulong clientId)
    {
        if (clientId != OwnerClientId) return;
        
        Vector2 moveVector = new Vector2(moveVectorX, moveVectorY);
        _rigidbody.velocity = moveVector * speed;
        
        if (!Mathf.Approximately(moveVector.x, 0f) && !Mathf.Approximately(moveVector.y, 0f))
        {
            float degrees = Mathf.Rad2Deg * Mathf.Atan2(moveVector.y,moveVector.x);
            transform.rotation = Quaternion.AngleAxis(degrees, Vector3.forward);
        }
    }
    
    private async void Shoot()
    {
        Debug.Log("Shoot");
        canAttack = false;
        _spriteRenderer.sprite = shootingSprite;
        CreateProjectileServerRpc(OwnerClientId, new ServerRpcParams());
        await Task.Delay(500);
        _spriteRenderer.sprite = idleSprite;
        canAttack = true;
    }

    [ServerRpc]
    private void CreateProjectileServerRpc(ulong ownerClientId, ServerRpcParams rpcParams)
    {
        GameObject obj = Instantiate(projectile, gameObject.transform.position, quaternion.identity);
            
        obj.GetComponent<NetworkObject>().Spawn(true);
        obj.GetComponent<Projectile>().realOwner = rpcParams.Receive.SenderClientId;
        obj.GetComponent<Projectile>().ShootClientRpc(transform.right);
        //CreateProjectileClientRpc(ownerClientId);
    }

    /*
     [ClientRpc]
    private void CreateProjectileClientRpc(ulong ownerClientId)
    {
        if (ownerClientId == OwnerClientId)
        {
            GameObject obj = Instantiate(projectile, gameObject.transform.position, quaternion.identity);
            
            obj.GetComponent<NetworkObject>().Spawn(true);
            
        }
    }
     */

    [ServerRpc(RequireOwnership = false)]
    public void AddCoinServerRpc(ulong ownerClientId)
    {
        AddCoinClientRpc(ownerClientId);
    }

    [ClientRpc]
    private void AddCoinClientRpc(ulong ownerClientId)
    {
        if (ownerClientId == OwnerClientId)
        {
            Coins += 1;
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void DecreaseHealthServerRpc(ulong ownerClientId)
    {
        DecreaseHealthClientRpc(ownerClientId);
    }

    [ClientRpc]
    private void DecreaseHealthClientRpc(ulong ownerClientId)
    {
        if (ownerClientId == OwnerClientId)
        {
            Health -= 10;
        }
    }
    
}
