using UnityEngine;
using UnityEngine.UI;
//using UnityEngine.UIElements;

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

    public CageScript scriptCage;


    public PlayerMovLocal p1;
    public PlayerMovLocal p2;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {

       
        if (!active) return;

        value = Mathf.MoveTowards(value, 0.5f, decaySpeed * Time.deltaTime);
        battleSlider.value = value;
        battleText.text = value.ToString();



        if (value <= 0.01f)
            EndBattle(p2);
        else if (value >= 0.99f)
            EndBattle(p1);
        Debug.Log($"Value: {value} / Slider: {battleSlider.value}");
    }

    public void StartBattle(PlayerMovLocal player1, PlayerMovLocal player2)
    {
       
        p1 = player1;
        p2 = player2;
        p1.SetState(PlayerMovLocal.States.ClickBattle);
        p2.SetState(PlayerMovLocal.States.ClickBattle);
        value = 0.5f;
        battleSlider.value = value;
        active = true;
        battleSlider.gameObject.SetActive(true);
        battleText.gameObject.SetActive(true);
    }

    public void RegisterClick(PlayerMovLocal who)
    {
        if (!active) return;

        if (who == p1)
            value += clickPower;
        else if (who == p2)
            value -= clickPower;

        value = Mathf.Clamp01(value);
        battleSlider.value = value;
        battleText.text = value.ToString();
    }

    public void EndBattle(PlayerMovLocal winner)
    {
        PlayerMovLocal loser = (winner == p1) ? p2 : p1;
        if (loser != null) loser.SetState(PlayerMovLocal.States.Dead);
        if (winner != null) winner.SetState(PlayerMovLocal.States.Idle);
        active = false;
        battleSlider.gameObject.SetActive(false);
        battleText.gameObject.SetActive(false);
        Debug.Log($"Click battle won by: {winner.name}");
        CageScript script = scriptCage.GetComponent<CageScript>();
        script.ClickBattleEnd();
    }
}
