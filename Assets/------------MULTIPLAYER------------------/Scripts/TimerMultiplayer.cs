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
    [SerializeField] private float suddenDeathDuration = 30f;
    [SerializeField] private bool startOnAwake = true;

    [Header("Eventos")]
    public UnityEvent onFinished;

    private NetworkVariable<float> remaining = new NetworkVariable<float>(60f);
    private NetworkVariable<bool> isRunning = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> isSuddenDeath = new NetworkVariable<bool>(false);

    private bool cagePrepared = false;

    public override void OnNetworkSpawn()
    {
        if (IsServer && startOnAwake)
        {
            StartTimerServerRpc();
        }

        remaining.OnValueChanged += OnRemainingChanged;
        UpdateDisplay();
    }

    [ServerRpc]
    public void StartTimerServerRpc()
    {
        remaining.Value = duration;
        isRunning.Value = true;
        isSuddenDeath.Value = false;
        cagePrepared = false;
    }

    [ServerRpc]
    public void StartSuddenDeathServerRpc()
    {
        remaining.Value = suddenDeathDuration;
        isRunning.Value = true;
        isSuddenDeath.Value = true;
        cagePrepared = false;

        StartSuddenDeathClientRpc();
    }

    [ClientRpc]
    private void StartSuddenDeathClientRpc()
    {
        Debug.Log("¡Muerte súbita activada!");
        // Puedes añadir efectos visuales/sonoros aquí
    }

    [ServerRpc]
    public void StopTimerServerRpc()
    {
        isRunning.Value = false;
    }

    [ServerRpc]
    public void ResetTimerServerRpc()
    {
        remaining.Value = duration;
        isRunning.Value = false;
        isSuddenDeath.Value = false;
        cagePrepared = false;
    }

    private void Update()
    {
        if (IsServer && isRunning.Value)
        {
            float newRemaining = remaining.Value - Time.deltaTime;

            // Check for cage at 30 seconds (solo en tiempo normal, no en muerte súbita)
            if (!isSuddenDeath.Value && !cagePrepared && newRemaining <= 30f)
            {
                cagePrepared = true;
                PrepareCageServerRpc();
            }

            if (newRemaining <= 0f)
            {
                newRemaining = 0f;
                isRunning.Value = false;
                TimerFinishedServerRpc();
            }

            remaining.Value = newRemaining;
        }

        // Todos los clientes actualizan el display
        UpdateDisplay();
    }

    [ServerRpc]
    private void PrepareCageServerRpc()
    {
        Debug.Log("Preparing cage at 30 seconds...");

        // Notificar a GameManager para que tire la caja
        if (GameManagerMultiplayer.Instance != null)
        {
            GameManagerMultiplayer.Instance.PrepareCageServerRpc();
        }
    }

    [ServerRpc]
    private void TimerFinishedServerRpc()
    {
        TimerFinishedClientRpc();

        // Notificar a GameManager
        if (GameManagerMultiplayer.Instance != null)
        {
            GameManagerMultiplayer.Instance.OnTimerFinished();
        }
    }

    [ClientRpc]
    private void TimerFinishedClientRpc()
    {
        onFinished?.Invoke();
        Debug.Log("¡Tiempo terminado!");
    }

    private void OnRemainingChanged(float oldValue, float newValue)
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(remaining.Value / 60f);
        int seconds = Mathf.FloorToInt(remaining.Value % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";

        // Cambiar color en muerte súbita (opcional)
        if (isSuddenDeath.Value)
        {
            timerText.color = Color.red;
        }
        else
        {
            timerText.color = Color.white;
        }
    }

    // Métodos públicos para compatibilidad
    public void ReiniciarTemporizador()
    {
        ResetTimerServerRpc();
    }

    public void DetenerTemporizador()
    {
        StopTimerServerRpc();
    }

    // Propiedades para acceso externo
    public bool IsRunning => isRunning.Value;
    public bool IsSuddenDeath => isSuddenDeath.Value;
    public float RemainingTime => remaining.Value;

    public override void OnDestroy()
    {
        base.OnDestroy();
        remaining.OnValueChanged -= OnRemainingChanged;
    }
}