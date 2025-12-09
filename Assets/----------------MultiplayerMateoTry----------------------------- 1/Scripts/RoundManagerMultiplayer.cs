using System.Linq;
using UnityEngine;

public class RoundManagerMultiplayer : MonoBehaviour
{
    /*[Header("Referencias")]
    [SerializeField] private TimerLocal timer;

    private bool roundEnded = false;

    void Start()
    {
        if (timer != null)
            timer.onFinished.AddListener(EndRound);
    }

    void Update()
    {
        if (roundEnded) return;

        // Detecta si solo queda un jugador vivo
        int alivePlayers = FindObjectsOfType<PlayerStats>().Count(p => p.lives > 0);

        if (alivePlayers <= 1)
        {
            EndRound();
        }
    }

    private void EndRound()
    {
        if (roundEnded) return;
        roundEnded = true;

        Debug.Log("Fin de ronda");

        // Aquí pondrás tu lógica de resultado
        // Ejemplo:
        // int winnerID = DetermineWinner();
        // UIManager.Instance.ShowRoundEndPopup(winnerID);
    }*/
}
