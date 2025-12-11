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
        // Solo el servidor gestiona el valor y el tiempo
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
        // Obtenemos referencias en el servidor para lógica interna
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(atkId, out NetworkObject atkObj))
            attacker = atkObj.GetComponent<PlayerMovMultiplayer>();

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(knId, out NetworkObject knObj))
            knocked = knObj.GetComponent<PlayerMovMultiplayer>();

        if (attacker != null && knocked != null)
            StartBattleServer(attacker, knocked);
    }

    private void StartBattleServer(PlayerMovMultiplayer atk, PlayerMovMultiplayer kn)
    {
        attacker = atk;
        knocked = kn;

        // IMPORTANTE: NO llamamos a SetState aquí directamente porque fallará si no somos el Owner.
        // Lo delegamos al ClientRpc para que cada cliente lo haga.

        gamemanagerlocal.SetClickerState(true);
        value = 0.5f;

        // Enviamos la orden a los clientes con los IDs
        StartBattleClientRpc(atk.NetworkObjectId, kn.NetworkObjectId);

        StartCoroutine(BeginAfterDelay());
    }

    private IEnumerator BeginAfterDelay()
    {
        yield return new WaitForSeconds(1f);

        active = true;

        // TIMER LOCAL
        if (timerClickGame != null)
            timerClickGame.StartTimer();

        ShowUIClientRpc(true, value);
    }

    // MODIFICADO: Ahora recibe los IDs para configurar a los jugadores en los clientes
    [ClientRpc]
    private void StartBattleClientRpc(ulong atkId, ulong knId)
    {
        // Buscar objetos por ID
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(atkId, out NetworkObject atkObj))
            attacker = atkObj.GetComponent<PlayerMovMultiplayer>();

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(knId, out NetworkObject knObj))
            knocked = knObj.GetComponent<PlayerMovMultiplayer>();

        // Ahora que tenemos las referencias locales, cambiamos el estado.
        // El 'SetState' tiene un check 'if (!IsOwner) return'.
        // Al ejecutarse este RPC en TODOS los clientes:
        // - El cliente dueño del Atacante ejecutará SetState(ClickBattle) con éxito.
        // - El cliente dueño del Golpeado ejecutará SetState(ClickBattle) con éxito.
        // - Los terceros (observadores) no cambiarán el estado lógico, pero verán la UI del slider.

        if (attacker != null) attacker.SetState(PlayerMovMultiplayer.States.ClickBattle);
        if (knocked != null) knocked.SetState(PlayerMovMultiplayer.States.ClickBattle);
    }

    // =====================================================
    // UI SYNC RPC
    // =====================================================
    [ClientRpc]
    private void ShowUIClientRpc(bool show, float startValue)
    {
        if (battleSlider != null) battleSlider.gameObject.SetActive(show);
        if (battleText != null) battleText.gameObject.SetActive(show);

        if (battleSlider != null) battleSlider.value = startValue;
    }

    [ClientRpc]
    private void UpdateSliderClientRpc(float v)
    {
        if (battleSlider != null) battleSlider.value = v;
        if (battleText != null) battleText.text = $"{v * 100f:F0}%";
    }

    // =====================================================
    // CLICK INPUT
    // =====================================================
    [ServerRpc(RequireOwnership = false)]
    public void RegisterClickServerRpc(ulong playerId)
    {
        if (!active) return;
        if (attacker == null || knocked == null) return;

        if (playerId == attacker.NetworkObjectId)
            value -= clickPower;

        if (playerId == knocked.NetworkObjectId)
            value += clickPower;

        value = Mathf.Clamp01(value);
        UpdateSliderClientRpc(value); // Actualizar UI visualmente a todos
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
        if (timerClickGame != null) timerClickGame.StopTimer();

        PlayerMovMultiplayer loser = (winner == attacker) ? knocked : attacker;
        bool loserDies = false;

        // Lógica de juego en el servidor (Autoridad)
        if (winner == attacker)
        {
            loser.isDefinitivelyDead = true;
            // No seteamos el estado aquí directamente, lo mandamos por RPC
            gamemanagerlocal.CheckRemainingPlayers();
            loserDies = true;
        }
        else
        {
            knocked.lives--;
            // Si gestionas las vidas con NetworkVariable se actualiza solo, 
            // si no, deberías sincronizarlo, pero asumiremos que el estado basta.
            knocked.ResetVidas();
        }

        // Notificar a todos que terminó y qué hacer con los estados
        FinishBattleClientRpc(attacker.NetworkObjectId, knocked.NetworkObjectId, winner.NetworkObjectId, loserDies);

        ShowUIClientRpc(false, value);
    }

    [ClientRpc]
    private void FinishBattleClientRpc(ulong atkId, ulong knId, ulong winnerId, bool loserDies)
    {
        // Apagar UI
        if (battleSlider != null) battleSlider.gameObject.SetActive(false);
        if (battleText != null) battleText.gameObject.SetActive(false);

        // Recuperar referencias locales por seguridad
        PlayerMovMultiplayer atkLocal = null;
        PlayerMovMultiplayer knLocal = null;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(atkId, out NetworkObject atkObj))
            atkLocal = atkObj.GetComponent<PlayerMovMultiplayer>();

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(knId, out NetworkObject knObj))
            knLocal = knObj.GetComponent<PlayerMovMultiplayer>();

        if (atkLocal == null || knLocal == null) return;

        PlayerMovMultiplayer loser = (winnerId == atkId) ? knLocal : atkLocal;
        PlayerMovMultiplayer winner = (winnerId == atkId) ? atkLocal : knLocal;

        // APLICAR ESTADOS (Cada cliente aplicará el cambio si es Owner gracias al check en SetState)

        // 1. El ganador vuelve a Idle
        winner.SetState(PlayerMovMultiplayer.States.Idle);

        // 2. El perdedor muere o vuelve a Idle
        if (loserDies)
        {
            loser.isDefinitivelyDead = true;
            loser.SetState(PlayerMovMultiplayer.States.Dead);
        }
        else
        {
            // Caso donde el defensor gana (no muere nadie, el defensor pierde una vida "extra" pero sigue vivo)
            // Ojo: Ajusta esta lógica según tus reglas exactas. 
            // Tu código original hacía: knocked.lives--; knocked.ResetVidas(); knocked.SetState(Idle);

            // Si el que perdió NO muere definitivamente (ej. era el atacante y falló, o el defensor ganó)
            // En tu lógica original:
            // Si gana Atacante -> Defensor muere.
            // Si gana Defensor -> Defensor pierde vida (lives--) y resetea corazones? 
            // (Esto era un poco raro en tu código original, pero lo replico aquí para sincronización).

            // Asumiendo que ambos vuelven a Idle si nadie muere definitivamente:
            loser.SetState(PlayerMovMultiplayer.States.Idle);
        }

        // Limpiar jaula si existe
        if (atkLocal.IsOwner) atkLocal.CageGone();
        if (knLocal.IsOwner) knLocal.CageGone();
    }
}