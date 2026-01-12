using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Unity.Netcode;

public class TimerMultiplayer : NetworkBehaviour
{
    [Header("UI")]
    [SerializeField] private Text timerText;

    [Header("Configuración")]
    [SerializeField] private float duration = 60f;

    [Header("Eventos")]
    public UnityEvent onFinished;

    private float remaining;
    private bool isRunning = false;
    private bool isPaused = false;

    // Variables de control para eventos
    private bool event30Triggered = false;
    private GameManageMultiplayer gameManager;

    private void Awake()
    {
        remaining = duration;
        UpdateDisplay(remaining);

        // Cacheamos el manager al inicio para no buscarlo cada frame (Mejora rendimiento)
        gameManager = FindFirstObjectByType<GameManageMultiplayer>();
    }

    public void StartTimer()
    {
        remaining = duration;
        isRunning = true;
        isPaused = false;

        // Reseteamos los flags de eventos
        event30Triggered = false;

        UpdateDisplay(remaining);
        Debug.Log("▶️ [TimerMultiplayer] Timer iniciado");
    }

    public void ResetTimer()
    {
        remaining = duration;
        isRunning = false;
        isPaused = false;
        event30Triggered = false;
        UpdateDisplay(remaining);
    }

    public void SetPause(bool state)
    {
        isPaused = state;
        Debug.Log(state ? "⏸️ Timer PAUSADO" : "▶️ Timer REANUDADO");
    }

    private void Update()
    {
        // 1. Si no está corriendo o está pausado, no hacemos nada.
        if (!isRunning || isPaused) return;

        // 2. Restamos tiempo
        remaining -= Time.deltaTime;

        // 3. Lógica de eventos (30 segundos)
        // Usamos un flag para que solo se llame UNA vez
        if (remaining <= 30f && !event30Triggered)
        {
            event30Triggered = true;
            if (gameManager != null) gameManager.PrepareCage();
            Debug.Log("📦 Evento 30s disparado");
        }

        // 4. Fin del tiempo
        if (remaining <= 0f)
        {
            remaining = 0;
            isRunning = false;
            onFinished?.Invoke();

            if (gameManager != null) gameManager.TimeEnded();
            Debug.Log("⏰ Tiempo terminado");
        }

        // 5. Actualizar UI
        UpdateDisplay(remaining);
    }

    private void UpdateDisplay(float currentTime)
    {
        if (timerText == null) return;

        float t = Mathf.Max(currentTime, 0);
        int m = Mathf.FloorToInt(t / 60f);
        int s = Mathf.FloorToInt(t % 60f);

        timerText.text = $"{m:00}:{s:00}";
    }
}