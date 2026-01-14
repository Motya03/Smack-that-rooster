using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;

public class KillEventsApi : MonoBehaviour
{
    [SerializeField] private string baseUrl = "http://localhost/unity_api/";

    public IEnumerator InsertKillEvent(long matchId, int killerUserId, int victimUserId, float eventTimeSec, string cause)
    {
        WWWForm form = new WWWForm();

        // ✅ Convertir a string para evitar CS1503
        form.AddField("match_id", matchId.ToString());

        if (killerUserId > 0)
            form.AddField("killer_user_id", killerUserId.ToString());

        form.AddField("victim_user_id", victimUserId.ToString());

        // ✅ float en formato con punto (.)
        form.AddField("event_time_sec", eventTimeSec.ToString(CultureInfo.InvariantCulture));

        form.AddField("cause", cause ?? "");

        using var req = UnityWebRequest.Post(baseUrl + "insert_kill_event.php", form);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[KillEventsApi] Error: " + req.error);
            yield break;
        }

        Debug.Log("[KillEventsApi] OK: " + req.downloadHandler.text);
    }
}
