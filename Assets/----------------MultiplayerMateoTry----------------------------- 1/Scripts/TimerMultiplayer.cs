using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TimerMultiplayer : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Text timerText;

    [Header("Configuración")]
    [SerializeField] private float duration = 60f;

    [Header("Eventos")]
    public UnityEvent onFinished;

    private float remaining;
    private bool isRunning;
    private Coroutine countdownCoroutine;
    private bool timePassed = false;
    private void Awake()
    {
        remaining = duration;
        UpdateDisplay();
        Debug.Log("⏱ [TimerMultiplayer] Awake, duración = " + duration);
    }

    public void StartTimer()
    {
        Debug.Log("▶️ [TimerMultiplayer] StartTimer() llamado");

        if (countdownCoroutine != null)
            StopCoroutine(countdownCoroutine);

        remaining = duration;
        isRunning = true;
        countdownCoroutine = StartCoroutine(Countdown());
    }

    private System.Collections.IEnumerator Countdown()
    {
        Debug.Log("🔁 [TimerMultiplayer] Empieza la cuenta regresiva");

        while (remaining > 0f)
        {
            remaining -= Time.deltaTime;
            UpdateDisplay();
            yield return null;
        }

        remaining = 0f;
        isRunning = false;
        UpdateDisplay();

        Debug.Log("⏰ [TimerMultiplayer] Tiempo terminado, disparando onFinished");
        onFinished?.Invoke();
    }

    public void ResetTimer()
    {
        Debug.Log("🔄 [TimerMultiplayer] ResetTimer() llamado");

        if (countdownCoroutine != null)
            StopCoroutine(countdownCoroutine);

        remaining = duration;
        isRunning = false;
        UpdateDisplay();
    }

    public void StartSuddenDeath()
    {
        Debug.Log("⚡ [TimerMultiplayer] StartSuddenDeath()");

        if (countdownCoroutine != null)
            StopCoroutine(countdownCoroutine);

        remaining = duration;
        isRunning = true;
        countdownCoroutine = StartCoroutine(Countdown());
    }

    private void UpdateDisplay()
    {
        if (!timerText)
        {
            Debug.LogWarning("⚠️ [TimerMultiplayer] timerText es NULL");
            return;
        }

        float t = Mathf.Max(remaining, 0);
        int m = Mathf.FloorToInt(t / 60f);
        int s = Mathf.FloorToInt(t % 60f);

        timerText.text = $"{m:00}:{s:00}";
        if (s == 30.0f && !timePassed)
        {

            //throwCageBool = true;
            FindAnyObjectByType<GameManageMultiplayer>().PrepareCage();
            timePassed = true;

        }
        if (s == 01.0f )
        {
            FindAnyObjectByType<GameManageMultiplayer>().TimeEnded();
        }
    }
}
