using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class GameManagerMultiplayer : NetworkBehaviour
{
    [Header("Referencias")]
    [SerializeField] private TimerLocalMultiplayer timer;
    [SerializeField] private GameObject[] playerPopups;
    [SerializeField] private Text winnerText;
    [SerializeField] private GameObject canvasWinner;
    [SerializeField] private GameObject canvasLocal;
    [SerializeField] private GameObject cajaRota;

    [Header("Configuración")]
    [SerializeField] private float startGameDelay = 3f;
    [SerializeField] private int initialPlayerLives = 3;

    [Header("Estado del Juego")]
    private NetworkVariable<bool> gameStarted = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> gameEnded = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> isSuddenDeath = new NetworkVariable<bool>(false);
    private NetworkVariable<int> winnerIndex = new NetworkVariable<int>(-1);

    private bool clickerActive = false;
    private List<PlayerMovMultiplayer> players = new List<PlayerMovMultiplayer>();
    private bool playersInitialized = false;

    public static GameManagerMultiplayer Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            Debug.Log("GameManagerMultiplayer iniciado en servidor");

            // Buscar Timer automáticamente si no está asignado
            if (timer == null)
            {
                timer = FindAnyObjectByType<TimerLocalMultiplayer>();
            }

            // Asegurar que el timer NO inicie automáticamente
            if (timer != null)
            {
                timer.onFinished.AddListener(OnTimerFinished);
                Debug.Log("Timer configurado");
            }

            // Ocultar UI inicial
            if (canvasWinner != null)
                canvasWinner.SetActive(false);

            if (canvasLocal != null)
                canvasLocal.SetActive(false);

            foreach (var p in playerPopups)
                p.SetActive(false);

            // Esperar a que los jugadores se conecten
            StartCoroutine(WaitForPlayers());
        }

        // Todos los clientes se suscriben a cambios
        gameEnded.OnValueChanged += OnGameEndedChanged;
        winnerIndex.OnValueChanged += OnWinnerChanged;

        // Inicialmente ocultar UI de juego
        if (canvasLocal != null)
            canvasLocal.SetActive(false);
    }

    public override void OnNetworkDespawn()
    {
        gameEnded.OnValueChanged -= OnGameEndedChanged;
        winnerIndex.OnValueChanged -= OnWinnerChanged;
        base.OnNetworkDespawn();
    }

    private IEnumerator WaitForPlayers()
    {
        Debug.Log("Esperando a que los jugadores se conecten...");

        // Esperar hasta que haya al menos 2 jugadores conectados
        yield return new WaitUntil(() =>
            NetworkManager.Singleton != null &&
            NetworkManager.Singleton.ConnectedClients.Count >= 2);

        Debug.Log($"Jugadores conectados: {NetworkManager.Singleton.ConnectedClients.Count}");

        // Esperar un poco más para asegurar que todos los jugadores estén registrados
        yield return new WaitForSeconds(1f);

        Debug.Log($"Jugadores registrados en lista: {players.Count}");

        // Asegurar que al menos hay 2 jugadores registrados
        if (players.Count < 2)
        {
            Debug.Log("No hay suficientes jugadores registrados. Esperando...");
            yield return new WaitUntil(() => players.Count >= 2);
        }

        // Inicializar jugadores
        InitializePlayersForGame();

        // Mostrar cuenta regresiva
        StartCountdownClientRpc();

        yield return new WaitForSeconds(startGameDelay);

        // Iniciar el juego
        StartGame();
    }

    [ClientRpc]
    private void StartCountdownClientRpc()
    {
        Debug.Log("¡El juego comenzará en " + startGameDelay + " segundos!");
        // Aquí podrías mostrar una cuenta regresiva en UI
    }

    private void InitializePlayersForGame()
    {
        if (!IsServer) return;

        Debug.Log("Inicializando jugadores para nueva partida...");

        playersInitialized = false;

        foreach (var player in players)
        {
            if (player != null)
            {
                // Resetear completamente al jugador
                player.vidas = initialPlayerLives;
                player.lives = 1;
                player.isDefinitivelyDead = false;

                Debug.Log($"Jugador {player.OwnerClientId} inicializado con {player.vidas} vidas");
            }
        }

        playersInitialized = true;
        Debug.Log("Jugadores inicializados correctamente");
    }

    private void StartGame()
    {
        if (!IsServer) return;

        Debug.Log("¡Iniciando juego!");

        // Resetear estado del juego
        gameStarted.Value = true;
        gameEnded.Value = false;
        winnerIndex.Value = -1;
        isSuddenDeath.Value = false;

        // Mostrar UI del juego
        ShowGameUIClientRpc();

        // Iniciar timer
        if (timer != null)
        {
            timer.ResetTimer();
            Debug.Log("Timer iniciado con 60 segundos");
        }
        else
        {
            Debug.LogError("Timer no encontrado!");
        }

        // Habilitar controles de jugadores
        EnablePlayerControlsClientRpc();

        // Notificar a todos los clientes
        GameStartedAnnouncementClientRpc();
    }

    [ClientRpc]
    private void ShowGameUIClientRpc()
    {
        if (canvasLocal != null)
            canvasLocal.SetActive(true);

        if (canvasWinner != null)
            canvasWinner.SetActive(false);
    }

    [ClientRpc]
    private void EnablePlayerControlsClientRpc()
    {
        PlayerMovMultiplayer[] allPlayers = FindObjectsByType<PlayerMovMultiplayer>(FindObjectsSortMode.None);

        foreach (var player in allPlayers)
        {
            if (player != null)
            {
                player.enabled = true;
                PlayerInput input = player.GetComponent<PlayerInput>();
                if (input != null) input.enabled = true;
            }
        }
    }

    [ClientRpc]
    private void GameStartedAnnouncementClientRpc()
    {
        Debug.Log("¡El juego ha comenzado!");
    }

    private void Update()
    {
        if (!IsServer || !gameStarted.Value || gameEnded.Value || clickerActive)
            return;

        // Solo verificar jugadores si ya están inicializados
        if (playersInitialized)
        {
            CheckRemainingPlayers();
        }
    }

    // Método para registrar jugadores
    public void RegisterPlayer(PlayerMovMultiplayer player)
    {
        if (!players.Contains(player))
        {
            players.Add(player);
            Debug.Log($"Jugador registrado: {player.OwnerClientId}. Total: {players.Count}");

            // Inicializar al jugador inmediatamente
            if (IsServer)
            {
                player.vidas = initialPlayerLives;
                player.lives = 1;
                player.isDefinitivelyDead = false;

                Debug.Log($"Jugador {player.OwnerClientId} registrado con {player.vidas} vidas");
            }
        }
    }

    public void UnregisterPlayer(PlayerMovMultiplayer player)
    {
        if (players.Contains(player))
        {
            players.Remove(player);
            Debug.Log($"Jugador removido: {player.OwnerClientId}. Restantes: {players.Count}");
        }
    }

    private void CheckRemainingPlayers()
    {
        if (!IsServer || !playersInitialized) return;

        int alive = 0;
        ulong lastAliveId = 0;

        foreach (var player in players)
        {
            if (player != null && !player.isDefinitivelyDead && player.vidas > 0)
            {
                alive++;
                lastAliveId = player.OwnerClientId;
            }
        }

        Debug.Log($"Jugadores vivos: {alive} de {players.Count}");

        // Solo terminar el juego si hay más de 1 jugador registrado
        if (players.Count > 1 && alive == 1)
        {
            Debug.Log($"Solo queda 1 jugador vivo: {lastAliveId}");
            EndGameServerRpc(lastAliveId);
        }
        else if (players.Count > 0 && alive == 0)
        {
            Debug.Log("Empate - todos los jugadores murieron");
            // Terminar con empate (índice -1)
            EndGameServerRpc(players[0].OwnerClientId);
        }
    }
    // O si prefieres un método más específico para cuando un jugador muere:
    public void OnPlayerDied(PlayerMovMultiplayer deadPlayer)
    {
        if (!IsServer) return;

        Debug.Log($"Jugador {deadPlayer.OwnerClientId} ha muerto");

        // Contar jugadores vivos
        int aliveCount = 0;
        PlayerMovMultiplayer lastAlive = null;

        foreach (var player in players)
        {
            if (player != null && player != deadPlayer &&
                !player.isDefinitivelyDead && player.vidas > 0)
            {
                aliveCount++;
                lastAlive = player;
            }
        }

        if (aliveCount == 1 && lastAlive != null)
        {
            EndGameServerRpc(lastAlive.OwnerClientId);
        }
        else if (aliveCount == 0)
        {
            // Todos murieron - empate
            if (players.Count > 0)
                EndGameServerRpc(players[0].OwnerClientId);
        }
    }
    [ServerRpc(RequireOwnership = false)]
    private void EndGameServerRpc(ulong winnerId)
    {
        if (gameEnded.Value) return;

        Debug.Log($"Intentando terminar juego. Ganador ID: {winnerId}");

        gameEnded.Value = true;

        // Encontrar índice del ganador
        int index = -1;
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] != null && players[i].OwnerClientId == winnerId)
            {
                index = i;
                break;
            }
        }

        winnerIndex.Value = index;

        // Detener timer
        if (timer != null)
        {
            timer.StopTimer();
        }

        // Desactivar controles de jugadores
        DisablePlayerControlsClientRpc();

        Debug.Log($"🎮 Partida terminada. Ganador: Jugador {index + 1} (ID: {winnerId})");

        // Mostrar UI del ganador con delay
        StartCoroutine(ShowWinnerUICoroutine());
    }

    [ClientRpc]
    private void DisablePlayerControlsClientRpc()
    {
        PlayerMovMultiplayer[] allPlayers = FindObjectsByType<PlayerMovMultiplayer>(FindObjectsSortMode.None);

        foreach (var player in allPlayers)
        {
            if (player != null)
            {
                player.enabled = false;
                PlayerInput input = player.GetComponent<PlayerInput>();
                if (input != null) input.enabled = false;
            }
        }
    }

    private IEnumerator ShowWinnerUICoroutine()
    {
        yield return new WaitForSeconds(1f);
        ShowWinnerUIClientRpc(winnerIndex.Value);
    }

    [ClientRpc]
    private void ShowWinnerUIClientRpc(int winnerIdx)
    {
        Debug.Log($"Mostrando ganador en cliente: índice {winnerIdx}");

        // Ocultar HUD de juego
        if (canvasLocal != null)
            canvasLocal.SetActive(false);

        // Mostrar canvas del ganador
        if (canvasWinner != null)
            canvasWinner.SetActive(true);

        // Mostrar texto del ganador
        if (winnerText != null)
        {
            if (winnerIdx >= 0 && winnerIdx < players.Count)
                winnerText.text = $"Ganador: Jugador {winnerIdx + 1}";
            else
                winnerText.text = "¡Partida Terminada!";
        }

        // Mostrar popup del ganador si existe
        if (winnerIdx >= 0 && winnerIdx < playerPopups.Length && playerPopups[winnerIdx] != null)
        {
            playerPopups[winnerIdx].SetActive(true);
        }
    }

    private void OnGameEndedChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"GameEnded cambiado: {oldValue} -> {newValue}");
    }

    private void OnWinnerChanged(int oldValue, int newValue)
    {
        Debug.Log($"WinnerIndex cambiado: {oldValue} -> {newValue}");
    }

    private void OnTimerFinished()
    {
        if (!IsServer || gameEnded.Value || clickerActive) return;

        Debug.Log("Timer terminado - verificando estado del juego");

        int alive = CountAlivePlayers();

        Debug.Log($"Jugadores vivos al final del timer: {alive}");

        if (alive >= 2 && !isSuddenDeath.Value)
        {
            Debug.Log("Activando muerte súbita");
            ActivateSuddenDeath();
            return;
        }

        if (alive >= 2 && isSuddenDeath.Value)
        {
            Debug.Log("Muerte súbita - determinando ganador por salud");
            ulong winnerId = GetWinnerByHealth();
            EndGameServerRpc(winnerId);
            return;
        }

        if (alive == 1)
        {
            ulong winnerId = GetAlivePlayerId();
            EndGameServerRpc(winnerId);
        }
        else if (alive == 0)
        {
            Debug.Log("Empate - todos murieron durante muerte súbita");
            if (players.Count > 0)
                EndGameServerRpc(players[0].OwnerClientId);
        }
    }

    private int CountAlivePlayers()
    {
        int count = 0;
        foreach (var player in players)
        {
            if (player != null && !player.isDefinitivelyDead && player.vidas > 0)
                count++;
        }
        return count;
    }

    private void ActivateSuddenDeath()
    {
        isSuddenDeath.Value = true;
        Debug.Log("⚡ Muerte súbita activada");

        if (timer != null)
        {
            timer.StartSuddenDeath();
        }

        ActivateSuddenDeathClientRpc();
    }

    [ClientRpc]
    private void ActivateSuddenDeathClientRpc()
    {
        if (winnerText != null)
            winnerText.text = "Muerte Súbita!";
    }

    private ulong GetWinnerByHealth()
    {
        PlayerMovMultiplayer bestPlayer = null;
        int maxHealth = -1;

        foreach (var player in players)
        {
            if (player != null && !player.isDefinitivelyDead)
            {
                if (player.vidas > maxHealth)
                {
                    maxHealth = player.vidas;
                    bestPlayer = player;
                }
            }
        }

        return bestPlayer != null ? bestPlayer.OwnerClientId : 0;
    }

    private ulong GetAlivePlayerId()
    {
        foreach (var player in players)
        {
            if (player != null && !player.isDefinitivelyDead && player.vidas > 0)
                return player.OwnerClientId;
        }
        return 0;
    }

    public void SetClickerState(bool state) => clickerActive = state;

    // --- Métodos para la caja ---
    private bool throwCageBool = true;

    public void PrepareCage()
    {
        if (!IsServer) return;

        if (throwCageBool)
        {
            throwCageBool = false;
            StartCoroutine(ThrowCageCorutina());
        }
    }

    private IEnumerator ThrowCageCorutina()
    {
        yield return new WaitForSeconds(1f);

        if (players.Count > 0)
        {
            int randomIndex = Random.Range(0, players.Count);
            Transform target = players[randomIndex].transform;
            ThrowCageServerRpc(target.position);
        }
    }

    [ServerRpc]
    private void ThrowCageServerRpc(Vector3 targetPosition)
    {
        Vector3 spawnPos = targetPosition + Vector3.up * 10f;
        GameObject caja = Instantiate(cajaRota, spawnPos, Quaternion.identity);

        NetworkObject netObj = caja.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn(true);
        }

        Destroy(caja, 5f);
        Debug.Log($"🔥 Caja lanzada sobre jugador en posición: {targetPosition}");
        ThrowCageClientRpc(targetPosition);
    }

    [ClientRpc]
    private void ThrowCageClientRpc(Vector3 targetPosition)
    {
        Debug.Log("¡Caja lanzada en pantalla!");
    }
}