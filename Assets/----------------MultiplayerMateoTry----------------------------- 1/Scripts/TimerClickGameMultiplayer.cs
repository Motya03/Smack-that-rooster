using UnityEngine;
using UnityEngine.UI;

public class TimerClickGameMultiplayer : MonoBehaviour
{
    [Header("UI")]
    public Text timerText;

    [Header("Config")]
    public float duration = 15f;

    private float timeRemaining;
    private bool active = false;

    private void Awake()
    {
        HideUI();
    }

    public void StartTimer()
    {
        timeRemaining = duration;
        active = true;

        ShowUI();
        UpdateTimer(Mathf.CeilToInt(timeRemaining));
    }

    public void StopTimer()
    {
        active = false;
        HideUI();
    }

    private void Update()
    {
        if (!active) return;

        timeRemaining -= Time.deltaTime;

        int t = Mathf.CeilToInt(timeRemaining);
        UpdateTimer(t);

        if (timeRemaining <= 0f)
        {
            active = false;
            HideUI();

            // Avisar al manager local
            ClickGameManagerMultiplayer.Instance.HandleTimerEndedServer();
        }
    }

    public void UpdateTimer(int seconds)
    {
        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
            timerText.text = seconds.ToString("00");
        }
    }

    private void ShowUI()
    {
        if (timerText != null)
            timerText.gameObject.SetActive(true);
    }

    private void HideUI()
    {
        if (timerText != null)
            timerText.gameObject.SetActive(false);
    }
}
