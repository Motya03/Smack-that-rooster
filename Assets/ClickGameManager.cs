using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ClickGameManager : MonoBehaviour
{
    public Slider battleSlider;  // UI con valor de 0 a 1 (inicio en 0.5)
    public float clickPower = 0.02f;
    public float decaySpeed = 0.01f;
    public float winThreshold = 0.95f;

    private PlayerMovLocal player1;
    private PlayerMovLocal player2;
    private bool battleActive = false;
    private float battleValue = 0.5f; // Centro

    void Update()
    {
        if (!battleActive) return;

        if (Keyboard.current.jKey.wasPressedThisFrame)
            battleValue += clickPower;
        if (Keyboard.current.lKey.wasPressedThisFrame)
            battleValue -= clickPower;


        // --- Decaimiento suave (barra tiende al centro) ---
        if (battleValue > 0.5f)
            battleValue -= decaySpeed * Time.deltaTime;
        else if (battleValue < 0.5f)
            battleValue += decaySpeed * Time.deltaTime;

        // --- Clamp y UI ---
        battleValue = Mathf.Clamp01(battleValue);
        battleSlider.value = battleValue;

        // --- Verificación de victoria ---
        if (battleValue >= winThreshold)
            EndBattle(player1);
        else if (battleValue <= 1f - winThreshold)
            EndBattle(player2);
    }

    public void StartBattle(PlayerMovLocal p1, PlayerMovLocal p2)
    {
        player1 = p1;
        player2 = p2;

        battleValue = 0.5f;
        battleSlider.gameObject.SetActive(true);
        battleSlider.value = battleValue;

        battleActive = true;
        p1.SetState(PlayerMovLocal.States.ClickBattle);
        p2.SetState(PlayerMovLocal.States.ClickBattle);

        Debug.Log($"Click Battle iniciada entre {p1.name} y {p2.name}");
    }

    void EndBattle(PlayerMovLocal winner)
    {
        Debug.Log($"Click Battle ganada por {winner.name}");
        battleActive = false;
        battleSlider.gameObject.SetActive(false);

        // El ganador puede rematar, el perdedor muere
        PlayerMovLocal loser = (winner == player1) ? player2 : player1;
        loser.SetState(PlayerMovLocal.States.Dead);

        // Reiniciar al ganador al Idle
        winner.SetState(PlayerMovLocal.States.Idle);
    }
}
