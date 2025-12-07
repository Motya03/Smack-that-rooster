using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

public class TimerClickGameMulti : NetworkBehaviour
{
    [Header("UI")]
    [SerializeField] private Text timerText; // Cambiado a TMP_Text para consistencia
    [SerializeField] private GameObject timerPanel;

    [Header("Configuración")]
    [SerializeField] private float duration = 15f; // segundos

    // NetworkVariables para sincronizar entre clientes
    private NetworkVariable<float> tiempoRestante = new NetworkVariable<float>(15f);
    private NetworkVariable<bool> temporizadorActivo = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> timerVisible = new NetworkVariable<bool>(false);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Suscribirse a cambios en las NetworkVariables
        tiempoRestante.OnValueChanged += OnTiempoRestanteChanged;
        temporizadorActivo.OnValueChanged += OnTemporizadorActivoChanged;
        timerVisible.OnValueChanged += OnTimerVisibleChanged;

        // Configurar UI inicial
        UpdateUI();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        // Desuscribirse de cambios
        tiempoRestante.OnValueChanged -= OnTiempoRestanteChanged;
        temporizadorActivo.OnValueChanged -= OnTemporizadorActivoChanged;
        timerVisible.OnValueChanged -= OnTimerVisibleChanged;
    }

    private void Update()
    {
        if (!IsServer) return;

        if (temporizadorActivo.Value)
        {
            // Solo el servidor actualiza el tiempo
            float newTime = tiempoRestante.Value - Time.deltaTime;
            tiempoRestante.Value = Mathf.Max(0f, newTime);

            if (tiempoRestante.Value <= 0)
            {
                temporizadorActivo.Value = false;
                OnTimerEndServer();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartTimerServerRpc()
    {
        ReiniciarTemporizadorServer();
    }

    [ServerRpc]
    public void StopTimerServerRpc()
    {
        DetenerTemporizadorServer();
    }

    private void ReiniciarTemporizadorServer()
    {
        if (!IsServer) return;

        tiempoRestante.Value = duration;
        temporizadorActivo.Value = true;
        timerVisible.Value = true;

        Debug.Log($"Temporizador multiplayer iniciado: {duration} segundos");
    }

    private void DetenerTemporizadorServer()
    {
        if (!IsServer) return;

        temporizadorActivo.Value = false;
        timerVisible.Value = false;

        Debug.Log("Temporizador multiplayer detenido");
    }

    private void OnTimerEndServer()
    {
        if (!IsServer) return;

        Debug.Log("¡El temporizador multiplayer ha finalizado!");

        // Notificar a ClickGameManagerMulti
        if (ClickGameManagerMulti.Instance != null)
        {
            // Forzar fin de batalla por tiempo
            ulong attackerId = ClickGameManagerMulti.Instance.GetAttackerId();
            ClickGameManagerMulti.Instance.EndBattleByTimeServerRpc(attackerId);
        }

        // Ocultar timer
        timerVisible.Value = false;
    }

    // Callbacks para cambios en NetworkVariables
    private void OnTiempoRestanteChanged(float oldValue, float newValue)
    {
        UpdateTimerText();
    }

    private void OnTemporizadorActivoChanged(bool oldValue, bool newValue)
    {
        UpdateUI();
    }

    private void OnTimerVisibleChanged(bool oldValue, bool newValue)
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (timerPanel != null)
            timerPanel.SetActive(timerVisible.Value);

        if (timerText != null)
            timerText.gameObject.SetActive(timerVisible.Value && temporizadorActivo.Value);

        if (timerVisible.Value && temporizadorActivo.Value)
        {
            UpdateTimerText();
        }
    }

    private void UpdateTimerText()
    {
        if (timerText != null && timerVisible.Value)
        {
            int segundos = Mathf.CeilToInt(tiempoRestante.Value);
            timerText.text = segundos.ToString("00");
        }
    }

    // Métodos públicos para consultar estado
    public bool IsTimerActive()
    {
        return temporizadorActivo.Value;
    }

    public float GetRemainingTime()
    {
        return tiempoRestante.Value;
    }

    // Para el caso especial cuando el tiempo se acaba
    public void ForceTimerEnd()
    {
        if (IsServer)
        {
            tiempoRestante.Value = 0;
            temporizadorActivo.Value = false;
        }
    }

    // Método para cambiar duración dinámicamente
    [ServerRpc(RequireOwnership = false)]
    public void SetDurationServerRpc(float newDuration)
    {
        duration = Mathf.Max(1f, newDuration);

        if (temporizadorActivo.Value)
        {
            tiempoRestante.Value = Mathf.Min(tiempoRestante.Value, duration);
        }
    }
}