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
    [SerializeField] private bool startOnAwake = false; // NO auto-start en network

    [Header("Eventos")]
    public UnityEvent onFinished;

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
    private bool alreadySentCage = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        remainingTime.OnValueChanged += OnTimeChanged;
        timerRunning.OnValueChanged += OnRunningChanged;
        isSuddenDeath.OnValueChanged += OnSuddenDeathChanged;

        UpdateDisplay(remainingTime.Value);

        if (IsServer && startOnAwake)
        {
            // Si realmente quieres auto-start, el server lo hace
            ResetTimer();
        }
    }

    public override void OnNetworkDespawn()
    {
        remainingTime.OnValueChanged -= OnTimeChanged;
        timerRunning.OnValueChanged -= OnRunningChanged;
        isSuddenDeath.OnValueChanged -= OnSuddenDeathChanged;

        if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
        base.OnNetworkDespawn();
    }

    private void OnTimeChanged(float oldVal, float newVal)
    {
        UpdateDisplay(newVal);

        // Si cruzamos 30s desde arriba hacia abajo, preparamos caja (solo server)
        if (IsServer && oldVal > 30f && newVal <= 30f && !isSuddenDeath.Value && !alreadySentCage)
        {
            alreadySentCage = true;
            PrepareCageServerRpc();
        }
    }

    private void OnRunningChanged(bool oldVal, bool newVal)
    {
        // debug
        Debug.Log($"Timer running: {newVal}");
    }

    private void OnSuddenDeathChanged(bool oldVal, bool newVal)
    {
        if (newVal)
        {
            Debug.Log("⚡ Muerte súbita activada");
            if (timerText != null) timerText.color = Color.red;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PrepareCageServerRpc(ServerRpcParams rpcParams = default)
    {
        // El server notifica al GameManager para lanzar la caja
        if (GameManagerMultiplayer.Instance != null)
        {
            GameManagerMultiplayer.Instance.PrepareCage();
        }
        PrepareCageClientRpc();
    }

    [ClientRpc]
    private void PrepareCageClientRpc()
    {
        Debug.Log("Preparando caja (cliente)");
    }

    public void ResetTimer()
    {
        if (!IsServer) return;

        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }

        remainingTime.Value = duration;
        timerRunning.Value = true;
        isSuddenDeath.Value = false;
        alreadySentCage = false;

        if (timerText != null) timerText.color = Color.white;

        countdownCoroutine = StartCoroutine(CountdownCoroutine());
        Debug.Log("🔵 TIMER RESET desde GameManager");
    }

    private IEnumerator CountdownCoroutine()
    {
        if (!IsServer) yield break;

        while (remainingTime.Value > 0f && timerRunning.Value)
        {
            yield return new WaitForSeconds(1f);
            remainingTime.Value = Mathf.Max(0f, remainingTime.Value - 1f);

            if (Mathf.Approximately(remainingTime.Value % 10f, 0f))
            {
                Debug.Log($"Tiempo restante: {remainingTime.Value} segundos");
            }
        }

        if (remainingTime.Value <= 0f)
        {
            remainingTime.Value = 0f;
            timerRunning.Value = false;
            TimerFinishedServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void TimerFinishedServerRpc(ServerRpcParams rpcParams = default)
    {
        Debug.Log("⏰ Timer terminado (server)");
        TimerFinishedClientRpc();
        onFinished?.Invoke();
    }

    [ClientRpc]
    private void TimerFinishedClientRpc()
    {
        Debug.Log("Timer terminado recibido en cliente");
    }

    public void StartSuddenDeath()
    {
        if (!IsServer) return;

        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }

        isSuddenDeath.Value = true;
        remainingTime.Value = duration;
        timerRunning.Value = true;
        if (timerText != null) timerText.color = Color.red;
        countdownCoroutine = StartCoroutine(CountdownCoroutine());
    }

    public void StopTimer()
    {
        if (!IsServer) return;
        timerRunning.Value = false;
        if (countdownCoroutine != null) { StopCoroutine(countdownCoroutine); countdownCoroutine = null; }
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

        if (time <= 10f && !isSuddenDeath.Value)
            timerText.color = Color.yellow;
        else if (time <= 5f && !isSuddenDeath.Value)
            timerText.color = Color.red;
        else if (!isSuddenDeath.Value)
            timerText.color = Color.white;
    }

    // Exponer valor (lectura)
    public float RemainingTime => remainingTime.Value;
    public bool IsRunning => timerRunning.Value;
    public bool IsSuddenDeath => isSuddenDeath.Value;
}
