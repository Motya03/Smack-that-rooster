using System.Collections;
using TMPro;

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class LoginManager : MonoBehaviour
{
    public TMP_InputField usernameField;
    public TMP_InputField passwordField;
    public TextMeshProUGUI resultText;

    public void StartLogin()
    {
        StartCoroutine(Login());
    }
    void LoadMenu()
    {
        SceneManager.LoadScene("Menu");
    }
    IEnumerator Login()
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
            }
            else
            {
                string responseText = www.downloadHandler.text;
                if (responseText.Contains("success"))
                {
                    resultText.text = "Login successful!";
                    Invoke(nameof(LoadMenu), 1.5f);
                }
                else
                {
                    resultText.text = "Login failed!";
                }
            }
        }
    }
}
