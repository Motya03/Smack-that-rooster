using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System.Collections;

public class ClickGameManagerMulti : NetworkBehaviour
{
    public static ClickGameManagerMulti Instance { get; private set; }

    [Header("Battle Settings")]
    public Slider battleSlider;
    public float clickPower = 0.02f;
    public float decaySpeed = 0.3f;
    public Text battleText;

    [Header("Timer")]
    public TimerClickGameMulti timerClickGame;

    private NetworkVariable<float> battleValue = new NetworkVariable<float>(0.5f);
    private NetworkVariable<bool> isBattleActive = new NetworkVariable<bool>(false);
    private NetworkVariable<ulong> attackerId = new NetworkVariable<ulong>();
    private NetworkVariable<ulong> knockedId = new NetworkVariable<ulong>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (!IsServer) return;

        if (!isBattleActive.Value) return;

        // Decay towards center
        float newValue = Mathf.MoveTowards(battleValue.Value, 0.5f, decaySpeed * Time.deltaTime);
        battleValue.Value = newValue;

        // Check win conditions
        if (battleValue.Value <= 0.01f)
        {
            EndBattleServer(attackerId.Value);
        }
        else if (battleValue.Value >= 0.99f)
        {
            EndBattleServer(knockedId.Value);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartBattleServerRpc(ulong atkId, ulong knId)
    {
        if (isBattleActive.Value) return;

        attackerId.Value = atkId;
        knockedId.Value = knId;
        battleValue.Value = 0.5f;
        isBattleActive.Value = true;

        // Set players to ClickBattle state
        var attacker = GetPlayer(attackerId.Value);
        var knocked = GetPlayer(knockedId.Value);

        if (attacker != null) attacker.SetStateServerRpc(PlayerMovMultiplayer.States.ClickBattle);
        if (knocked != null) knocked.SetStateServerRpc(PlayerMovMultiplayer.States.ClickBattle);

        // Show UI to all clients
        StartBattleClientRpc();

        // Start timer if exists
        if (timerClickGame != null)
        {
            timerClickGame.StartTimerServerRpc();
        }
    }

    [ClientRpc]
    private void StartBattleClientRpc()
    {
        battleSlider.gameObject.SetActive(true);
        battleText.gameObject.SetActive(true);
        battleSlider.value = battleValue.Value;
        battleText.text = $"{battleValue.Value * 100f:F0}%";

        // Play music
        MusicManager.StopMusic(MusicType.MainMenuBack);
        MusicManager.StopMusic(MusicType.FightMusic);
        MusicManager.PlayMusic(MusicType.ClickerGameMusic, 0.5f);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RegisterClickServerRpc(ulong playerId)
    {
        if (!isBattleActive.Value) return;

        if (playerId == attackerId.Value)
        {
            battleValue.Value -= clickPower;
        }
        else if (playerId == knockedId.Value)
        {
            battleValue.Value += clickPower;
        }

        battleValue.Value = Mathf.Clamp01(battleValue.Value);

        // Update UI for all clients
        UpdateBattleUIClientRpc(battleValue.Value);
    }

    [ClientRpc]
    private void UpdateBattleUIClientRpc(float value)
    {
        battleSlider.value = value;
        battleText.text = $"{value * 100f:F0}%";
    }

    private void EndBattleServer(ulong winnerId)
    {
        if (!isBattleActive.Value) return;

        isBattleActive.Value = false;

        ulong loserId = (winnerId == attackerId.Value) ? knockedId.Value : attackerId.Value;

        var winner = GetPlayer(winnerId);
        var loser = GetPlayer(loserId);

        if (winnerId == attackerId.Value)
        {
            // Attacker wins - knocked dies
            if (loser != null)
            {
                loser.networkIsDefinitivelyDead.Value = true;
                loser.SetStateServerRpc(PlayerMovMultiplayer.States.Dead);
            }
        }
        else
        {
            // Knocked wins - gets another chance
            if (loser != null)
            {
                loser.networkLives.Value--;
                loser.networkVidas.Value = 3;
                loser.SetStateServerRpc(PlayerMovMultiplayer.States.Idle);
            }
        }

        // Winner exits click battle
        if (winner != null)
        {
            winner.SetStateServerRpc(PlayerMovMultiplayer.States.Idle);
            winner.CageGone();
        }

        // Hide UI
        EndBattleClientRpc();

        // Notify game manager to check remaining players
        GameManagerMultiplayer.Instance?.CheckRemainingPlayersServerRpc();
    }

    [ClientRpc]
    private void EndBattleClientRpc()
    {
        battleSlider.gameObject.SetActive(false);
        battleText.gameObject.SetActive(false);

        // Stop battle music
        MusicManager.StopMusic(MusicType.ClickerGameMusic);
    }

    private PlayerMovMultiplayer GetPlayer(ulong playerId)
    {
        if (NetworkManager.Singleton.ConnectedClients.ContainsKey(playerId))
        {
            return NetworkManager.Singleton.ConnectedClients[playerId]
                .PlayerObject.GetComponent<PlayerMovMultiplayer>();
        }
        return null;
    }
    // Añade este método en ClickGameManagerMulti.cs
    [ServerRpc(RequireOwnership = false)]
    public void EndBattleByTimeServerRpc(ulong defaultWinnerId)
    {
        if (!isBattleActive.Value) return;

        Debug.Log($"Batalla terminada por tiempo. Ganador por defecto: {defaultWinnerId}");

        // El atacante gana por defecto si se acaba el tiempo
        EndBattleServer(defaultWinnerId);
    }

    // Añade este método para obtener el ID del atacante
    public ulong GetAttackerId()
    {
        return attackerId.Value;
    }
}