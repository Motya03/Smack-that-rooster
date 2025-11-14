using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManagerLocal : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private TimerLocal timer;
    [SerializeField] private GameObject[] playerPopups;
    [SerializeField] private Text winnerText;
    [SerializeField] private GameObject canvasWinner;

    [Header("Canvas Gameplay (HUD Local)")]
    [SerializeField] private GameObject canvasLocal;

    private bool gameStarted = false;
    private bool gameEnded = false;
    private bool isSuddenDeath = false;

    // 🔥 Indica si el click battle está activo
    private bool clickerActive = false;
    public void SetClickerState(bool state) => clickerActive = state;

    private void Start()
    {
        // Se apaga todo al iniciar
        foreach (var p in playerPopups)
            p.SetActive(false);

        if (canvasWinner != null)
            canvasWinner.SetActive(false);
    }

    // 🔥 Activado desde LobbyJoinManager cuando presionan START
    public void ActivateGame()
    {
        if (gameStarted) return;

        gameStarted = true;
        Debug.Log("🏁 GameManagerLocal ACTIVADO");

        if (timer != null)
        {
            timer.onFinished.AddListener(OnTimerFinished);
            timer.ResetTimer();
        }
    }

    private void Update()
    {
        if (!gameStarted || gameEnded)
            return;

        // ⛔ No revisar nada mientras haya click battle
        if (clickerActive)
            return;

        CheckRemainingPlayers();
    }

    // -----------------------------------------------------
    // 🔍 Revisa cuántos jugadores siguen vivos
    // -----------------------------------------------------
    public void CheckRemainingPlayers()
    {
        int alive = 0;
        int lastAlive = -1;

        for (int i = 0; i < PlayerSpawn.joinedPlayers.Count; i++)
        {
            GameObject obj = PlayerSpawn.joinedPlayers[i];
            if (!obj) continue;

            PlayerMovLocal p = obj.GetComponent<PlayerMovLocal>();
            if (!p) continue;

            if (p.uiHealth != null &&
                p.uiHealth.health > 0 &&
                !p.isDefinitivelyDead)
            {
                alive++;
                lastAlive = i;
            }
        }

        if (alive == 1 && lastAlive != -1)
            EndGame(lastAlive);
    }

    // -----------------------------------------------------
    // ⏳ Cuando el timer llega a 0
    // -----------------------------------------------------
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

            PlayerMovLocal p = obj.GetComponent<PlayerMovLocal>();
            if (!p) continue;

            if (p.uiHealth != null &&
                p.uiHealth.health > 0 &&
                !p.isDefinitivelyDead)
                count++;
        }

        return count;
    }

    // -----------------------------------------------------
    // ❤️ Ganador por salud (sin empates)
    // -----------------------------------------------------
    private int GetWinnerIndexByHealth()
    {
        int maxHealth = -1;
        List<int> candidates = new List<int>();

        for (int i = 0; i < PlayerSpawn.joinedPlayers.Count; i++)
        {
            GameObject obj = PlayerSpawn.joinedPlayers[i];
            if (!obj) continue;

            PlayerMovLocal p = obj.GetComponent<PlayerMovLocal>();
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

        // 🔥 Si hay empate → elegir uno aleatorio (sin empates finales)
        if (candidates.Count > 1)
            return candidates[Random.Range(0, candidates.Count)];

        return candidates.Count == 1 ? candidates[0] : 0;
    }

    // -----------------------------------------------------
    // ⚡ Activar muerte súbita
    // -----------------------------------------------------
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

    // -----------------------------------------------------
    // 🏆 Final de partida
    // -----------------------------------------------------
    private void EndGame(int winnerIndex)
    {
        if (gameEnded) return;

        gameEnded = true;

        // Desactivar control de jugadores
        foreach (var obj in PlayerSpawn.joinedPlayers)
            PlayerSpawn.TogglePlayerControl(obj, false);

        // Apagar popups individuales
        foreach (var p in playerPopups)
            p.SetActive(false);

        // Mensaje ganador
        string msg = $"Ganador: Jugador {winnerIndex + 1}";

        // Mostrar popup del ganador
        if (winnerIndex >= 0 && winnerIndex < playerPopups.Length)
            playerPopups[winnerIndex].SetActive(true);

        // 🔥 DESACTIVAR HUD LOCAL
        if (canvasLocal != null)
            canvasLocal.SetActive(false);

        // 🔥 ACTIVAR CANVAS GANADOR
        if (winnerText != null)
            winnerText.text = msg;

        if (canvasWinner != null)
            canvasWinner.SetActive(true);

        Debug.Log("🎉 FIN DE PARTIDA → " + msg);
    }
}
