using System.Collections;
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

    public GameObject Cage;

    public static int lives;
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
        battleText.text = $"{value * 100f:F0}%";



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
        if (loser != null)
        {
            if (loser.vidas > 0)
            {
                
                loser.SetState(PlayerMovLocal.States.Idle);
                loser.ResetInputs();
            }
            // Si el jugador no tiene vidas y tampoco tiene "segunda oportunidad"
            else if (loser.vidas <= 0 )
            {
                loser.SetState(PlayerMovLocal.States.Dead);
                Debug.Log($"{loser.name} ha perdido y no tenía segunda oportunidad.");
            }


            // Si tenía vidas == 0 pero sí tenía una segunda oportunidad
           // else if ((loser.vidas <= 0 && loser.lives > 0))
          //  {
          //      // Pierde su segunda oportunidad
          //      loser.lives--;
          //      loser.ResetVidas(); // restaurar 3 vidas
          //      loser.SetState(PlayerMovLocal.States.Idle);
          //      Debug.Log($"{loser.name} usó su segunda oportunidad. Le quedan {loser.lives} oportunidades.");
          //  }
        }

        
   
            if ((winner.vidas <= 0 && winner.lives > 0))
            {
                winner.lives--;
                winner.ResetVidas(); // restaurar 3 vidas
                winner.SetState(PlayerMovLocal.States.Idle);
                winner.ResetInputs();
            }

        
        winner.ResetInputs();
        winner.SetState(PlayerMovLocal.States.Idle);
        active = false;
        battleSlider.gameObject.SetActive(false);
        battleText.gameObject.SetActive(false);
        Debug.Log($"Click battle won by: {winner.name}");
        if (winner != null)
            winner.CageGone();
    }
}
