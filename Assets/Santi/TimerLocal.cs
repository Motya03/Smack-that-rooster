using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class TimerLocal : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Text timerText;

    [Header("Configuración")]
    [SerializeField] private float duration = 60f; // segundos
    [SerializeField] private bool startOnAwake = true;

    [Header("Eventos")]
    [SerializeField] private UnityEvent onFinished;

    private float remaining;
    private Coroutine countdownCoroutine;
    private bool isRunning;

    private void Awake()
    {
        remaining = Mathf.Max(0f, duration);
    }

    private void Start()
    {
        UpdateDisplay(remaining);
        if (startOnAwake) StartTimer();
    }

    public void StartTimer()
    {
        if (isRunning) return;
        isRunning = true;
        if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
        countdownCoroutine = StartCoroutine(CountdownRoutine());
    }

    public void StopTimer()
    {
        if (!isRunning) return;
        isRunning = false;
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
    }

    public void ResetTimer()
    {
        StopTimer();
        remaining = Mathf.Max(0f, duration);
        UpdateDisplay(remaining);
    }

    public void SetDuration(float seconds, bool reset = true)
    {
        duration = Mathf.Max(0f, seconds);
        if (reset) ResetTimer();
    }

    public bool IsRunning => isRunning;
    public float RemainingTime => remaining;

    private IEnumerator CountdownRoutine()
    {
        while (remaining > 0f && isRunning)
        {
            remaining -= Time.deltaTime;
            UpdateDisplay(Mathf.Max(0f, remaining));
            yield return null;
        }

        isRunning = false;
        countdownCoroutine = null;
        UpdateDisplay(0f);
        onFinished?.Invoke();
    }

    private void UpdateDisplay(float seconds)
    {
        if (timerText == null)
        {
            Debug.LogWarning("TimerLocal: timerText no está asignado en el Inspector.");
            return;
        }
        else
        {
            int minutes = (int)(seconds / 60f);
            int secs = (int)(seconds % 60f);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, secs);
        }
    }
}
