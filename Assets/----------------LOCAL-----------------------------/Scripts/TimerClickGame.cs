using UnityEngine;
using UnityEngine.UI;

public class TimerClickGame : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Text timerText;

    [Header("Configuración")]
    [SerializeField] private float duration = 60f; // segundos

    private float tiempoRestante;
    private bool temporizadorActivo = false;

    private void OnEnable()
    {
        // Reinicia automáticamente si se activa
        ReiniciarTemporizador();
    }

    private void Update()
    {
        if (!temporizadorActivo) return;

        tiempoRestante -= Time.deltaTime;
        if (tiempoRestante <= 0)
        {
            tiempoRestante = 0;
            temporizadorActivo = false;
            TemporizadorFinalizado();
        }

        ActualizarTextoTemporizador();
    }

    public void ReiniciarTemporizador()
    {
        tiempoRestante = duration;
        temporizadorActivo = true;

        // 🔹 Aseguramos que el texto esté visible
        if (timerText != null)
            timerText.gameObject.SetActive(true);

        ActualizarTextoTemporizador();
    }

    private void ActualizarTextoTemporizador()
    {
        int minutos = Mathf.FloorToInt(tiempoRestante / 60);
        int segundos = Mathf.FloorToInt(tiempoRestante % 60);

        if (timerText != null)
            timerText.text = string.Format("{0:00}:{1:00}", minutos, segundos);
    }

    private void TemporizadorFinalizado()
    {
        Debug.Log("¡El temporizador ha finalizado!");

        // 🔹 Oculta el texto al terminar
        if (timerText != null)
            timerText.gameObject.SetActive(false);
    }
}
