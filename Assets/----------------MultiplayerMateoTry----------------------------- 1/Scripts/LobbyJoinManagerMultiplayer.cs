using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
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
        base.OnNetworkSpawn();

        isHost = IsHost;

        // Solo el host ve el botón de inicio
        if (!isHost)
        {
            startButton.gameObject.SetActive(false);
        }

        playerCount.OnValueChanged += OnPlayerCountChanged;

        // Registrar jugador en el servidor (host y clientes)
        StartCoroutine(WaitForSecondsTest());
    }

    [ServerRpc(RequireOwnership = false)]
    private void RegisterPlayerServerRpc(ServerRpcParams rpcParams = default)
    {
        playerCount.Value++;

        // Notificar a todos los clientes sobre el nuevo jugador
        int newPlayerIndex = playerCount.Value - 1;
        OnPlayerJoinedClientRpc(newPlayerIndex, rpcParams.Receive.SenderClientId);
    }

    [ClientRpc]
    private void OnPlayerJoinedClientRpc(int playerIndex, ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            localPlayerIndex = playerIndex;
        }

        OnPlayerJoinedVisual(playerIndex);
    }

    public void OnPlayerJoinedVisual(int index)
    {
        if (index < playerSlots.Length)
        {
            SoundManager.PlaySound(SoundType.CharEnter);
            playerSlots[index].SetActive(true);

            Debug.Log($"🎮 Jugador {index + 1} se unió");

            // Solo el host puede iniciar y solo si hay al menos 2 jugadores
            if (isHost && playerCount.Value >= 2)
            {
                startButton.gameObject.SetActive(true);
            }
        }
    }

    private IEnumerator WaitForSecondsTest()
    {
        yield return new WaitForSeconds(3);
        RegisterPlayerServerRpc();
    }

    public void StartGame()
    {
        // Solo el host puede iniciar la partida
        if (!isHost) return;

        StartGameServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartGameServerRpc(ServerRpcParams rpcParams = default)
    {
        // Solo permitir al host iniciar el juego
        if (!IsHost) return;

        StartGameClientRpc();
    }

    [ClientRpc]
    private void StartGameClientRpc()
    {
        SoundManager.PlaySound(SoundType.SelectionButtonChar);
        Debug.Log("🎮 Empieza la partida");

        lobbyCanvas.SetActive(false);
        gameplayCanvas.SetActive(true);

        // ✅ REACTIVAR HUD del GameManager si estaba apagado
        var gm = FindFirstObjectByType<GameManageMultiplayer>();
        if (gm != null && gm.canvasLocal != null)
            gm.canvasLocal.SetActive(true);

        // ✅ ELIMINAR jugadores destruidos de la lista
        PlayerSpawnMultiplayer.joinedPlayers.RemoveAll(p => p == null);

        // ✅ ACTIVAR control para jugadores existentes
        foreach (var playerObj in PlayerSpawnMultiplayer.joinedPlayers)
        {
            if (playerObj == null) continue;
            PlayerSpawnMultiplayer.TogglePlayerControl(playerObj, true);
        }

        int playerCountLocal = PlayerSpawnMultiplayer.joinedPlayers.Count;

        // ✅ ACTIVAR SOLO las barras necesarias y asignar salud
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

                player.uiHealth = uiHealth;
                uiHealth.ResetHealth();
            }
        }

        Debug.Log($"❤️ Activadas {playerCountLocal} barras de vida.");

        // ✅ ACTIVAR GAME MANAGER
        gm?.ActivateGame();
    }

    private void OnPlayerCountChanged(int oldValue, int newValue)
    {
        Debug.Log($"👥 Jugadores conectados: {newValue}");
    }

    // (Opcional) si algún día quieres que otros scripts pidan una barra concreta:
    public GameObject GetHealthBar(int index)
    {
        if (index < 0 || index >= healthBars.Length) return null;
        return healthBars[index];
    }
}
