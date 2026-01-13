using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class LoginManager : MonoBehaviour
{
    [System.Serializable]
    private class LoginResponse
    {
        public string status;   // campo público
        public int user_id;     // campo público (mismo nombre que el JSON)
    }

    public TMP_InputField usernameField;
    public TMP_InputField passwordField;
    public TextMeshProUGUI resultText;

    public void StartLogin()
    {
        StartCoroutine(Login());
    }

    private void LoadMenu()
    {
        SceneManager.LoadScene("Menu");
    }

    private IEnumerator Login()
    {
        WWWForm form = new WWWForm();
        form.AddField("username", usernameField.text);
        form.AddField("password", passwordField.text);

        using (UnityWebRequest www = UnityWebRequest.Post("http://localhost/unity_api/login.php", form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                resultText.text = "Error: " + www.error;
                yield break;
            }

            string responseText = www.downloadHandler.text;
            Debug.Log("LOGIN RESPONSE RAW: " + responseText);

            // Parse JSON
            LoginResponse data = null;
            try
            {
                data = JsonUtility.FromJson<LoginResponse>(responseText);
            }
            catch
            {
                data = null;
            }

            // ✅ Condición robusta (por si el JSON viene con espacios raros)
            bool success = (data != null && data.status == "success") || responseText.Contains("\"status\":\"success\"");

            if (success)
            {
                // Si parseó, guardamos el id
                if (data != null && data.user_id > 0)
                    Session.UserId = data.user_id;
                else
                    Session.UserId = ExtractInt(responseText, "user_id"); // fallback

                Debug.Log("LOGIN OK. UserId = " + Session.UserId);

                resultText.text = $"Login successful! (UserId: {Session.UserId})";
                Invoke(nameof(LoadMenu), 1.5f);
            }
            else
            {
                Debug.Log("LOGIN FAILED RESPONSE: " + responseText);
                resultText.text = "Login failed!";
            }
        }
    }

    // Extrae user_id si por lo que sea JsonUtility no parsea
    private int ExtractInt(string json, string key)
    {
        int i = json.IndexOf($"\"{key}\":");
        if (i < 0) return -1;
        i += key.Length + 3;
        int j = i;
        while (j < json.Length && char.IsDigit(json[j])) j++;
        return int.TryParse(json.Substring(i, j - i), out var v) ? v : -1;
    }
}
