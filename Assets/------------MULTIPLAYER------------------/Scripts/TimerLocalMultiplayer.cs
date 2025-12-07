using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

public class TimerLocalMultiplayer : NetworkBehaviour
{
    [Header("UI")]
    [SerializeField] private Text timerText;

    [Header("Configuración")]
    [SerializeField] private float duration = 60f;
    [SerializeField] private bool startOnAwake = true;

    [Header("Eventos")]
    public UnityEvent onFinished;

    // Variables de red
    private NetworkVariable<float> remainingTime = new NetworkVariable<float>(
        60f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<bool> timerRunning = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<bool> isSuddenDeath = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private Coroutine countdownCoroutine;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Suscribirse a cambios en las variables de red
        remainingTime.OnValueChanged += OnTimeChanged;
        timerRunning.OnValueChanged += OnRunningChanged;
        isSuddenDeath.OnValueChanged += OnSuddenDeathChanged;

        // Inicializar UI
        UpdateDisplay(remainingTime.Value);

        if (IsServer)
        {
            if (startOnAwake)
            {
                // Esperar un momento antes de empezar
                StartCoroutine(StartTimerAfterDelay(2f));
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        remainingTime.OnValueChanged -= OnTimeChanged;
        timerRunning.OnValueChanged -= OnRunningChanged;
        isSuddenDeath.OnValueChanged -= OnSuddenDeathChanged;

        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }

        base.OnNetworkDespawn();
    }

    private IEnumerator StartTimerAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ResetTimer();
    }

    // Llamado cuando el tiempo cambia en la red
    private void OnTimeChanged(float oldValue, float newValue)
    {
        UpdateDisplay(newValue);

        // Verificar si es momento de lanzar la caja (a los 30 segundos)
        if (IsServer && Mathf.Approximately(newValue, 30f) && !isSuddenDeath.Value)
        {
            PrepareCageServerRpc();
        }
    }

    private void OnRunningChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"Timer running: {newValue}");
    }

    private void OnSuddenDeathChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            Debug.Log("⚡ Muerte súbita activada");
            if (timerText != null)
                timerText.color = Color.red;
        }
    }

    [ServerRpc]
    private void PrepareCageServerRpc()
    {
        Debug.Log("🕒 30 segundos - Preparando caja");
        PrepareCageClientRpc();
    }

    [ClientRpc]
    private void PrepareCageClientRpc()
    {
        // Notificar al GameManager para que lance la caja
        if (GameManagerMultiplayer.Instance != null)
        {
            GameManagerMultiplayer.Instance.PrepareCage();
        }
    }

    // Método para empezar el contador (solo servidor)
    public void ResetTimer()
    {
        if (!IsServer) return;

        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }

        remainingTime.Value = duration;
        timerRunning.Value = true;
        isSuddenDeath.Value = false;

        if (timerText != null)
            timerText.color = Color.white;

        countdownCoroutine = StartCoroutine(CountdownCoroutine());
    }

    private IEnumerator CountdownCoroutine()
    {
        if (!IsServer) yield break;

        while (remainingTime.Value > 0f && timerRunning.Value)
        {
            yield return new WaitForSeconds(1f); // Actualizar cada segundo
            remainingTime.Value -= 1f;

            // Log cada 10 segundos para debugging
            if (Mathf.Approximately(remainingTime.Value % 10f, 0f))
            {
                Debug.Log($"Tiempo restante: {remainingTime.Value} segundos");
            }
        }

        // Timer terminó
        if (remainingTime.Value <= 0f)
        {
            remainingTime.Value = 0f;
            timerRunning.Value = false;

            // Disparar evento de timer terminado
            TimerFinishedServerRpc();
        }
    }

    [ServerRpc]
    private void TimerFinishedServerRpc()
    {
        Debug.Log("⏰ Timer terminado");
        TimerFinishedClientRpc();
        onFinished?.Invoke();
    }

    [ClientRpc]
    private void TimerFinishedClientRpc()
    {
        Debug.Log("Timer terminado recibido en cliente");
        // Sonido, efectos, etc.
    }

    public void StartSuddenDeath()
    {
        if (!IsServer) return;

        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }

        isSuddenDeath.Value = true;
        remainingTime.Value = duration; // O podrías usar un tiempo diferente para muerte súbita
        timerRunning.Value = true;

        if (timerText != null)
            timerText.color = Color.red;

        countdownCoroutine = StartCoroutine(CountdownCoroutine());
    }

    public void StopTimer()
    {
        if (!IsServer) return;

        timerRunning.Value = false;
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
    }

    public void AddTime(float secondsToAdd)
    {
        if (!IsServer) return;

        remainingTime.Value += secondsToAdd;
        Debug.Log($"Se añadieron {secondsToAdd} segundos. Tiempo total: {remainingTime.Value}");
    }

    private void UpdateDisplay(float time)
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";

        // Cambiar color cuando quedan menos de 10 segundos
        if (time <= 10f && !isSuddenDeath.Value)
        {
            timerText.color = Color.yellow;
        }
        else if (time <= 5f && !isSuddenDeath.Value)
        {
            timerText.color = Color.red;
        }
    }

    // Propiedades públicas para acceso
    public float RemainingTime => remainingTime.Value;
    public bool IsRunning => timerRunning.Value;
    public bool IsSuddenDeath => isSuddenDeath.Value;

    // Método para debugging
    public void DebugTimerStatus()
    {
        Debug.Log($"Timer Status - Time: {remainingTime.Value}, Running: {timerRunning.Value}, SuddenDeath: {isSuddenDeath.Value}");
    }
}