using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TimerLocal : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Text timerText;

    [Header("Configuración")]
    [SerializeField] private float duration = 60f;
    [SerializeField] private bool startOnAwake = true;

    [Header("Eventos")]
    public UnityEvent onFinished;

    private float remaining;
    private bool isRunning;

    void Start()
    {
        remaining = duration;
        UpdateDisplay();
        if (startOnAwake) StartCoroutine(Countdown());
    }

    private IEnumerator Countdown()
    {
        isRunning = true;
        while (remaining > 0f)
        {
            remaining -= Time.deltaTime;
            UpdateDisplay();
            yield return null;
        }

        isRunning = false;
        remaining = 0f;
        UpdateDisplay();
        onFinished?.Invoke();
    }

    public void ResetTimer()
    {
        StopAllCoroutines();
        remaining = duration;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (!timerText) return;
        int minutes = Mathf.FloorToInt(remaining / 60f);
        int seconds = Mathf.FloorToInt(remaining % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }
}

