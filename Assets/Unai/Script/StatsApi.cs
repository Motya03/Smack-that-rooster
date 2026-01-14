using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class StatsApi : MonoBehaviour
{
    [SerializeField] private string baseUrl = "http://localhost/unity_api/";

    public IEnumerator UpdateStatsForMatch(List<int> playerIds, int winnerUserId)
    {
        string ids = string.Join(",", playerIds);

        WWWForm form = new WWWForm();
        form.AddField("player_ids", ids);
        form.AddField("winner_user_id", winnerUserId);

        using var req = UnityWebRequest.Post(baseUrl + "update_player_stats_match.php", form);
        yield return req.SendWebRequest();

        Debug.Log($"[STATS API] HTTP={(int)req.responseCode} result={req.result} error={req.error} body={req.downloadHandler.text}");
    }

    public IEnumerator AddCombatStats(int userId, int killsAdd, int deathsAdd, int knockoutsAdd)
    {
        WWWForm form = new WWWForm();
        form.AddField("user_id", userId);
        form.AddField("kills_add", killsAdd);
        form.AddField("deaths_add", deathsAdd);
        form.AddField("knockouts_add", knockoutsAdd);

        using var req = UnityWebRequest.Post(baseUrl + "update_player_combat_stats.php", form);
        yield return req.SendWebRequest();

        Debug.Log($"[STATS API] AddCombatStats HTTP={(int)req.responseCode} result={req.result} error={req.error} body={req.downloadHandler.text}");
    }


}
