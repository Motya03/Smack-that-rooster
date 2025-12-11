using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyJoinManagerMultiplayer : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject[] playerSlots;
    [SerializeField] private Button startButton;
    [SerializeField] private GameObject lobbyCanvas;
    [SerializeField] private GameObject gameplayCanvas;

    [Header("Gameplay UI")]
    [SerializeField] private GameObject[] healthBars;

    private NetworkVariable<int> playerCount = new NetworkVariable<int>(0);
    private int localPlayerIndex = -1;
    private bool isHost = false;

    private void Awake()
    {
        Debug.Log("🔥 [Lobby] Awake ejecutado");

        lobbyCanvas.SetActive(true);
        gameplayCanvas.SetActive(false);
        startButton.gameObject.SetActive(false);

        foreach (var slot in playerSlots)
            slot.SetActive(false);

        foreach (var bar in healthBars)
            bar.SetActive(false);
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log("🔥 [Lobby] OnNetworkSpawn | IsHost = " + IsHost);
        isHost = IsHost;

        if (!isHost)
            startButton.gameObject.SetActive(false);

        StartCoroutine(RegisterLate());
    }

    private IEnumerator RegisterLate()
    {
        yield return new WaitForSeconds(1);
        RegisterPlayerServerRpc();
    }

    // ----------------------------------------
    // SERVER REGISTRA JUGADORES
    // ----------------------------------------
    [ServerRpc(RequireOwnership = false)]
    private void RegisterPlayerServerRpc(ServerRpcParams rpcParams = default)
    {
        playerCount.Value++;

        Debug.Log($"👥 [Lobby] Jugador registrado. Total = {playerCount.Value}");

        OnPlayerJoinedClientRpc(
            playerCount.Value - 1,
            rpcParams.Receive.SenderClientId
        );
    }

    // ----------------------------------------
    // CLIENTE RECIBE LA NOTIFICACIÓN
    // ----------------------------------------
    [ClientRpc]
    private void OnPlayerJoinedClientRpc(int playerIndex, ulong clientId)
    {
        Debug.Log($"📨 [Lobby] OnPlayerJoinedClientRpc ejecutado | index={playerIndex} | clientId={clientId}");

        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            localPlayerIndex = playerIndex;
            Debug.Log($"📌 [Lobby] Este cliente es el jugador {localPlayerIndex + 1}");
        }

        OnPlayerJoinedVisual(playerIndex);
    }

    // ----------------------------------------
    // ACTUALIZACIÓN VISUAL DEL LOBBY
    // ----------------------------------------
    public void OnPlayerJoinedVisual(int index)
    {
        Debug.Log("🎉 [Lobby] OnPlayerJoinedVisual → " + index);

        if (index < playerSlots.Length)
        {
            playerSlots[index].SetActive(true);
            SoundManager.PlaySound(SoundType.CharEnter);
        }

        if (isHost && playerCount.Value >= 2)
        {
            Debug.Log("🎮 [Lobby] Host puede iniciar, mostrando botón START");
            startButton.gameObject.SetActive(true);
        }
    }

    // ----------------------------------------
    // HOST INICIA LA PARTIDA
    // ----------------------------------------
    public void StartGame()
    {
        if (!isHost)
        {
            Debug.Log("⚠️ [Lobby] Cliente intentó iniciar la partida. Bloqueado.");
            return;
        }

        Debug.Log("🚀 [Lobby] HOST ha presionado START");

        StartGameServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartGameServerRpc()
    {
        Debug.Log("⚡ [Lobby] StartGameServerRpc ejecutado (HOST)");

        StartGameClientRpc();
    }

    // ----------------------------------------
    // TODOS LOS CLIENTES ENTRAN AL COMBATE
    // ----------------------------------------
    [ClientRpc]
    private void StartGameClientRpc()
    {
        Debug.Log("🎮 [Lobby] StartGameClientRpc ejecutado en TODOS");

        lobbyCanvas.SetActive(false);
        gameplayCanvas.SetActive(true);

        // --------------------------------------------
        // REACTIVAR CONTROL DE JUGADORES
        // --------------------------------------------
        PlayerSpawnMultiplayer.joinedPlayers.RemoveAll(p => p == null);

        foreach (var playerObj in PlayerSpawnMultiplayer.joinedPlayers)
        {
            if (playerObj == null) continue;
            PlayerSpawnMultiplayer.TogglePlayerControl(playerObj, true);
        }

        // --------------------------------------------
        // ACTIVAR BARRAS DE VIDA
        // --------------------------------------------
        int playerCountLocal = PlayerSpawnMultiplayer.joinedPlayers.Count;

        for (int i = 0; i < healthBars.Length; i++)
        {
            bool active = i < playerCountLocal;
            healthBars[i].SetActive(active);

            if (active)
            {
                var playerObj = PlayerSpawnMultiplayer.joinedPlayers[i];
                if (playerObj == null) continue;

                var player = playerObj.GetComponent<PlayerMovMultiplayer>();
                if (player == null) continue;

                var uiHealth = healthBars[i].GetComponent<HealthSystemMultiplayer>();
                if (uiHealth == null) continue;

                // Enlazar barra con jugador
                player.uiHealth = uiHealth;

                // Reset visual de salud
                uiHealth.ResetHealth();
            }
        }

        Debug.Log($"❤️ [Lobby] Barras de vida activadas: {playerCountLocal}");

        // --------------------------------------------
        // ACTIVAR GAME MANAGER
        // --------------------------------------------
        Debug.Log("🔎 [Lobby] Buscando GameManager…");

        var gm = FindFirstObjectByType<GameManageMultiplayer>();

        Debug.Log("Resultado GM = " + gm);

        if (gm == null)
        {
            Debug.LogError("❌❌❌ [Lobby] ERROR: GameManageMultiplayer NO existe en la escena");
            return;
        }

        Debug.Log("🔥 [Lobby] Llamando a gm.ActivateGame()");
        gm.ActivateGame();
    }
}
