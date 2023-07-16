using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Warrior : NetworkBehaviour
{
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite shootingSprite;
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
            coins.text = "Coins: " + _coins;
        }
    }
    
    private int _health = 100;
    
    public int Health
    {
        get => _health;
        private set
        {
            _health = value;
            healthbar.value = _health;
        }
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log("I spawned! I am a " + (IsHost ? "Host" : "Client"));
        _rigidbody = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        movement.Enable();
        attack.Enable();
        
        canAttack = true;
        base.OnNetworkSpawn();
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
        
        //if (attack.WasPressedThisFrame() && canAttack)
        //    Shoot();
    }

    private void Movement(Vector2 moveVector)
    {
        MovementServerRpc(moveVector.x, moveVector.y, OwnerClientId);
    }

    private async void Shoot()
    {
        Debug.Log("Shoot");
        canAttack = false;
        _spriteRenderer.sprite = shootingSprite;
        await Task.Delay(500);
        _spriteRenderer.sprite = idleSprite;
        canAttack = true;
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
    
}
