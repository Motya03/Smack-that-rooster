using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameManagerMultiplayer : NetworkBehaviour
{
    public static GameManagerMultiplayer Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TimerMultiplayer timer;
    [SerializeField] private GameObject[] playerPopups;
    [SerializeField] private Text winnerText;
    [SerializeField] private GameObject canvasWinner;
    [SerializeField] private GameObject canvasLocal;

    [Header("Game Objects")]
    [SerializeField] private GameObject cajaRota;

    private NetworkVariable<bool> gameStarted = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> gameEnded = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> isSuddenDeath = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> clickerActive = new NetworkVariable<bool>(false);

    private GameObject enemy;
    private bool throwCageBool = true;

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

    private void Start()
    {
        // Initialize UI
        foreach (var p in playerPopups)
            p.SetActive(false);

        if (canvasWinner != null)
            canvasWinner.SetActive(false);

        if (canvasLocal != null)
            canvasLocal.SetActive(true);
    }
    // Método público para verificar si el juego ha comenzado
    public bool IsGameStarted()
    {
        return gameStarted.Value;
    }
    [ServerRpc(RequireOwnership = false)]
    public void ActivateGameServerRpc()
    {
        if (gameStarted.Value) return;

        gameStarted.Value = true;
        gameEnded.Value = false;
        isSuddenDeath.Value = false;

        // Reset all players
        ResetPlayersForNewMatch();

        // Start timer
        if (timer != null)
        {
            timer.StartTimerServerRpc();
        }

        // Play music
        PlayMusicClientRpc();

        ActivateGameClientRpc();
    }

    [ClientRpc]
    private void ActivateGameClientRpc()
    {
        // Show local canvas
        if (canvasLocal != null)
            canvasLocal.SetActive(true);

        // Hide winner canvas
        if (canvasWinner != null)
            canvasWinner.SetActive(false);
    }

    [ClientRpc]
    private void PlayMusicClientRpc()
    {
        MusicManager.PlayMusic(MusicType.ChickenMusic, 0.05f);
        MusicManager.PlayMusic(MusicType.FightMusic, 0.09f);
        MusicManager.StopMusic(MusicType.EnterCharMusic);
    }

    [ServerRpc]
    public void CheckRemainingPlayersServerRpc()
    {
        if (gameEnded.Value || clickerActive.Value) return;

        int aliveCount = 0;
        ulong lastAliveId = 0;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var player = client.PlayerObject.GetComponent<PlayerMovMultiplayer>();
            if (player != null && !player.networkIsDefinitivelyDead.Value && player.networkVidas.Value > 0)
            {
                aliveCount++;
                lastAliveId = client.ClientId;
            }
        }

        if (aliveCount == 1)
        {
            EndGameServerRpc(lastAliveId);
        }
        else if (aliveCount == 0)
        {
            // Draw game
            EndGameServerRpc(999); // Special ID for draw
        }
    }

    [ServerRpc]
    public void EndGameServerRpc(ulong winnerId)
    {
        if (gameEnded.Value) return;

        gameEnded.Value = true;

        // Disable player controls
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var player = client.PlayerObject.GetComponent<PlayerMovMultiplayer>();
            if (player != null)
            {
                player.SetStateServerRpc(PlayerMovMultiplayer.States.Idle);
            }
        }

        // Determine winner message
        string winnerMessage = "";
        if (winnerId == 999)
        {
            winnerMessage = "¡Empate!";
        }
        else
        {
            winnerMessage = $"Ganador: Jugador {winnerId + 1}";
        }

        EndGameClientRpc(winnerId, winnerMessage);
    }

    [ClientRpc]
    private void EndGameClientRpc(ulong winnerId, string winnerMessage)
    {
        // Hide local canvas
        if (canvasLocal != null)
            canvasLocal.SetActive(false);

        // Show winner canvas
        if (canvasWinner != null)
        {
            canvasWinner.SetActive(true);
            if (winnerText != null)
                winnerText.text = winnerMessage;
        }

        // Stop all music
        MusicManager.StopMusic(MusicType.MainMenuBack);
        MusicManager.StopMusic(MusicType.FightMusic);
        MusicManager.StopMusic(MusicType.ChickenMusic);
        MusicManager.StopMusic(MusicType.ClickerGameMusic);
        MusicManager.PlayMusic(MusicType.EnterCharMusic, 0.5f);
    }

    private void ResetPlayersForNewMatch()
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var player = client.PlayerObject.GetComponent<PlayerMovMultiplayer>();
            if (player != null)
            {
                player.networkIsDefinitivelyDead.Value = false;
                player.networkVidas.Value = 3;
                player.networkLives.Value = 1;
                player.SetStateServerRpc(PlayerMovMultiplayer.States.Idle);

                // Reset health UI
                if (player.uiHealth != null)
                {
                    player.uiHealth.ResetHealth();
                }
            }
        }
    }

    [ServerRpc]
    public void SetClickerStateServerRpc(bool state)
    {
        clickerActive.Value = state;
    }

    [ServerRpc]
    public void PrepareCageServerRpc()
    {
        if (throwCageBool)
        {
            ThrowCageServerRpc();
            throwCageBool = false;
        }
    }

    [ServerRpc]
    private void ThrowCageServerRpc()
    {
        // Find a random player to throw cage at
        var clients = NetworkManager.Singleton.ConnectedClientsList;
        if (clients.Count > 0)
        {
            int randomIndex = Random.Range(0, clients.Count);
            var targetPlayer = clients[randomIndex].PlayerObject;

            if (targetPlayer != null)
            {
                Vector3 spawnPos = targetPlayer.transform.position + Vector3.up * 10f;

                // Spawn broken box as network object
                var boxObj = Instantiate(cajaRota, spawnPos, Quaternion.identity);
                var networkBox = boxObj.GetComponent<NetworkObject>();
                if (networkBox != null)
                {
                    networkBox.Spawn();
                }
            }
        }
    }

    public void OnTimerFinished()
    {
        if (IsServer && !gameEnded.Value && !clickerActive.Value)
        {
            // Timer finished logic
            CheckRemainingPlayersServerRpc();
        }
    }
}