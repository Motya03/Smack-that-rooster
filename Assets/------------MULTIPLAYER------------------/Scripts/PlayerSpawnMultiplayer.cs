using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class PlayerSpawnMultiplayer : NetworkBehaviour
{
    [SerializeField] private Transform[] spawnPoints;
    private List<int> usedIndexes = new List<int>();

    public static PlayerSpawnMultiplayer Instance { get; private set; }
    public static List<GameObject> joinedPlayers = new List<GameObject>();

    [SerializeField] private Material[] outlineMaterials;
    [SerializeField] private GameObject playerPrefab;

    private Dictionary<ulong, GameObject> playerObjects = new Dictionary<ulong, GameObject>();
    private Dictionary<ulong, int> playerIndices = new Dictionary<ulong, int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            // Spawn host player immediately
            if (NetworkManager.Singleton.IsHost)
            {
                StartCoroutine(SpawnPlayerWithDelay(NetworkManager.Singleton.LocalClientId));
            }
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (IsServer)
        {
            Debug.Log($"Client connected: {clientId}");
            StartCoroutine(SpawnPlayerWithDelay(clientId));
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (IsServer)
        {
            Debug.Log($"Client disconnected: {clientId}");
            RemovePlayer(clientId);

            // Check if game should end
            if (GameManagerMultiplayer.Instance != null)
            {
                GameManagerMultiplayer.Instance.CheckRemainingPlayersServerRpc();
            }
        }
    }

    private IEnumerator SpawnPlayerWithDelay(ulong clientId)
    {
        // Small delay to ensure everything is initialized
        yield return new WaitForSeconds(0.1f);

        if (IsServer)
        {
            SpawnPlayer(clientId);
        }
    }

    private void SpawnPlayer(ulong clientId)
    {
        if (!IsServer) return;

        // Get unique spawn point
        int spawnIndex = GetUniqueRandomIndex();
        Vector3 spawnPosition = spawnPoints[spawnIndex].position;
        Quaternion spawnRotation = spawnPoints[spawnIndex].rotation;

        // Spawn player as network object
        GameObject playerObj = Instantiate(playerPrefab, spawnPosition, spawnRotation);
        NetworkObject networkObject = playerObj.GetComponent<NetworkObject>();

        if (networkObject == null)
        {
            Debug.LogError("Player prefab doesn't have NetworkObject component!");
            Destroy(playerObj);
            return;
        }

        networkObject.SpawnAsPlayerObject(clientId, true);

        // Store references
        playerObjects[clientId] = playerObj;
        playerIndices[clientId] = spawnIndex;

        if (!joinedPlayers.Contains(playerObj))
            joinedPlayers.Add(playerObj);

        // Initialize player components
        InitializePlayerComponents(playerObj, clientId);

        // Apply outline material
        ApplyOutlineMaterial(playerObj, clientId);

        // Notify client
        OnPlayerSpawnedClientRpc(clientId, spawnPosition, spawnRotation);

        Debug.Log($"Player {clientId} spawned at spawn point {spawnIndex}");
    }

    private void InitializePlayerComponents(GameObject playerObj, ulong clientId)
    {
        // Initialize PlayerMovMultiplayer
        PlayerMovMultiplayer playerScript = playerObj.GetComponent<PlayerMovMultiplayer>();
        if (playerScript != null)
        {
            playerScript.networkVidas.Value = 3;
            playerScript.networkLives.Value = 1;
            playerScript.networkIsDefinitivelyDead.Value = false;

            // Assign health system if needed
            HealthSystemMulti healthSystem = playerObj.GetComponentInChildren<HealthSystemMulti>();
            if (healthSystem != null)
            {
                playerScript.uiHealth = healthSystem;
            }
        }

        // Notify lobby manager
        NotifyLobbyManager(clientId);

        // Initially disable player control (can be enabled by GameManager)
        TogglePlayerControl(playerObj, false);
    }

    private void NotifyLobbyManager(ulong clientId)
    {
        try
        {
            LobbyJoinManagerMultiplayer lobbyManager = FindAnyObjectByType<LobbyJoinManagerMultiplayer>();
            if (lobbyManager != null)
            {
                // Verificar si el método OnPlayerSpawned existe
                System.Reflection.MethodInfo method = lobbyManager.GetType().GetMethod("OnPlayerSpawned");
                if (method != null)
                {
                    method.Invoke(lobbyManager, new object[] { clientId });
                }
                else
                {
                    // Alternativa: usar OnPlayerJoinedVisual si existe
                    System.Reflection.MethodInfo method2 = lobbyManager.GetType().GetMethod("OnPlayerJoinedVisual");
                    if (method2 != null)
                    {
                        // Buscar qué índice de slot usar
                        int slotIndex = GetSlotIndexForClient(clientId);
                        method2.Invoke(lobbyManager, new object[] { slotIndex });
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Could not notify lobby manager: {e.Message}");
        }
    }

    private int GetSlotIndexForClient(ulong clientId)
    {
        // Buscar el índice basado en la conexión
        int index = 0;
        foreach (var kvp in playerObjects)
        {
            if (kvp.Key == clientId)
                return index;
            index++;
        }
        return 0;
    }

    [ClientRpc]
    private void OnPlayerSpawnedClientRpc(ulong clientId, Vector3 position, Quaternion rotation)
    {
        // If this is our player, we might want to do some client-side setup
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log($"My player has been spawned at {position}");

            // Enable local player control if game has started
            StartCoroutine(CheckGameStartAndEnableControl());
        }
    }

    private IEnumerator CheckGameStartAndEnableControl()
    {
        // Wait a bit and check if game has started
        yield return new WaitForSeconds(0.5f);

        GameObject myPlayer = GetLocalPlayer();
        if (myPlayer != null)
        {
            // Verificar si el juego ha comenzado
            if (IsGameActive())
            {
                TogglePlayerControl(myPlayer, true);
            }
        }
    }

    private bool IsGameActive()
    {
        // Primero verificar si el juego ha comenzado en el GameManager
        if (GameManagerMultiplayer.Instance != null)
        {
            // Acceder al NetworkVariable gameStarted
            var gameStartedField = typeof(GameManagerMultiplayer).GetField("gameStarted",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (gameStartedField != null)
            {
                var gameStartedValue = gameStartedField.GetValue(GameManagerMultiplayer.Instance) as NetworkVariable<bool>;
                if (gameStartedValue != null && gameStartedValue.Value)
                {
                    return true;
                }
            }
        }

        // También verificar el LobbyJoinManager
        if (LobbyJoinManagerMultiplayer.Instance != null)
        {
            var gameStartedProp = typeof(LobbyJoinManagerMultiplayer).GetProperty("IsGameStarted");
            if (gameStartedProp != null)
            {
                return (bool)gameStartedProp.GetValue(LobbyJoinManagerMultiplayer.Instance);
            }
        }

        // Si no se puede determinar, asumir que el juego ha comenzado
        return true;
    }

    private int GetUniqueRandomIndex()
    {
        if (usedIndexes.Count >= spawnPoints.Length)
            usedIndexes.Clear();

        int randomIndex;
        int attempts = 0;
        do
        {
            randomIndex = Random.Range(0, spawnPoints.Length);
            attempts++;

            if (attempts > spawnPoints.Length * 2)
            {
                Debug.LogWarning("Could not find unique spawn point, reusing");
                usedIndexes.Clear();
                break;
            }
        }
        while (usedIndexes.Contains(randomIndex));

        usedIndexes.Add(randomIndex);
        return randomIndex;
    }

    private void ApplyOutlineMaterial(GameObject playerObj, ulong clientId)
    {
        if (outlineMaterials == null || outlineMaterials.Length == 0)
        {
            Debug.LogWarning("No outline materials assigned!");
            return;
        }

        // Calculate material index based on client ID
        int materialIndex = (int)clientId % outlineMaterials.Length;

        Transform meshChild = playerObj.transform.Find("Sphere.001");
        if (meshChild != null)
        {
            Renderer renderer = meshChild.GetComponent<Renderer>();
            if (renderer != null && outlineMaterials[materialIndex] != null)
            {
                List<Material> mats = new List<Material>(renderer.sharedMaterials);

                if (mats.Count == 1)
                    mats.Add(outlineMaterials[materialIndex]);
                else if (mats.Count > 1)
                    mats[1] = outlineMaterials[materialIndex];

                renderer.materials = mats.ToArray();
            }
        }
    }

    public void RemovePlayer(ulong clientId)
    {
        if (playerObjects.ContainsKey(clientId))
        {
            GameObject playerToRemove = playerObjects[clientId];

            // Remove from lists
            if (joinedPlayers.Contains(playerToRemove))
                joinedPlayers.Remove(playerToRemove);

            // Free up spawn point
            if (playerIndices.ContainsKey(clientId))
            {
                int spawnIndex = playerIndices[clientId];
                usedIndexes.Remove(spawnIndex);
                playerIndices.Remove(clientId);
            }

            playerObjects.Remove(clientId);

            // Destroy the network object
            NetworkObject netObj = playerToRemove.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Despawn();
            }
            else
            {
                Destroy(playerToRemove);
            }
        }
    }

    // Static method to toggle player control (compatible with existing code)
    public static void TogglePlayerControl(GameObject playerObj, bool state)
    {
        if (!playerObj) return;

        // Enable/disable PlayerMovMultiplayer script
        PlayerMovMultiplayer playerScript = playerObj.GetComponent<PlayerMovMultiplayer>();
        if (playerScript != null)
        {
            playerScript.enabled = state;
        }

        // Enable/disable InputSystem components
        PlayerInput playerInput = playerObj.GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            playerInput.enabled = state;
        }

        // Enable/disable CharacterController if needed
        CharacterController characterController = playerObj.GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = state;
        }

        Debug.Log($"Player control toggled to: {state}");
    }

    // Method to get local player
    public GameObject GetLocalPlayer()
    {
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        if (playerObjects.ContainsKey(localClientId))
        {
            return playerObjects[localClientId];
        }
        return null;
    }

    // Method to get all active players
    public List<GameObject> GetAllActivePlayers()
    {
        List<GameObject> activePlayers = new List<GameObject>();

        foreach (var playerObj in joinedPlayers)
        {
            if (playerObj != null)
            {
                PlayerMovMultiplayer playerScript = playerObj.GetComponent<PlayerMovMultiplayer>();
                if (playerScript != null && !playerScript.networkIsDefinitivelyDead.Value)
                {
                    activePlayers.Add(playerObj);
                }
            }
        }

        return activePlayers;
    }

    // Method to respawn a dead player
    [ServerRpc(RequireOwnership = false)]
    public void RespawnPlayerServerRpc(ulong clientId)
    {
        if (!playerObjects.ContainsKey(clientId)) return;

        GameObject playerObj = playerObjects[clientId];
        PlayerMovMultiplayer playerScript = playerObj.GetComponent<PlayerMovMultiplayer>();

        if (playerScript == null) return;

        // Get new spawn point
        int spawnIndex = GetUniqueRandomIndex();
        Vector3 spawnPosition = spawnPoints[spawnIndex].position;
        Quaternion spawnRotation = spawnPoints[spawnIndex].rotation;

        // Move player
        CharacterController cc = playerObj.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            playerObj.transform.position = spawnPosition;
            playerObj.transform.rotation = spawnRotation;
            cc.enabled = true;
        }
        else
        {
            playerObj.transform.position = spawnPosition;
            playerObj.transform.rotation = spawnRotation;
        }

        // Reset player state
        playerScript.networkVidas.Value = 3;
        playerScript.networkIsDefinitivelyDead.Value = false;
        playerScript.SetStateServerRpc(PlayerMovMultiplayer.States.Idle);

        // Update spawn index
        if (playerIndices.ContainsKey(clientId))
        {
            usedIndexes.Remove(playerIndices[clientId]);
        }
        playerIndices[clientId] = spawnIndex;
        usedIndexes.Add(spawnIndex);

        Debug.Log($"Player {clientId} respawned at spawn point {spawnIndex}");
    }

    // Clean up
    public override void OnDestroy()
    {
        if (IsServer)
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }

        // Clear static lists
        joinedPlayers.Clear();

        base.OnDestroy();
    }

    // Public getters
    public int GetPlayerCount() => joinedPlayers.Count;

    public int GetActivePlayerCount()
    {
        int count = 0;
        foreach (var playerObj in joinedPlayers)
        {
            if (playerObj != null)
            {
                PlayerMovMultiplayer playerScript = playerObj.GetComponent<PlayerMovMultiplayer>();
                if (playerScript != null && !playerScript.networkIsDefinitivelyDead.Value)
                {
                    count++;
                }
            }
        }
        return count;
    }

    public GameObject GetPlayerByClientId(ulong clientId)
    {
        return playerObjects.ContainsKey(clientId) ? playerObjects[clientId] : null;
    }

    public ulong GetClientIdByPlayer(GameObject playerObj)
    {
        foreach (var kvp in playerObjects)
        {
            if (kvp.Value == playerObj)
            {
                return kvp.Key;
            }
        }
        return 999; // Invalid ID
    }

    // Método para remover jugador usando GameObject
    public void RemovePlayer(GameObject playerObj)
    {
        if (playerObj == null) return;

        ulong clientId = GetClientIdByPlayer(playerObj);
        if (clientId != 999)
        {
            RemovePlayer(clientId);
        }
    }

    // Método para limpiar jugadores nulos
    public void RemoveNullPlayers()
    {
        joinedPlayers.RemoveAll(p => p == null);
    }
}