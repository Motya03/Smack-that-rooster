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

        float newValue = Mathf.MoveTowards(netValue.Value, 0.5f, decaySpeed * Time.deltaTime);
        netValue.Value = newValue;

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

        StartBattleServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartBattleServerRpc(ServerRpcParams serverRpcParams = default)
    {
        // Guardamos la referencia en la variable global de la clase 'currentCageInstance'
        currentCageInstance = Instantiate(cagePrefab, attacker.transform.position, Quaternion.identity);

        NetworkObject netObj = currentCageInstance.GetComponent<NetworkObject>();
        if (netObj != null) netObj.Spawn();

        if (attacker != null)
            attacker.SetState(PlayerMovMultiplayer.States.ClickBattle);

        if (knocked != null)
            knocked.SetState(PlayerMovMultiplayer.States.ClickBattle);

        StartCoroutine(CanvasApearServerCoroutine(1));
    }

    private IEnumerator CanvasApearServerCoroutine(float duration)
    {
        yield return new WaitForSeconds(duration);

        netValue.Value = 0.5f;
        active = true;

        ShowUIClientRpc(true);

        if (timerClickGame != null) timerClickGame.StartTimer();
    }

    [ClientRpc]
    private void ShowUIClientRpc(bool state)
    {
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
        active = false;
        if (timerClickGame) timerClickGame.StopTimer();

        // --- AQUÍ ACCEDEMOS A LA JAULA GUARDADA ---
        if (currentCageInstance != null)
        {
            // Asegúrate de cambiar 'CageScript' por el nombre REAL de tu script en la jaula
            var cageScript = currentCageInstance.GetComponentInChildren<CageScriptMultiplayer>();

            if (cageScript != null)
            {
                // Llamamos a un ClientRpc dentro de la jaula o a la función directa
                // Si ClickBattleEnd hace cosas visuales, dentro de esa función 
                // en la jaula deberías llamar a un ClientRpc.
                cageScript.ClickBattleEnd();
            }
            else
            {
                Debug.LogError("No se encontró el script en la jaula instanciada");
            }
        }
        else
        {
            Debug.LogWarning("No hay una instancia de jaula guardada");
        }
        // ------------------------------------------

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

        ShowUIClientRpc(false);
    }
}