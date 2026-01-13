using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class MatchApi : MonoBehaviour
{
    [SerializeField] private string baseUrl = "http://localhost/unity_api/";

    public IEnumerator CreateMatch(string gameMode, string relayJoinCode)
    {
        if (Session.UserId <= 0)
        {
            Debug.LogError("CreateMatch: Session.UserId inválido. ¿Has hecho login?");
            yield break;
        }

        WWWForm form = new WWWForm();
        form.AddField("host_user_id", Session.UserId);
        form.AddField("game_mode", gameMode);
        form.AddField("relay_join_code", relayJoinCode ?? "");

        using var req = UnityWebRequest.Post(baseUrl + "create_match.php", form);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("CreateMatch error: " + req.error);
            yield break;
        }

        string json = req.downloadHandler.text;
        Debug.Log("CreateMatch response: " + json);

        if (json.Contains("\"status\":\"success\""))
        {
            long matchId = ExtractLong(json, "match_id");
            Session.CurrentMatchId = matchId;
            Debug.Log("MATCH CREADO. match_id=" + Session.CurrentMatchId);
        }
        else
        {
            Debug.LogError("CreateMatch failed: " + json);
        }
    }

    public IEnumerator EndMatch()
    {
        if (Session.CurrentMatchId <= 0)
        {
            Debug.LogWarning("EndMatch: no hay match activo.");
            yield break;
        }

        WWWForm form = new WWWForm();
        form.AddField("match_id", Session.CurrentMatchId.ToString());

        using var req = UnityWebRequest.Post(baseUrl + "end_match.php", form);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("EndMatch error: " + req.error);
            yield break;
        }

        Debug.Log("EndMatch response: " + req.downloadHandler.text);
        Session.CurrentMatchId = -1;
    }

    private static long ExtractLong(string json, string key)
    {
        int i = json.IndexOf($"\"{key}\":");
        if (i < 0) return -1;
        i += key.Length + 3;
        int j = i;
        while (j < json.Length && char.IsDigit(json[j])) j++;
        return long.TryParse(json.Substring(i, j - i), out var v) ? v : -1;
    }
}
