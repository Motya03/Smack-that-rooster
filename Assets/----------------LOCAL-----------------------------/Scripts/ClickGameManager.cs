using System.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;

public class ClickGameManager : MonoBehaviour
{
    public static ClickGameManager Instance { get; private set; }

    [Header("Battle Settings")]
    public Slider battleSlider;
    public float clickPower = 0.02f;
    public float decaySpeed = 0.3f;
    private float value = 0.5f;
    private bool active = false;
    public Text battleText;

    public PlayerMovLocal attacker;   // EL QUE HIZO KO
    public PlayerMovLocal knocked;    // EL KO

    [Header("Timer")]
    public TimerClickGame timerClickGame;

    public GameManagerLocal gamemanagerlocal;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (gamemanagerlocal == null)
            gamemanagerlocal = FindFirstObjectByType<GameManagerLocal>();
    }

    private void Update()
    {
        if (!active) return;

        value = Mathf.MoveTowards(value, 0.5f, decaySpeed * Time.deltaTime);
        battleSlider.value = value;
        battleText.text = $"{value * 100f:F0}%";

        if (value <= 0.01f)
            EndBattle(attacker);

        else if (value >= 0.99f)
            EndBattle(knocked);
    }

    // attacker = el que golpeó
    // knocked = el que llegó a 0 HP
    public void StartBattle(PlayerMovLocal atk, PlayerMovLocal kn)
    {
        MusicManager.StopMusic(MusicType.MainMenuBack);
        MusicManager.StopMusic(MusicType.FightMusic);
        MusicManager.StopMusic(MusicType.ClickerGameMusic);
       // MusicManager.StopMusic(MusicType.EnterCharMusic);
        MusicManager.PlayMusic(MusicType.ClickerGameMusic, 0.5f);
        attacker = atk;
        knocked = kn;

        // 🔥 Bloquear la lógica del GameManager
        gamemanagerlocal.SetClickerState(true);

        attacker.SetState(PlayerMovLocal.States.ClickBattle);
        knocked.SetState(PlayerMovLocal.States.ClickBattle);

        StartCoroutine(CanvasApear(1f));
    }
    

    private IEnumerator CanvasApear(float duration)
    {
        yield return new WaitForSeconds(duration);

        value = 0.5f;
        battleSlider.value = value;
        active = true;

        battleSlider.gameObject.SetActive(true);
        battleText.gameObject.SetActive(true);

        if (timerClickGame != null)
        {
            timerClickGame.OnTimerEnd = null;
            timerClickGame.OnTimerEnd += HandleTimerEnded;
            timerClickGame.gameObject.SetActive(true);
            timerClickGame.ReiniciarTemporizador();
        }
    }

    public void RegisterClick(PlayerMovLocal who)
    {
        if (!active) return;

        if (who == attacker) value -= clickPower;   // attacker empuja hacia 0%
        else if (who == knocked) value += clickPower; // knocked empuja hacia 100%

        value = Mathf.Clamp01(value);
        battleSlider.value = value;
        battleText.text = $"{value * 100f:F0}%";
    }

    public void EndBattle(PlayerMovLocal winner)
    {
        if (timerClickGame != null)
            timerClickGame.DetenerTemporizador();

        PlayerMovLocal loser = (winner == attacker) ? knocked : attacker;

        // ------------------------------------
        // SI GANA EL ATTACKER → KNOCKED MUERE
        // ------------------------------------
        if (winner == attacker)
        {
            knocked.isDefinitivelyDead = true;
            knocked.SetState(PlayerMovLocal.States.Dead);

            Debug.Log($"❌ {knocked.name} murió definitivamente por perder el click battle.");

            active = false;
            battleSlider.gameObject.SetActive(false);
            battleText.gameObject.SetActive(false);
            attacker.SetState(PlayerMovLocal.States.Idle);
            //attacker.CanReceive();
            winner.CageGone();

            // 🔥 Desbloquear y revisar win condition
            gamemanagerlocal.SetClickerState(false);
            gamemanagerlocal.CheckRemainingPlayers();
            MusicManager.StopMusic(MusicType.MainMenuBack);
            MusicManager.StopMusic(MusicType.FightMusic);
            MusicManager.StopMusic(MusicType.ClickerGameMusic);
           
            
            return;
           

        }

        // ------------------------------------
        // SI GANA EL KNOCKED → REVIVE
        // ------------------------------------
        if (winner == knocked)
        {
            attacker.SetState(PlayerMovLocal.States.Idle);
            knocked.lives--;   
            knocked.ResetVidas();
            knocked.SetState(PlayerMovLocal.States.Idle);
            //  knocked.CanReceive();
            // attacker.CanReceive();
            // knocked.ResetInputs();
            //PlayerMovLocal g = GetComponentInChildren<PlayerMovLocal>();
            attacker.AttackDone = true;
            attacker.ResetInputs();

            Debug.Log($"💖 {knocked.name} ganó su segunda oportunidad.");

            active = false;
            battleSlider.gameObject.SetActive(false);
            battleText.gameObject.SetActive(false);

            winner.CageGone();

            // 🔥 Se desbloquea el GameManager
            gamemanagerlocal.SetClickerState(false);
            return;
        }
    }

    private void HandleTimerEnded()
    {
        if (!active) return;

        PlayerMovLocal winner = (value >= 0.5f) ? knocked : attacker;
        EndBattle(winner);
    }
}
