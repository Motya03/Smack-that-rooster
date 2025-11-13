using UnityEngine;
using UnityEngine.UI;

public class TimerClickGame : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Text timerText;

    [Header("Configuración")]
    [SerializeField] private float duration = 15f; // segundos

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
        int segundos = Mathf.CeilToInt(tiempoRestante); // redondea hacia arriba para que no muestre 59.9 → 59

        if (timerText != null)
            timerText.text = segundos.ToString("00"); // muestra por ejemplo: 05, 14, 32...
    }

    public System.Action OnTimerEnd; // <- Evento público para avisar cuando termina el tiempo

    private void TemporizadorFinalizado()
    {
        Debug.Log("¡El temporizador ha finalizado!");

        // 🔹 Avisamos al GameManager que terminó el tiempo
        OnTimerEnd?.Invoke();

        // 🔹 Ocultamos el texto
        if (timerText != null)
            timerText.gameObject.SetActive(false);
    }
    public void DetenerTemporizador()
    {
        temporizadorActivo = false;

        // Ocultamos el texto
        if (timerText != null)
            timerText.gameObject.SetActive(false);

        Debug.Log("Temporizador detenido manualmente.");
    }


}
