using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class ClickGameManagerMultiplayer : NetworkBehaviour
{
    public static ClickGameManagerMultiplayer Instance { get; private set; }

    [Header("Battle Settings")]
    public Slider battleSlider;
    public float clickPower = 0.02f;
    public float decaySpeed = 0.3f;

    private float value = 0.5f;
    private bool active = false;
    public Text battleText;

    [Header("Players in battle")]
    private NetworkObjectReference attackerRef;
    private NetworkObjectReference knockedRef;

    private PlayerMovMultiplayer attacker;
    private PlayerMovMultiplayer knocked;

    [Header("Timer")]
    public TimerClickGameMultiplayer timerClickGame;

    public GameManageMultiplayer gamemanagerlocal;

    private void Awake()
    {
        if (Instance != null)
            Destroy(gameObject);
        else
            Instance = this;

        if (gamemanagerlocal == null)
            gamemanagerlocal = FindFirstObjectByType<GameManageMultiplayer>();
    }

    private void Update()
    {
        if (!active) return;
        if (!IsServer) return;   // 🔥 SOLO EL SERVER ACTUALIZA LA LÓGICA

        value = Mathf.MoveTowards(value, 0.5f, decaySpeed * Time.deltaTime);

        UpdateSliderClientRpc(value);

        if (value <= 0.01f)
            EndBattleServer(attacker);

        else if (value >= 0.99f)
            EndBattleServer(knocked);
    }

    // ---------------------------------------------------------
    // 🚀 LLAMADO DESDE PlayerMovMultiplayer (solo el owner)
    // ---------------------------------------------------------
    public void StartBattle(PlayerMovMultiplayer atk, PlayerMovMultiplayer kn)
    {
        if (!IsServer)
        {
            StartBattleServerRpc(atk.NetworkObjectId, kn.NetworkObjectId);
            return;
        }

        StartBattleServer(atk, kn);
    }

    // SERVER RPC
    [ServerRpc(RequireOwnership = false)]
    private void StartBattleServerRpc(ulong atkId, ulong knId)
    {
        PlayerMovMultiplayer atk = NetworkManager.Singleton.SpawnManager.SpawnedObjects[atkId].GetComponent<PlayerMovMultiplayer>();
        PlayerMovMultiplayer kn = NetworkManager.Singleton.SpawnManager.SpawnedObjects[knId].GetComponent<PlayerMovMultiplayer>();

        StartBattleServer(atk, kn);
    }

    // ---------------------------------------------------------
    // 👑 SERVER INICIA LA BATALLA
    // ---------------------------------------------------------
    private void StartBattleServer(PlayerMovMultiplayer atk, PlayerMovMultiplayer kn)
    {
        attacker = atk;
        knocked = kn;

        attackerRef = atk.NetworkObject;
        knockedRef = kn.NetworkObject;

        gamemanagerlocal.SetClickerState(true);

        attacker.SetState(PlayerMovMultiplayer.States.ClickBattle);
        knocked.SetState(PlayerMovMultiplayer.States.ClickBattle);

        value = 0.5f;

        StartBattleClientRpc(attacker.NetworkObjectId, knocked.NetworkObjectId);

        StartCoroutine(WaitStartBattle());
    }

    private IEnumerator WaitStartBattle()
    {
        yield return new WaitForSeconds(1f);

        active = true;
        timerClickGame.OnTimerEnd = HandleTimerEndedServer;
        timerClickGame.ReiniciarTemporizador();

        ShowUIClientRpc(true, value);
    }

    // ---------------------------------------------------------
    // 📡 CLIENT RPC = mostrar UI en todas las pantallas
    // ---------------------------------------------------------
    [ClientRpc]
    private void StartBattleClientRpc(ulong atkId, ulong knId)
    {
        attacker = NetworkManager.Singleton.SpawnManager.SpawnedObjects[atkId].GetComponent<PlayerMovMultiplayer>();
        knocked = NetworkManager.Singleton.SpawnManager.SpawnedObjects[knId].GetComponent<PlayerMovMultiplayer>();
    }

    [ClientRpc]
    private void ShowUIClientRpc(bool show, float startValue)
    {
        battleSlider.gameObject.SetActive(show);
        battleText.gameObject.SetActive(show);

        battleSlider.value = startValue;
        battleText.text = $"{startValue * 100f:F0}%";
    }

    // ---------------------------------------------------------
    // 📡 ACTUALIZAR SLIDER EN TODAS LAS MÁQUINAS
    // ---------------------------------------------------------
    [ClientRpc]
    private void UpdateSliderClientRpc(float v)
    {
        battleSlider.value = v;
        battleText.text = $"{v * 100f:F0}%";
    }

    // ---------------------------------------------------------
    // 📡 UN JUGADOR HACE CLICK → se manda al SERVER
    // ---------------------------------------------------------
    [ServerRpc(RequireOwnership = false)]
    public void RegisterClickServerRpc(ulong playerId)
    {
        if (!active) return;

        if (attacker == null || knocked == null) return;

        if (attacker.NetworkObjectId == playerId)
            value -= clickPower;

        if (knocked.NetworkObjectId == playerId)
            value += clickPower;

        value = Mathf.Clamp01(value);
        UpdateSliderClientRpc(value);
    }

    // ---------------------------------------------------------
    // 🏁 FIN DE BATALLA (solo el server decide)
    // ---------------------------------------------------------
    private void EndBattleServer(PlayerMovMultiplayer winner)
    {
        active = false;
        timerClickGame.DetenerTemporizador();

        PlayerMovMultiplayer loser = (winner == attacker) ? knocked : attacker;

        if (winner == attacker)
        {
            loser.isDefinitivelyDead = true;
            loser.SetState(PlayerMovMultiplayer.States.Dead);

            gamemanagerlocal.SetClickerState(false);
            gamemanagerlocal.CheckRemainingPlayers();
        }
        else
        {
            knocked.lives--;
            knocked.ResetVidas();
            knocked.SetState(PlayerMovMultiplayer.States.Idle);
        }

        attacker.SetState(PlayerMovMultiplayer.States.Idle);

        FinishBattleClientRpc();

        ShowUIClientRpc(false, value);
    }

    [ClientRpc]
    private void FinishBattleClientRpc()
    {
        battleSlider.gameObject.SetActive(false);
        battleText.gameObject.SetActive(false);
    }

    // Tiempo expiró → server decide ganador
    private void HandleTimerEndedServer()
    {
        if (!IsServer) return;

        PlayerMovMultiplayer winner = (value >= 0.5f) ? knocked : attacker;
        EndBattleServer(winner);
    }
}
