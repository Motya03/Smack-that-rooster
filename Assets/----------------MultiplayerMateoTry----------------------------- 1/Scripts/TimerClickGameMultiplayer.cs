using Unity.Netcode;
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
        HideUIClientRpc();
    }

    public void StartTimer()
    {
        timeRemaining = duration;
        active = true;

        ShowUI();
        UpdateTimer(Mathf.CeilToInt(timeRemaining));
    }
    [ServerRpc(RequireOwnership = false)]
    public void StopTimerServerRpc()
    {
        active = false;
        HideUIClientRpc();
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
            HideUIClientRpc();

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
    [ClientRpc]
    private void HideUIClientRpc()
    {
        if (timerText != null)
            timerText.gameObject.SetActive(false);
    }
}
