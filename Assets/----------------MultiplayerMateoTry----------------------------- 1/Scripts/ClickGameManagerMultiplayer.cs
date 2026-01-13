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

       // float newValue = Mathf.MoveTowards(netValue.Value, 0.5f, decaySpeed * Time.deltaTime);
        //netValue.Value = newValue;

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
        if (cageDown) return;
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
        StartCoroutine(CanvasApearServerCoroutine(1));
    }
    [ClientRpc]
    private void SetPlayersStateClientRpc(ulong atkId, ulong knId, PlayerMovMultiplayer.States newState)
    {
        // Find the objects on the Client side using the Network ID
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
        // FIX: Only start the timer if we are activating the battle (state is true)
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
            return; // IMPORTANTÍSIMO: evitamos que siga ejecutando código abajo
        }

        else
        {
            // El jugador "knocked" ganó la batalla (sobrevivió)

            // 1. Lógica en Servidor
            knocked.ResetVidas();
            knocked.SetState(PlayerMovMultiplayer.States.Idle);

            // 2. Lógica en Cliente (LA SOLUCIÓN A TU PROBLEMA)
            ResetPlayerLivesClientRpc(knocked.NetworkObjectId);
           // knocked.lives--;
        }

        attacker.SetState(PlayerMovMultiplayer.States.Idle);

        // Sincronizar estado Idle del atacante en clientes también
        SetPlayersStateClientRpc(attacker.NetworkObjectId, 99999, PlayerMovMultiplayer.States.Idle); // Usamos un ID falso para el segundo parámetro o creamos un RPC individual
        if (gamemanagerlocal != null)
            gamemanagerlocal.PauseMainTimer(false);
        ShowUIClientRpc(false);
        
    }
    [ClientRpc]
    private void ResetPlayerLivesClientRpc(ulong playerId)
    {
        // Buscamos el objeto en el cliente usando su NetworkId
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerId, out NetworkObject playerObj))
        {
            var player = playerObj.GetComponent<PlayerMovMultiplayer>();
            if (player != null)
            {
                // Esto actualiza las vidas y la UI en el cliente local
                player.ResetVidas();
                // Aseguramos que el estado visual también se reinicie
                player.SetState(PlayerMovMultiplayer.States.Idle);
            }
        }
    }
}