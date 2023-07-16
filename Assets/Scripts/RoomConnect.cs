using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IngameDebugConsole;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomConnect : MonoBehaviour
{
    private static Lobby hostLobby;
    private static Lobby joinedLobby;
    private float heartbeatTimer;
    private float heartbeatTimerMax = 15f;
    
    private float lobbyUpdateTimer;
    private float lobbyUpdateTimerMax = 1.1f;
    
    //TODO: player name
    public static string playerName;

    private static bool _relayJoined = false;

    private void Update()
    {
        HandleLobbyHeartbeat();
        HandleLobbyPollForUpdates();
    }

    private async void HandleLobbyHeartbeat()
    {
        try
        {
            if (hostLobby != null)
            {
                heartbeatTimer -= Time.deltaTime;
                if (heartbeatTimer < 0f)
                {
                    heartbeatTimer = heartbeatTimerMax;
                    await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
                }
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

    }
    
    private async void HandleLobbyPollForUpdates()
    {
        if (joinedLobby != null)
        {
            lobbyUpdateTimer -= Time.deltaTime;
            if (lobbyUpdateTimer < 0f)
            {
                lobbyUpdateTimer = lobbyUpdateTimerMax;
                Lobby lobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
                joinedLobby = lobby;

                if (joinedLobby.Data["StartGame"].Value != "0" && !_relayJoined)
                {
                    _relayJoined = true;
                    JoinRelay(joinedLobby.Data["StartGame"].Value);
                }
            }
        }
    }

    [ConsoleMethod("Authenticate", "Authenticates player by playerName")]
    public static async Task Authenticate(string newPlayerName)
    {
        await UnityServices.InitializeAsync();
        if (AuthenticationService.Instance.IsExpired || AuthenticationService.Instance.IsSignedIn)
            return;
        InitializationOptions initializationOptions = new InitializationOptions();
        initializationOptions.SetProfile(newPlayerName);
        playerName = newPlayerName;

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in" + AuthenticationService.Instance.PlayerId);
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private async void Start()
    {
    DontDestroyOnLoad(gameObject);
    }


    [ConsoleMethod("CreateRelay", "Creates a relay")]
    public static async Task<string> CreateRelay()
    {
        try
        {
            Debug.Log("Creating Relay");
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort) allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData);

            _relayJoined = true;
            SceneManager.sceneLoaded += (scene, mode) =>
            {
                NetworkManager.Singleton.StartHost();
            };
            SceneManager.LoadSceneAsync("Game");
            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
            return null;
        }
    }

    [ConsoleMethod("JoinRelay", "Joins a relay")]
    public static async void JoinRelay(string joinCode)
    {
        try
        {
            Debug.Log("Joining relay with " + joinCode);
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort) joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData);

            SceneManager.sceneLoaded += (scene, mode) =>
            {
                NetworkManager.Singleton.StartClient();
            };
            SceneManager.LoadSceneAsync("Game");
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }

    private static Player GetPlayer()
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                {"PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName)}
            }
        };
    }

    [ConsoleMethod("CreateLobby", "Creates a lobby")]
    public static async void CreateLobby(string lobbyName)
    {
        try
        {
            int maxPlayers = 4;
            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions {
                IsPrivate = false,
                Player = GetPlayer(),
                Data = new Dictionary<string, DataObject>
                {
                    {"StartGame", new DataObject(DataObject.VisibilityOptions.Member, "0")}
                }
            };
            
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createLobbyOptions);
            if (lobby == null)
                Debug.LogError("lobby is null!");
            hostLobby = lobby;
            joinedLobby = lobby;

            Debug.Log("Created Lobby! Lobby name: " + lobby.Name 
                                                    + " | Max players: " + lobby.MaxPlayers
                                                    + " | Lobby Id: " + lobby.Id
                                                    + " | Lobby Code: " + lobby.LobbyCode);

            StartGame();
            
            PrintPlayers(hostLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    [ConsoleMethod("ListLobbies", "Lists all available lobbies")]
    public static async void ListLobbies()
    {
        try
        {
            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync();

            Debug.Log("Lobbies found: " + queryResponse.Results.Count);
            foreach (Lobby lobby in queryResponse.Results)
            {
                
                Debug.Log(lobby.Name + " " + lobby.MaxPlayers);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    [ConsoleMethod("JoinLobbyById", "Joins a lobby through id")]
    public static async void JoinLobbyById(string lobbyId)
    {
        try
        {
            JoinLobbyByIdOptions joinLobbyByCodeOptions = new JoinLobbyByIdOptions
            {
                Player = GetPlayer()
            };
            Lobby lobby = await Lobbies.Instance.JoinLobbyByIdAsync(lobbyId, joinLobbyByCodeOptions);
            joinedLobby = lobby;
            Debug.Log("Joined Lobby with id. LobbyId: " + lobbyId);
            PrintPlayers(lobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    
    [ConsoleMethod("JoinLobbyByName", "Joins a lobby through lobby name")]
    public static async void JoinLobbyByName(string lobbyName)
    {
        try
        {
            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync();
            Debug.Log("Joining by name... Lobbies found: " + queryResponse.Results.Count);
            string lobbyId = null;
            foreach (Lobby lobby in queryResponse.Results)
            {
                if (lobby.Name == lobbyName)
                {
                    lobbyId = lobby.Id;
                    break;
                }
                    
            }

            if (lobbyId != null)
            {
                JoinLobbyById(lobbyId);
            }
            else
            {
                Debug.LogWarning("lobby with this name not found!");
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    [ConsoleMethod("PrintPlayers", "Prints players in current lobby")]
    public static void PrintPlayers()
    {
        PrintPlayers(joinedLobby);
    }
    
    private static void PrintPlayers(Lobby lobby)
    {
        Debug.Log("Players in Lobby: " + lobby.Name);
        Debug.Log("Players count: " + lobby.Players.Count);
        foreach (Player player in lobby.Players)
        {
            Debug.Log("Player Id: " + player.Id + " | PlayerName: " + player.Data["PlayerName"].Value);
        }
    }

    [ConsoleMethod("LeaveLobby", "Make player leave lobby")]
    public static async void LeaveLobby()
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    [ConsoleMethod("StartGame", "Start the game")]
    public static async void StartGame()
    {
        //TODO: StartGame on 2 players
        try
        {
            if (hostLobby != null)
            {
                Debug.Log("StartGame");

                string relayCode = await CreateRelay();
                _relayJoined = true;
                Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        {"StartGame", new DataObject(DataObject.VisibilityOptions.Member, relayCode)}
                    }
                });

                joinedLobby = lobby;
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
}