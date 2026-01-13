using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameManageMultiplayer : NetworkBehaviour
{
    [Header("Referencias")]
    [SerializeField] private TimerMultiplayer timer;
    [SerializeField] private GameObject[] playerPopups;
    [SerializeField] private Text winnerText;
    [SerializeField] private GameObject canvasWinner;

    private GameObject enemy;

    private bool throwCageBool = true;
    private Transform enemyPoint;

    [Header("Prefab")]
    [SerializeField] private GameObject Box; // ¡Este prefab debe tener un NetworkObject!

    [Header("Canvas Gameplay")]
    public GameObject canvasLocal;

    private bool gameEndedServer = false;
    private bool gameEndedClient = false;

    public bool gameStarted = false;
    private bool clickerActive = false;

    private void Start()
    {
        Debug.Log("🔥 [GM] Start ejecutado");
    }

    private void Update()
    {
        // ✅ El servidor decide cuándo termina la partida.
        if (!IsServer) return;
        if (!gameStarted || gameEndedServer) return;

        if (clickerActive) return; // mientras el clicker está activo, no cerramos por “queda 1 vivo” aquí

        CheckRemainingPlayers();
    }

    public void SetClickerState(bool state)
    {
        clickerActive = state;
    }

    public void ActivateGame()
    {
        if (gameStarted) return;
        gameStarted = true;

        if (timer != null)
        {
            timer.ResetTimer();
            timer.StartTimer();
        }

        if (canvasLocal != null) canvasLocal.SetActive(true);
    }

    // ------------------------------------------------------------------
    // Caja / Cage
    // ------------------------------------------------------------------
    public void PrepareCage()
    {
        if (throwCageBool && IsServer)
        {
            RequestThrowBoxServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestThrowBoxServerRpc(ServerRpcParams serverRpcParams = default)
    {
        StartCoroutine(ServerThrowBoxCoroutine());
    }

    private IEnumerator ServerThrowBoxCoroutine()
    {
        yield return new WaitForSeconds(1f);

        GameObject target = FindEnemyLogic();
        if (target == null)
        {
            Debug.LogWarning("❌ [Server] No hay enemigo para tirar la caja");
            yield break;
        }

        Vector3 spawnPos = target.transform.position + Vector3.up * 10f;

        GameObject cajaInstance = Instantiate(Box, spawnPos, Quaternion.identity);

        NetworkObject netObj = cajaInstance.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
            Debug.Log("🔥 Caja spawneada en red correctamente");
        }
        else
        {
            Debug.LogError("❌ El prefab de la caja NO tiene el componente NetworkObject");
        }
    }

    public GameObject FindEnemyLogic()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players.Length == 0) return null;

        GameObject selectedEnemy = players[Random.Range(0, players.Length)];
        enemy = selectedEnemy;
        return selectedEnemy;
    }

    // ------------------------------------------------------------------
    // ✅ NUEVO: Cierre por “queda 1 vivo”
    // ------------------------------------------------------------------
    public void CheckRemainingPlayers()
    {
        if (!IsServer || gameEndedServer) return;

        int alive = 0;
        int lastAliveIndex = -1;

        for (int i = 0; i < PlayerSpawnMultiplayer.joinedPlayers.Count; i++)
        {
            GameObject obj = PlayerSpawnMultiplayer.joinedPlayers[i];
            if (obj == null) continue;

            PlayerMovMultiplayer p = obj.GetComponent<PlayerMovMultiplayer>();
            if (p == null) continue;

            if (!p.isDefinitivelyDead)
            {
                alive++;
                lastAliveIndex = i;
            }
        }

        if (alive == 1 && lastAliveIndex != -1)
        {
            gameEndedServer = true;
            EndGameClientRpc(lastAliveIndex);
        }
    }

    // ------------------------------------------------------------------
    // Tiempo terminado
    // ------------------------------------------------------------------
    public void TimeEnded()
    {
        if (!IsServer || gameEndedServer) return;
        if (clickerActive) return;

        gameEndedServer = true;
        EndGameClientRpc(GetWinnerIndexByHealth());
    }


    // ------------------------------------------------------------------
    // UI / End Game
    // ------------------------------------------------------------------
    private void EndGame(int winnerIndex)
    {
        if (gameEndedClient) return;
        gameEndedClient = true;


        foreach (var obj in PlayerSpawnMultiplayer.joinedPlayers)
        {
            if (obj != null)
                PlayerSpawnMultiplayer.TogglePlayerControl(obj, false);
        }

        foreach (var p in playerPopups)
        {
            if (p != null)
                p.SetActive(false);
        }

        string msg = $"Ganador: Jugador {winnerIndex + 1}";

        if (winnerIndex >= 0 && winnerIndex < playerPopups.Length && playerPopups[winnerIndex] != null)
            playerPopups[winnerIndex].SetActive(true);

        if (canvasLocal != null)
            canvasLocal.SetActive(false);

        if (winnerText != null)
            winnerText.text = msg;

        if (canvasWinner != null)
            canvasWinner.SetActive(true);

        Debug.Log("🎉 FIN DE PARTIDA → " + msg);

        MusicManager.StopMusic(MusicType.MainMenuBack);
        MusicManager.StopMusic(MusicType.FightMusic);
        MusicManager.StopMusic(MusicType.ChickenMusic);
        MusicManager.StopMusic(MusicType.ClickerGameMusic);
        MusicManager.PlayMusic(MusicType.EnterCharMusic, 0.5f);
    }

    public void HandlePlayerDeathServer(ulong loserClientId)
    {
        if (!IsServer) return;
        if (gameEndedServer) return;


        int winnerIndex = -1;

        for (int i = 0; i < PlayerSpawnMultiplayer.joinedPlayers.Count; i++)
        {
            GameObject playerObj = PlayerSpawnMultiplayer.joinedPlayers[i];
            if (playerObj == null) continue;

            NetworkObject netObj = playerObj.GetComponent<NetworkObject>();
            if (netObj != null && netObj.OwnerClientId != loserClientId)
            {
                winnerIndex = i;
                break;
            }
        }

        if (winnerIndex != -1)
        {
            gameEndedServer = true;
            EndGameClientRpc(winnerIndex);

        }
    }


    [ClientRpc]
    private void EndGameClientRpc(int winnerIndex)
    {
        EndGame(winnerIndex);
    }

    public void PauseMainTimer(bool shouldPause)
    {
        if (IsServer)
        {
            PauseMainTimerClientRpc(shouldPause);
        }
    }

    [ClientRpc]
    private void PauseMainTimerClientRpc(bool shouldPause)
    {
        if (timer != null)
        {
            timer.SetPause(shouldPause);
        }
    }

    private int GetWinnerIndexByHealth()
    {
        int maxHealth = -1;
        List<int> candidates = new List<int>();

        for (int i = 0; i < PlayerSpawnMultiplayer.joinedPlayers.Count; i++)
        {
            GameObject obj = PlayerSpawnMultiplayer.joinedPlayers[i];
            if (!obj) continue;

            PlayerMovMultiplayer p = obj.GetComponent<PlayerMovMultiplayer>();
            if (!p || p.isDefinitivelyDead || p.uiHealth == null) continue;

            int h = p.uiHealth.health;

            if (h > maxHealth)
            {
                maxHealth = h;
                candidates.Clear();
                candidates.Add(i);
            }
            else if (h == maxHealth)
            {
                candidates.Add(i);
            }
        }

        if (candidates.Count > 1)
            return candidates[UnityEngine.Random.Range(0, candidates.Count)];

        return candidates.Count == 1 ? candidates[0] : 0;
    }
}
