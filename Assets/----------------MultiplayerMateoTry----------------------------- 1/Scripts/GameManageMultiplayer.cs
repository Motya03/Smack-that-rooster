using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameManageMultiplayer : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private TimerLocal timer;
    [SerializeField] private GameObject[] playerPopups;
    [SerializeField] private Text winnerText;
    [SerializeField] private GameObject canvasWinner;

    [Header("Jugador")]
    private GameObject enemy;
    private Transform enemyPoint;
    [SerializeField] private GameObject cajaRota;

    private bool throwCageBool = true;

    [Header("Canvas Gameplay (HUD Local)")]
    [SerializeField] public GameObject canvasLocal;

    public bool gameStarted = false;
    private bool gameEnded = false;
    private bool isSuddenDeath = false;

    // 🔥 Indica si el click battle está activo
    private bool clickerActive = false;
    public void SetClickerState(bool state) => clickerActive = state;

    private void Start()
    {
        // Esperar a que NetworkManager esté listo
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            StartCoroutine(WaitForGameStart());
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.U))
        {
            ThrowCage();
            Debug.Log("Рщдф");
        }

        if (!gameStarted || gameEnded)
            return;

        // ⛔ No revisar nada mientras haya click battle
        if (clickerActive)
            return;

        CheckRemainingPlayers();
    }

    // --------------------------------------------------------------------
    // 🔥 Activado desde LobbyJoinManager cuando presionan START
    // --------------------------------------------------------------------
    public void ActivateGame()
    {
        MusicManager.PlayMusic(MusicType.ChickenMusic, 0.05f);
        MusicManager.PlayMusic(MusicType.FightMusic, 0.09f);
        MusicManager.StopMusic(MusicType.EnterCharMusic);

        if (gameStarted) return;

        PrepareForNewMatch();
        ResetPlayersForNewMatch();

        gameStarted = true;
        Debug.Log("🏁 GameManagerLocal ACTIVADO");

        if (timer != null)
        {
            timer.onFinished.AddListener(OnTimerFinished);
            timer.ResetTimer();
        }
    }

    // --------------------------------------------------------------------
    // Espera a que el juego empiece
    // --------------------------------------------------------------------
    IEnumerator WaitForGameStart()
    {
        GameManageMultiplayer check = GetComponent<GameManageMultiplayer>();

        yield return new WaitUntil(() => check.gameStarted);

        FindEnemy();  // ya hay players en escena
    }

    // --------------------------------------------------------------------
    // Preparar la caja
    // --------------------------------------------------------------------
    public void PrepareCage()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        if (throwCageBool)
        {
            ThrowCage();
            throwCageBool = false;
        }
    }


    // --------------------------------------------------------------------
    // Tirar la caja
    // --------------------------------------------------------------------
    public void ThrowCage()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        StartCoroutine(ThrowCageCorutina());
    }


    private IEnumerator ThrowCageCorutina()
    {
        yield return new WaitForSeconds(1f);

        if (enemy == null)
            FindEnemy();

        if (enemy == null)
        {
            Debug.LogWarning("❌ No hay enemigo para tirar la caja");
            yield break;
        }

        Vector3 spawnPos = enemy.transform.position + Vector3.up * 10f;
        Instantiate(cajaRota, spawnPos, Quaternion.identity);
        Debug.Log("🔥 Caja lanzada correctamente");
    }

    // --------------------------------------------------------------------
    // Buscar enemigo
    // --------------------------------------------------------------------
    public void FindEnemy()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        if (players.Length > 0)
            enemy = players[UnityEngine.Random.Range(0, players.Length)];

        GameObject enemy2 = GameObject.FindWithTag("Player");
        PlayerMovMultiplayer p2 = enemy2.GetComponent<PlayerMovMultiplayer>();
        enemyPoint = p2.GallinaApunta;
    }

    // --------------------------------------------------------------------
    // Revisar cuántos jugadores siguen vivos
    // --------------------------------------------------------------------
    public void CheckRemainingPlayers()
    {
        int alive = 0;
        int lastAlive = -1;

        for (int i = 0; i < PlayerSpawn.joinedPlayers.Count; i++)
        {
            GameObject obj = PlayerSpawn.joinedPlayers[i];
            if (obj == null) continue;

            PlayerMovMultiplayer p = obj.GetComponent<PlayerMovMultiplayer>();
            if (p == null) continue;

            if (p.uiHealth != null && p.uiHealth.health > 0 && !p.isDefinitivelyDead)
            {
                alive++;
                lastAlive = i;
            }
        }

        if (alive == 1 && lastAlive != -1)
            EndGame(lastAlive);
    }

    // --------------------------------------------------------------------
    // Timer llegó a 0
    // --------------------------------------------------------------------
    private void OnTimerFinished()
    {
        if (gameEnded || clickerActive)
            return;

        int alive = CountAlivePlayers();

        if (alive >= 2 && !isSuddenDeath)
        {
            ActivateSuddenDeath();
            return;
        }

        if (alive >= 2 && isSuddenDeath)
        {
            EndGame(GetWinnerIndexByHealth());
            return;
        }

        EndGame(GetWinnerIndexByHealth());
    }

    private int CountAlivePlayers()
    {
        int count = 0;

        foreach (var obj in PlayerSpawn.joinedPlayers)
        {
            if (!obj) continue;

            PlayerMovMultiplayer p = obj.GetComponent<PlayerMovMultiplayer>();
            if (!p) continue;

            if (p.uiHealth != null && p.uiHealth.health > 0 && !p.isDefinitivelyDead)
                count++;
        }

        return count;
    }

    // --------------------------------------------------------------------
    // Winner por salud
    // --------------------------------------------------------------------
    private int GetWinnerIndexByHealth()
    {
        int maxHealth = -1;
        List<int> candidates = new List<int>();

        for (int i = 0; i < PlayerSpawn.joinedPlayers.Count; i++)
        {
            GameObject obj = PlayerSpawn.joinedPlayers[i];
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

    // --------------------------------------------------------------------
    // Activar muerte súbita
    // --------------------------------------------------------------------
    private void ActivateSuddenDeath()
    {
        isSuddenDeath = true;

        Debug.Log("⚡ Muerte súbita activada");

        if (winnerText != null)
            winnerText.text = "Muerte Súbita";

        if (timer != null)
        {
            timer.StopAllCoroutines();
            timer.ResetTimer();
            timer.StartSuddenDeath();
        }
    }

    // --------------------------------------------------------------------
    // Final del juego
    // --------------------------------------------------------------------
    private void EndGame(int winnerIndex)
    {
        if (gameEnded) return;

        gameEnded = true;

        foreach (var obj in PlayerSpawn.joinedPlayers)
            PlayerSpawn.TogglePlayerControl(obj, false);

        foreach (var p in playerPopups)
            p.SetActive(false);

        string msg = $"Ganador: Jugador {winnerIndex + 1}";

        if (winnerIndex >= 0 && winnerIndex < playerPopups.Length)
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

    // --------------------------------------------------------------------
    // Preparar HUD para una nueva partida
    // --------------------------------------------------------------------
    public void PrepareForNewMatch()
    {
        gameEnded = false;
        isSuddenDeath = false;

        if (canvasLocal != null)
            canvasLocal.SetActive(true);

        if (canvasWinner != null)
            canvasWinner.SetActive(false);

        if (winnerText != null)
            winnerText.text = "";

        for (int i = 0; i < playerPopups.Length; i++)
            playerPopups[i].SetActive(false);

        if (timer != null)
        {
            timer.StopAllCoroutines();
            timer.ResetTimer();
        }

        if (timer != null)
        {
            timer.onFinished.RemoveListener(OnTimerFinished);
            timer.onFinished.AddListener(OnTimerFinished);
        }
    }

    // --------------------------------------------------------------------
    // Reset jugadores para nueva partida
    // --------------------------------------------------------------------
    public void ResetPlayersForNewMatch()
    {
        for (int i = 0; i < PlayerSpawn.joinedPlayers.Count; i++)
        {
            var obj = PlayerSpawn.joinedPlayers[i];
            if (obj == null) continue;

            var p = obj.GetComponent<PlayerMovMultiplayer>();
            if (p == null) continue;

            p.isDefinitivelyDead = false;
            p.vidas = 3;
            p.lives = 1;
            p.ResetVidas();
        }
    }
}
