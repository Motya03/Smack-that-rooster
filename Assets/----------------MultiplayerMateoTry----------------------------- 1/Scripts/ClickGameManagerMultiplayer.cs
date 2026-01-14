using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ClickGameManagerMultiplayer : NetworkBehaviour
{
    public static ClickGameManagerMultiplayer Instance { get; private set; }

    [Header("UI")]
    public Slider battleSlider;
    public Text battleText;

    [Header("Battle Settings")]
    public float clickPower = 0.02f;
    public float decaySpeed = 0.3f;

    private NetworkVariable<float> netValue = new NetworkVariable<float>(0.5f);

    private bool active = false;

    [Header("Players")]
    private PlayerMovMultiplayer attacker;
    private PlayerMovMultiplayer knocked;

    [Header("Timer")]
    public TimerClickGameMultiplayer timerClickGame;
    public GameManageMultiplayer gamemanagerlocal;

    [SerializeField] private GameObject cagePrefab;
    private bool cageDown = false;

    // --- NUEVA VARIABLE PARA GUARDAR LA JAULA ACTUAL ---
    private GameObject currentCageInstance;

    // -------------------------------------------------------
    // 📈 Stats helpers (server only)
    // -------------------------------------------------------
    private int GetDbUserId(PlayerMovMultiplayer p)
    {
        if (p == null) return 0;
        var id = p.GetComponent<PlayerDbIdentity>();
        return (id != null) ? id.DbUserId.Value : 0;
    }

    private void AddKnockoutStat(PlayerMovMultiplayer atk)
    {
        if (!IsServer) return;
        var statsApi = FindFirstObjectByType<StatsApi>();
        if (statsApi == null) return;

        int attackerUserId = GetDbUserId(atk);
        if (attackerUserId <= 0) return;

        // KO = mandar al clicker
        StartCoroutine(statsApi.AddCombatStats(attackerUserId, 0, 0, 1));
    }

    private void AddKillDeathStats(PlayerMovMultiplayer killer, PlayerMovMultiplayer dead)
    {
        if (!IsServer) return;
        var statsApi = FindFirstObjectByType<StatsApi>();
        if (statsApi == null) return;

        int killerUserId = GetDbUserId(killer);
        int deadUserId = GetDbUserId(dead);

        if (killerUserId > 0)
            StartCoroutine(statsApi.AddCombatStats(killerUserId, 1, 0, 0));

        if (deadUserId > 0)
            StartCoroutine(statsApi.AddCombatStats(deadUserId, 0, 1, 0));
    }

    // Evitar duplicar stats si llega el RPC 2 veces
    private bool statsCountedThisBattle = false;

    // -------------------------------------------------------
    // 🩸 Kill events helpers (server only)
    // -------------------------------------------------------
    private void LogKillEvent_Server(string cause, PlayerMovMultiplayer killer, PlayerMovMultiplayer victim)
    {
        if (!IsServer) return;
        var api = FindFirstObjectByType<KillEventsApi>();
        if (api == null) return;

        long matchId = Session.CurrentMatchId; // <- si tu Session usa otro nombre, cambia esto
        if (matchId <= 0) return;

        int killerId = GetDbUserId(killer); // puede ser 0 (si quieres permitir null)
        int victimId = GetDbUserId(victim);
        if (victimId <= 0) return;

        float t = Time.timeSinceLevelLoad;
        StartCoroutine(api.InsertKillEvent(matchId, killerId, victimId, t, cause));
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (gamemanagerlocal == null)
            gamemanagerlocal = FindFirstObjectByType<GameManageMultiplayer>();

        if (battleSlider) battleSlider.gameObject.SetActive(false);
        if (battleText) battleText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (active)
        {
            battleSlider.value = netValue.Value;
        }

        if (!IsServer || !active) return;

        if (netValue.Value <= 0.01f)
            EndBattleServer(attacker);
        else if (netValue.Value >= 0.99f)
            EndBattleServer(knocked);
    }

    public void StartBattle(PlayerMovMultiplayer atk, PlayerMovMultiplayer kn)
    {
        MusicManager.StopMusic(MusicType.MainMenuBack);
        MusicManager.StopMusic(MusicType.FightMusic);
        MusicManager.StopMusic(MusicType.ClickerGameMusic);
        MusicManager.PlayMusic(MusicType.ClickerGameMusic, 0.5f);

        attacker = atk;
        knocked = kn;

        StartBattleServerRpc(attacker.NetworkObjectId, knocked.NetworkObjectId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartBattleServerRpc(ulong attackerId, ulong knockedId)
    {
        if (cageDown || active) return;

        // ✅ Resolver referencias reales en el SERVIDOR
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(attackerId, out NetworkObject atkObj))
            attacker = atkObj.GetComponent<PlayerMovMultiplayer>();
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(knockedId, out NetworkObject knObj))
            knocked = knObj.GetComponent<PlayerMovMultiplayer>();

        if (attacker == null || knocked == null)
        {
            Debug.LogError("❌ StartBattleServerRpc: attacker o knocked es null (no encontrados por NetworkObjectId)");
            return;
        }

        statsCountedThisBattle = false;

        // Guardamos la referencia en la variable global de la clase 'currentCageInstance'
        currentCageInstance = Instantiate(cagePrefab, attacker.transform.position, Quaternion.identity);

        if (gamemanagerlocal != null)
            gamemanagerlocal.PauseMainTimer(true);

        NetworkObject netObj = currentCageInstance.GetComponent<NetworkObject>();
        if (netObj != null) netObj.Spawn();

        // 1. UPDATE SERVER STATE
        if (attacker != null) attacker.SetState(PlayerMovMultiplayer.States.ClickBattle);
        if (knocked != null) knocked.SetState(PlayerMovMultiplayer.States.ClickBattle);

        // 2. TELL CLIENTS TO UPDATE STATE
        SetPlayersStateClientRpc(attackerId, knockedId, PlayerMovMultiplayer.States.ClickBattle);
        cageDown = true;

        // ✅ Avisar al GameManager que el clicker está activo
        if (gamemanagerlocal != null)
            gamemanagerlocal.SetClickerState(true);

        // ✅ KO = mandar al clicker (solo si aún tenía "ticket")
        if (!statsCountedThisBattle && knocked.lives > 0)
        {
            AddKnockoutStat(attacker);

            // 🩸 Evento KO
            LogKillEvent_Server("knockout", attacker, knocked);

            statsCountedThisBattle = true;
        }

        StartCoroutine(CanvasApearServerCoroutine(1));
    }

    [ClientRpc]
    private void SetPlayersStateClientRpc(ulong atkId, ulong knId, PlayerMovMultiplayer.States newState)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(atkId, out NetworkObject atkObj))
        {
            atkObj.GetComponent<PlayerMovMultiplayer>().SetState(newState);
        }

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(knId, out NetworkObject knObj))
        {
            knObj.GetComponent<PlayerMovMultiplayer>().SetState(newState);
        }
    }

    private IEnumerator CanvasApearServerCoroutine(float duration)
    {
        yield return new WaitForSeconds(duration);

        netValue.Value = 0.5f;
        active = true;

        ShowUIClientRpc(true);
    }

    [ClientRpc]
    private void ShowUIClientRpc(bool state)
    {
        if (state && timerClickGame != null)
        {
            timerClickGame.StartTimer();
        }

        active = state;
        if (battleSlider) battleSlider.gameObject.SetActive(state);
        if (battleText) battleText.gameObject.SetActive(state);

        if (state && battleSlider) battleSlider.value = 0.5f;
    }

    public void PlayerClick(ulong myPlayerId)
    {
        if (active) RegisterClickServerRpc(myPlayerId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RegisterClickServerRpc(ulong playerId)
    {
        if (!active) return;

        float currentValue = netValue.Value;

        if (playerId == attacker.NetworkObjectId)
            currentValue -= clickPower;

        if (playerId == knocked.NetworkObjectId)
            currentValue += clickPower;

        netValue.Value = Mathf.Clamp01(currentValue);
    }

    public void HandleTimerEndedServer()
    {
        if (!IsServer) return;
        PlayerMovMultiplayer winner = (netValue.Value >= 0.5f) ? knocked : attacker;
        EndBattleServer(winner);
    }

    private void EndBattleServer(PlayerMovMultiplayer winner)
    {
        cageDown = false;
        active = false;
        if (timerClickGame) timerClickGame.StopTimerServerRpc();

        // --- Lógica de la Jaula (Tu código existente) ---
        if (currentCageInstance != null)
        {
            var cageScript = currentCageInstance.GetComponentInChildren<CageScriptMultiplayer>();
            if (cageScript != null) cageScript.ClickBattleEndClientRpc();
        }
        // ----------------------------------------------

        // Identificar perdedor
        PlayerMovMultiplayer loser = (winner == attacker) ? knocked : attacker;

        if (winner == attacker)
        {
            // El atacante ganó, el otro muere definitivamente (SERVER)
            loser.isDefinitivelyDead = true;

            // ✅ Stats: Kill para attacker + Death para loser
            AddKillDeathStats(attacker, loser);

            // 🩸 Evento kill por ganar clicker
            LogKillEvent_Server("kill_clicker", attacker, loser);

            // IMPORTANTE: marcar que el clicker ya no está activo
            if (gamemanagerlocal != null)
                gamemanagerlocal.SetClickerState(false);

            // Terminar partida de forma autoritativa y fiable
            var loserNetObj = loser.GetComponent<NetworkObject>();
            if (loserNetObj != null && gamemanagerlocal != null)
            {
                gamemanagerlocal.HandlePlayerDeathServer(loserNetObj.OwnerClientId);
            }
            else
            {
                Debug.LogError("❌ EndBattleServer: loserNetObj o gamemanagerlocal es null");
            }

            // (Opcional) para no dejar estados raros visualmente, puedes esconder UI del clicker
            ShowUIClientRpc(false);
            return;
        }
        else
        {
            // El jugador "knocked" ganó la batalla (sobrevivió)

            // 1. Lógica en Servidor
            knocked.ResetVidas();
            knocked.SetState(PlayerMovMultiplayer.States.Idle);

            // 2. Lógica en Cliente
            ResetPlayerLivesClientRpc(knocked.NetworkObjectId);

            // ✅ Consume su única segunda oportunidad
            knocked.lives--;
        }

        attacker.SetState(PlayerMovMultiplayer.States.Idle);

        // Sincronizar estado Idle del atacante en clientes también
        SetPlayersStateClientRpc(attacker.NetworkObjectId, 99999, PlayerMovMultiplayer.States.Idle);

        if (gamemanagerlocal != null)
            gamemanagerlocal.PauseMainTimer(false);

        // ✅ clicker termina
        if (gamemanagerlocal != null)
            gamemanagerlocal.SetClickerState(false);

        ShowUIClientRpc(false);

        // limpiar refs por seguridad
        attacker = null;
        knocked = null;
    }

    [ClientRpc]
    private void ResetPlayerLivesClientRpc(ulong playerId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerId, out NetworkObject playerObj))
        {
            var player = playerObj.GetComponent<PlayerMovMultiplayer>();
            if (player != null)
            {
                player.ResetVidas();
                player.SetState(PlayerMovMultiplayer.States.Idle);
            }
        }
    }
}
