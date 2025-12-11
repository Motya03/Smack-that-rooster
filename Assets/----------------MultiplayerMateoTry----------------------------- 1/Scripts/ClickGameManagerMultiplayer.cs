using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class ClickGameManagerMultiplayer : NetworkBehaviour
{
    public static ClickGameManagerMultiplayer Instance { get; private set; }

    [Header("UI")]
    public Slider battleSlider;
    public Text battleText;

    [Header("Battle Settings")]
    public float clickPower = 0.02f;
    public float decaySpeed = 0.3f;

    private float value = 0.5f;
    private bool active = false;

    [Header("Players")]
    private PlayerMovMultiplayer attacker;
    private PlayerMovMultiplayer knocked;

    [Header("Timer (LOCAL UI, NO NETWORKOBJECT)")]
    public TimerClickGameMultiplayer timerClickGame;

    public GameManageMultiplayer gamemanagerlocal;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (gamemanagerlocal == null)
            gamemanagerlocal = FindFirstObjectByType<GameManageMultiplayer>();
    }

    private void Update()
    {
        if (!active || !IsServer) return;

        value = Mathf.MoveTowards(value, 0.5f, decaySpeed * Time.deltaTime);
        UpdateSliderClientRpc(value);

        if (value <= 0.01f)
            EndBattleServer(attacker);
        else if (value >= 0.99f)
            EndBattleServer(knocked);
    }

    // =====================================================
    // START BATTLE
    // =====================================================
    public void StartBattle(PlayerMovMultiplayer atk, PlayerMovMultiplayer kn)
    {
        if (!IsServer)
        {
            StartBattleServerRpc(atk.NetworkObjectId, kn.NetworkObjectId);
            return;
        }

        StartBattleServer(atk, kn);
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartBattleServerRpc(ulong atkId, ulong knId)
    {
        attacker = NetworkManager.Singleton.SpawnManager.SpawnedObjects[atkId].GetComponent<PlayerMovMultiplayer>();
        knocked = NetworkManager.Singleton.SpawnManager.SpawnedObjects[knId].GetComponent<PlayerMovMultiplayer>();

        StartBattleServer(attacker, knocked);
    }

    private void StartBattleServer(PlayerMovMultiplayer atk, PlayerMovMultiplayer kn)
    {
        attacker = atk;
        knocked = kn;

        attacker.SetState(PlayerMovMultiplayer.States.ClickBattle);
        knocked.SetState(PlayerMovMultiplayer.States.ClickBattle);

        gamemanagerlocal.SetClickerState(true);
        value = 0.5f;

        StartBattleClientRpc();

        StartCoroutine(BeginAfterDelay());
    }

    private IEnumerator BeginAfterDelay()
    {
        yield return new WaitForSeconds(1f);

        active = true;

        // TIMER LOCAL
        timerClickGame.StartTimer();

        ShowUIClientRpc(true, value);
    }

    [ClientRpc]
    private void StartBattleClientRpc()
    {
        // Clientes ya tienen refs via RPC
    }

    // =====================================================
    // UI SYNC RPC
    // =====================================================
    [ClientRpc]
    private void ShowUIClientRpc(bool show, float startValue)
    {
        battleSlider.gameObject.SetActive(show);
        battleText.gameObject.SetActive(show);

        battleSlider.value = startValue;
    }

    [ClientRpc]
    private void UpdateSliderClientRpc(float v)
    {
        battleSlider.value = v;
        battleText.text = $"{v * 100f:F0}%";
    }

    // =====================================================
    // CLICK INPUT
    // =====================================================
    [ServerRpc(RequireOwnership = false)]
    public void RegisterClickServerRpc(ulong playerId)
    {
        if (!active) return;

        if (playerId == attacker.NetworkObjectId)
            value -= clickPower;

        if (playerId == knocked.NetworkObjectId)
            value += clickPower;

        value = Mathf.Clamp01(value);
        UpdateSliderClientRpc(value);
    }

    // =====================================================
    // END BATTLE (SERVER DECIDES)
    // =====================================================
    public void HandleTimerEndedServer()
    {
        if (!IsServer) return;

        PlayerMovMultiplayer winner =
            (value >= 0.5f) ? knocked : attacker;

        EndBattleServer(winner);
    }

    private void EndBattleServer(PlayerMovMultiplayer winner)
    {
        active = false;
        timerClickGame.StopTimer();

        PlayerMovMultiplayer loser = (winner == attacker) ? knocked : attacker;

        if (winner == attacker)
        {
            loser.isDefinitivelyDead = true;
            loser.SetState(PlayerMovMultiplayer.States.Dead);
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
}
