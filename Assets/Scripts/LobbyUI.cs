using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI createRoomName;
    [SerializeField] private TextMeshProUGUI joinRoomName;
    [SerializeField] private TextMeshProUGUI enterPlayerName;
    [SerializeField] private TextMeshProUGUI currentName;

    private string _currentName;
    private string GetRandomName()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, 10)
            .Select(s => s[Random.Range(0,chars.Length)]).ToArray());
    }
    private void Start()
    {
        _currentName = GetRandomName();
        
        currentName.text = "Current name: " + _currentName;
    }

    public void ChangeName()
    {
        _currentName = enterPlayerName.text;
        currentName.text = "Current name: " + _currentName;
    }

    public async void CreateLobby()
    {
        await RoomConnect.Authenticate(_currentName);
        RoomConnect.CreateLobby(createRoomName.text);
    }

    public async void JoinRoom()
    {
        await RoomConnect.Authenticate(_currentName);
        Debug.Log("Trying to enter in lobby " + joinRoomName.text);
        RoomConnect.JoinLobbyByName(joinRoomName.text);
    }
}
