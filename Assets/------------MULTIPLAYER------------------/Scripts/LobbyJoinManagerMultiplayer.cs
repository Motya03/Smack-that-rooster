using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;
using System.Collections.Generic;

public class LobbyJoinManagerMultiplayer : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject[] playerSlots;
    [SerializeField] private Button startButton;
    [SerializeField] private GameObject lobbyCanvas;
    [SerializeField] private GameObject gameplayCanvas;
    [SerializeField] private TMP_Text joinCodeText;
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private TMP_Text statusText;

    [Header("Gameplay UI")]
    [SerializeField] private GameObject[] healthBars;

    [Header("Player Colors")]
    [SerializeField] private Color[] playerColors;

    private Netmanager netManager;
    private bool gameStarted = false;
    private string currentJoinCode = "";

    // Diccionario para trackear qué cliente está en qué slot
    private Dictionary<ulong, int> clientSlotAssignments = new Dictionary<ulong, int>();

    public static LobbyJoinManagerMultiplayer Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // Initially hide everything
        startButton.gameObject.SetActive(false);
        gameplayCanvas.SetActive(false);

        if (statusText != null)
            statusText.text = "";

        foreach (var slot in playerSlots)
            slot.SetActive(false);

        foreach (var bar in healthBars)
            bar.SetActive(false);

        // Get Netmanager reference usando el método no obsoleto
        netManager = FindFirstObjectByType<Netmanager>();

        if (netManager == null)
        {
            Debug.LogError("❌ No se encontró Netmanager.");
        }
    }

    private void Start()
    {
        // Setup button listeners
        if (hostButton != null)
            hostButton.onClick.AddListener(StartAsHost);

        if (clientButton != null)
            clientButton.onClick.AddListener(StartAsClient);

        startButton.onClick.AddListener(StartGame);
    }

    private async void StartAsHost()
    {
        if (netManager == null) return;

        if (hostButton != null)
        {
            hostButton.interactable = false;
            hostButton.gameObject.SetActive(false);
        }

        if (clientButton != null)
        {
            clientButton.interactable = false;
            clientButton.gameObject.SetActive(false);
        }

        if (statusText != null)
            statusText.text = "Creating game...";

        // Start host using Netmanager
        string joinCode = await netManager.StartHostWithRelay(4, "udp");

        if (!string.IsNullOrEmpty(joinCode))
        {
            currentJoinCode = joinCode;

            if (joinCodeText != null)
            {
                joinCodeText.text = $"Join Code: {joinCode}";
                joinCodeText.gameObject.SetActive(true);
            }

            // Host se asigna a slot 0
            if (statusText != null)
                statusText.text = "Waiting for players...";
        }
        else
        {
            if (statusText != null)
                statusText.text = "Failed to create game!";

            // Reactivar botones si falla
            if (hostButton != null)
            {
                hostButton.gameObject.SetActive(true);
                hostButton.interactable = true;
            }
            if (clientButton != null)
            {
                clientButton.gameObject.SetActive(true);
                clientButton.interactable = true;
            }
        }
    }

    private async void StartAsClient()
    {
        if (netManager == null || joinCodeInput == null) return;

        if (string.IsNullOrEmpty(joinCodeInput.text))
        {
            if (statusText != null)
                statusText.text = "Please enter a join code!";
            return;
        }

        if (hostButton != null)
        {
            hostButton.interactable = false;
            hostButton.gameObject.SetActive(false);
        }

        if (clientButton != null)
        {
            clientButton.interactable = false;
            clientButton.gameObject.SetActive(false);
        }

        joinCodeInput.interactable = false;

        if (statusText != null)
            statusText.text = $"Joining...";

        bool success = await netManager.StartClientWithRelay(joinCodeInput.text, "udp");

        if (success)
        {
            currentJoinCode = joinCodeInput.text;

            if (joinCodeText != null)
            {
                joinCodeText.text = $"Joined!";
                joinCodeText.gameObject.SetActive(true);
            }

            if (statusText != null)
                statusText.text = "Waiting for host...";
        }
        else
        {
            if (statusText != null)
                statusText.text = "Failed to join!";

            // Reactivar botones si falla
            if (hostButton != null)
            {
                hostButton.gameObject.SetActive(true);
                hostButton.interactable = true;
            }
            if (clientButton != null)
            {
                clientButton.gameObject.SetActive(true);
                clientButton.interactable = true;
            }
            joinCodeInput.interactable = true;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Debug.Log($"LobbyJoinManager spawned. IsServer: {IsServer}, IsClient: {IsClient}");

        if (IsServer)
        {
            // Server sets up callbacks
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            // Assign slot to host
            AssignSlotToClient(NetworkManager.Singleton.LocalClientId);
        }

        // All clients should update UI
        UpdateLobbyUI();
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        Debug.Log($"Player {clientId} connected - Total: {NetworkManager.Singleton.ConnectedClients.Count}");

        // Assign slot to new player
        AssignSlotToClient(clientId);

        // Play sound for all clients
        PlaySoundClientRpc(SoundType.CharEnter);

        // Show start button if we have at least 2 players
        if (NetworkManager.Singleton.ConnectedClients.Count >= 2)
        {
            startButton.gameObject.SetActive(true);
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

        Debug.Log($"Player {clientId} disconnected - Total: {NetworkManager.Singleton.ConnectedClients.Count}");

        // Free the slot
        if (clientSlotAssignments.ContainsKey(clientId))
        {
            int slotIndex = clientSlotAssignments[clientId];
            clientSlotAssignments.Remove(clientId);

            // Update UI for all clients
            UpdateSlotVisualsClientRpc();
        }

        // Hide start button if less than 2 players
        if (NetworkManager.Singleton.ConnectedClients.Count < 2)
        {
            startButton.gameObject.SetActive(false);
        }
    }

    private void AssignSlotToClient(ulong clientId)
    {
        if (!IsServer) return;

        // Find next available slot
        for (int i = 0; i < playerSlots.Length; i++)
        {
            if (!clientSlotAssignments.ContainsValue(i))
            {
                clientSlotAssignments[clientId] = i;

                // Update UI for all clients
                UpdateSlotVisualsClientRpc();

                Debug.Log($"Assigned slot {i} to client {clientId}");
                return;
            }
        }

        Debug.LogWarning($"No available slots for client {clientId}");
    }

    [ClientRpc]
    private void UpdateSlotVisualsClientRpc()
    {
        // Clear all slots first
        foreach (var slot in playerSlots)
        {
            slot.SetActive(false);
        }

        // Activate slots based on assignments (client-side approximation)
        // Note: Only server knows the exact assignments, so we approximate
        // In a full implementation, the server would send exact assignments

        if (IsServer)
        {
            // Server knows exact assignments
            foreach (var assignment in clientSlotAssignments)
            {
                int slotIndex = assignment.Value;
                if (slotIndex < playerSlots.Length)
                {
                    playerSlots[slotIndex].SetActive(true);

                    // Apply color
                    Image slotImage = playerSlots[slotIndex].GetComponent<Image>();
                    if (slotImage != null && slotIndex < playerColors.Length)
                    {
                        slotImage.color = playerColors[slotIndex];
                    }
                }
            }
        }
        else
        {
            // Clients approximate based on connected client count
            int connectedCount = NetworkManager.Singleton.ConnectedClients.Count;
            for (int i = 0; i < connectedCount && i < playerSlots.Length; i++)
            {
                playerSlots[i].SetActive(true);

                // Apply color
                Image slotImage = playerSlots[i].GetComponent<Image>();
                if (slotImage != null && i < playerColors.Length)
                {
                    slotImage.color = playerColors[i];
                }
            }
        }
    }

    [ClientRpc]
    private void PlaySoundClientRpc(SoundType soundType)
    {
        SoundManager.PlaySound(soundType);
    }

    public void OnPlayerJoinedVisual(int index)
    {
        if (index < playerSlots.Length)
        {
            SoundManager.PlaySound(SoundType.CharEnter);
            playerSlots[index].SetActive(true);

            if (index >= 1)
                startButton.gameObject.SetActive(true);
        }
    }

    private void UpdateLobbyUI()
    {
        // Update slot visuals
        UpdateSlotVisualsClientRpc();

        // Update start button visibility
        if (IsServer)
        {
            startButton.gameObject.SetActive(NetworkManager.Singleton.ConnectedClients.Count >= 2);
        }
    }

    public void StartGame()
    {
        if (!IsServer)
        {
            Debug.LogWarning("Only host can start the game!");
            return;
        }

        SoundManager.PlaySound(SoundType.SelectionButtonChar);
        Debug.Log("🎮 Starting multiplayer game...");

        // Start game on server
        StartGameServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartGameServerRpc()
    {
        if (gameStarted) return;

        gameStarted = true;
        Debug.Log("Server starting game...");

        // Switch to gameplay for all clients
        StartGameClientRpc();

        // Initialize gameplay on server
        InitializeGameplay();
    }

    [ClientRpc]
    private void StartGameClientRpc()
    {
        Debug.Log("Client received StartGameClientRpc");

        lobbyCanvas.SetActive(false);
        gameplayCanvas.SetActive(true);

        // Hide network UI elements
        if (hostButton != null) hostButton.gameObject.SetActive(false);
        if (clientButton != null) clientButton.gameObject.SetActive(false);
        startButton.gameObject.SetActive(false);
        if (joinCodeText != null) joinCodeText.gameObject.SetActive(false);
        if (joinCodeInput != null) joinCodeInput.gameObject.SetActive(false);
        if (statusText != null) statusText.gameObject.SetActive(false);

        // Activate health bars
        ActivateHealthBars();
    }

    private void InitializeGameplay()
    {
        if (!IsServer) return;

        Debug.Log("Initializing gameplay on server...");

        // ✅ ELIMINAR jugadores destruidos de la lista
        if (PlayerSpawnMultiplayer.Instance != null)
        {
            PlayerSpawnMultiplayer.Instance.RemoveNullPlayers();
        }

        // ✅ ACTIVAR control para jugadores existentes
        foreach (var playerObj in PlayerSpawnMultiplayer.joinedPlayers)
        {
            if (playerObj == null) continue;

            // Enable player control
            PlayerSpawnMultiplayer.TogglePlayerControl(playerObj, true);

            // Enable player's NetworkTransform (client authoritative)
            ClientNetwork clientNetwork = playerObj.GetComponent<ClientNetwork>();
            if (clientNetwork != null)
            {
                clientNetwork.enabled = true;
            }
        }

        int playerCount = PlayerSpawnMultiplayer.joinedPlayers.Count;
        Debug.Log($"Total players: {playerCount}");

        // ✅ ACTIVAR SOLO las barras necesarias y asignar salud
        AssignHealthBarsToPlayers();

        Debug.Log($"❤️ Activated {playerCount} health bars.");

        // ✅ ACTIVAR GAME MANAGER
        var gm = FindFirstObjectByType<GameManagerMultiplayer>();
        if (gm != null)
        {
            gm.ActivateGameServerRpc();
        }
        else
        {
            Debug.LogWarning("GameManagerMultiplayer not found!");
        }
    }

    private void AssignHealthBarsToPlayers()
    {
        if (!IsServer) return;

        int playerCount = PlayerSpawnMultiplayer.joinedPlayers.Count;

        // Primero desactivar todas las health bars
        foreach (var healthBar in healthBars)
        {
            if (healthBar != null)
                healthBar.SetActive(false);
        }

        // Luego activar solo las necesarias
        for (int i = 0; i < playerCount && i < healthBars.Length; i++)
        {
            var playerObj = PlayerSpawnMultiplayer.joinedPlayers[i];
            if (playerObj == null) continue;

            var player = playerObj.GetComponent<PlayerMovMultiplayer>();
            if (player == null) continue;

            var uiHealth = healthBars[i].GetComponent<HealthSystemMulti>();
            if (uiHealth != null)
            {
                healthBars[i].SetActive(true);
                player.uiHealth = uiHealth;
                uiHealth.ResetHealthServerRpc();

                Debug.Log($"Assigned health bar {i} to player {player.OwnerClientId}");
            }
        }
    }

    private void ActivateHealthBars()
    {
        // Simple activation based on joined players count
        int playerCount = PlayerSpawnMultiplayer.joinedPlayers.Count;

        for (int i = 0; i < healthBars.Length; i++)
        {
            if (healthBars[i] != null)
            {
                healthBars[i].SetActive(i < playerCount);
            }
        }
    }

    // Clean up
    public override void OnDestroy()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        base.OnDestroy();
    }

    // For leaving the game
    public void LeaveGame()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // Return to lobby
        ReturnToLobby();
    }

    private void ReturnToLobby()
    {
        lobbyCanvas.SetActive(true);
        gameplayCanvas.SetActive(false);

        // Reset UI elements
        startButton.gameObject.SetActive(false);

        if (hostButton != null)
        {
            hostButton.gameObject.SetActive(true);
            hostButton.interactable = true;
        }
        if (clientButton != null)
        {
            clientButton.gameObject.SetActive(true);
            clientButton.interactable = true;
        }
        if (joinCodeText != null)
        {
            joinCodeText.gameObject.SetActive(true);
            joinCodeText.text = "";
        }
        if (joinCodeInput != null)
        {
            joinCodeInput.gameObject.SetActive(true);
            joinCodeInput.interactable = true;
            joinCodeInput.text = "";
        }
        if (statusText != null)
        {
            statusText.gameObject.SetActive(true);
            statusText.text = "Disconnected";
        }

        // Reset slots
        foreach (var slot in playerSlots)
            slot.SetActive(false);

        // Reset health bars
        foreach (var bar in healthBars)
            if (bar != null) bar.SetActive(false);

        // Clear dictionaries
        clientSlotAssignments.Clear();

        currentJoinCode = "";
        gameStarted = false;
    }

    // Helper method
    public bool IsGameStarted()
    {
        return gameStarted;
    }

    // Update UI when needed
    private void Update()
    {
        // Optional: Update lobby UI periodically
        if (IsClient && !gameStarted && lobbyCanvas.activeSelf)
        {
            UpdateLobbyUI();
        }
    }
}