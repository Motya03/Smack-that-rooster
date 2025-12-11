using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameManageMultiplayer : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private TimerMultiplayer timer;
    [SerializeField] private GameObject[] playerPopups;
    [SerializeField] private Text winnerText;
    [SerializeField] private GameObject canvasWinner;

    [Header("Canvas Gameplay")]
    public GameObject canvasLocal;

    private bool gameEnded = false;
    public bool gameStarted = false;
    private bool isSuddenDeath = false;
    private bool clickerActive = false;

    private void Start()
    {
        Debug.Log("🔥 [GM] Start ejecutado");
    }

    public void SetClickerState(bool state)
    {
        clickerActive = state;
        Debug.Log("🎯 [GM] SetClickerState → " + state);
    }

    public void ActivateGame()
    {
        Debug.Log("🔥🔥🔥 [GM] ActivateGame() EJECUTADO 🔥🔥🔥");

        if (gameStarted)
        {
            Debug.Log("⚠️ [GM] La partida ya estaba iniciada → salgo");
            return;
        }

        gameStarted = true;

        if (timer == null)
        {
            Debug.LogError("❌❌❌ [GM] ERROR: Timer es NULL en ActivateGame()");
            return;
        }

        // Reiniciar timer
        Debug.Log("⏱ [GM] Reseteando e iniciando timer…");
        timer.ResetTimer();
        timer.StartTimer();

        // Asegurar HUD
        if (canvasLocal != null)
            canvasLocal.SetActive(true);
    }

    // Debug: función requerida por otros scripts
    public void CheckRemainingPlayers()
    {
        Debug.Log("🧮 [GM] CheckRemainingPlayers llamado");
    }
}
