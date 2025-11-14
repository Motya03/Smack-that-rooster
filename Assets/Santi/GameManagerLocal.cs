using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class GameManagerLocal : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private TimerLocal timer;           // Referencia al temporizador
    [SerializeField] private GameObject[] playerPopups;  // Pop-ups individuales por jugador
    [SerializeField] private Text winnerText;            // Texto compartido
    [SerializeField] private GameObject canvasWinner;    // Canvas que se activa al final del juego

    private bool gameEnded = false;
    private bool isSuddenDeath = false;

    private void Start()
    {
        // Desactivar pop-ups y canvas al inicio
        foreach (var popup in playerPopups)
            popup.SetActive(false);

        if (canvasWinner != null)
            canvasWinner.SetActive(false);

        // Escuchar el evento del temporizador
        if (timer != null)
            timer.onFinished.AddListener(OnTimerFinished);
    }
    private void Update()
    {
        if (!gameEnded)
            CheckRemainingPlayers();
    }

    // Revisa cuántos jugadores siguen vivos
    public void CheckRemainingPlayers()
    {
        int aliveCount = 0;
        int lastAliveIndex = -1;

        for (int i = 0; i < PlayerSpawn.joinedPlayers.Count; i++)
        {
            var playerObj = PlayerSpawn.joinedPlayers[i];
            if (!playerObj) continue;

            var player = playerObj.GetComponent<PlayerMovLocal>();
            if (player == null) continue;

            // Solo cuenta jugadores con vida y que no estén marcados como definitivamente muertos
            if (player.uiHealth != null && player.uiHealth.health > 0 && !player.isDefinitivelyDead)
            {
                aliveCount++;
                lastAliveIndex = i;
            }
        }

        // Si solo queda uno vivo, termina el juego
        if (aliveCount == 1 && lastAliveIndex != -1)
        {
            EndGame(lastAliveIndex);
        }
    }

    // Se ejecuta automáticamente cuando el temporizador llega a 0
    private void OnTimerFinished()
    {
        if (gameEnded) return;

        int aliveCount = CountAlivePlayers();

        if (aliveCount >= 2 && !isSuddenDeath)
        {
            ActivateSuddenDeath();
        }
        else if (aliveCount >= 2 && isSuddenDeath)
        {
            EndGame(-2); // empate final
        }
        else
        {
            int winnerIndex = GetWinnerIndexByHealth();
            EndGame(winnerIndex);
        }
    }

    // Cuenta los jugadores activos y no definitivamente muertos
    private int CountAlivePlayers()
    {
        int count = 0;

        for (int i = 0; i < PlayerSpawn.joinedPlayers.Count; i++)
        {
            var playerObj = PlayerSpawn.joinedPlayers[i];
            if (!playerObj) continue;

            var player = playerObj.GetComponent<PlayerMovLocal>();
            if (player != null && player.uiHealth != null && player.uiHealth.health > 0 && !player.isDefinitivelyDead)
                count++;
        }

        return count;
    }

    // Determina el jugador con más vida
    private int GetWinnerIndexByHealth()
    {
        int maxHealth = -1;
        int winnerIndex = -1;
        bool empate = false;

        for (int i = 0; i < PlayerSpawn.joinedPlayers.Count; i++)
        {
            var playerObj = PlayerSpawn.joinedPlayers[i];
            if (!playerObj) continue;

            var player = playerObj.GetComponent<PlayerMovLocal>();
            if (player == null || player.uiHealth == null || player.isDefinitivelyDead) continue;

            int h = player.uiHealth.health;
            if (h > maxHealth)
            {
                maxHealth = h;
                winnerIndex = i;
                empate = false;
            }
            else if (h == maxHealth)
            {
                empate = true;
            }
        }

        return empate ? -2 : winnerIndex;
    }

    // Activa la fase de Muerte Súbita
    private void ActivateSuddenDeath()
    {
        isSuddenDeath = true;
        Debug.Log("Muerte súbita activada.");

        if (winnerText != null)
            winnerText.text = "Muerte súbita";

        if (timer != null)
        {
            timer.StopAllCoroutines();
            timer.ResetTimer();
            timer.StartSuddenDeath();
        }
    }

    // Finaliza la partida
    private void EndGame(int winnerIndex)
    {
        gameEnded = true;

        // Desactivar control de jugadores
        foreach (var player in PlayerSpawn.joinedPlayers)
            PlayerSpawn.TogglePlayerControl(player, false);

        // Apagar todos los pop-ups
        foreach (var popup in playerPopups)
            popup.SetActive(false);

        string message;

        if (winnerIndex == -2)
        {
            message = "Empate final";
        }
        else
        {
            message = $"Ganador: Jugador {winnerIndex + 1}";
            if (winnerIndex >= 0 && winnerIndex < playerPopups.Length)
                playerPopups[winnerIndex].SetActive(true);
        }

        // Mostrar texto y activar Canvas Winner
        if (winnerText != null)
            winnerText.text = message;

        if (canvasWinner != null)
            canvasWinner.SetActive(true);

        Debug.Log("Fin de la partida -> " + message);
    }
}
