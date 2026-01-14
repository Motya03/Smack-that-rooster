using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class RegisterManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField usernameField;
    public TMP_InputField passwordField;
    public TextMeshProUGUI resultText;

    [Header("API")]
    [SerializeField] private string registerUrl = "http://localhost/unity_api/register_duplicate.php";

    [System.Serializable]
    private class RegisterResponse
    {
        public string status;
        public int user_id;     // viene cuando status == "success"
        public string error;    // opcional si status == "failed"
    }

    public void StartRegister()
    {
        StartCoroutine(Register());
    }

    private IEnumerator Register()
    {
        string username = usernameField.text.Trim();
        string password = passwordField.text; // si quieres: .Trim()

        // (Opcional) validación básica
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            resultText.text = "Rellena usuario y contraseña.";
            yield break;
        }

        WWWForm form = new WWWForm();
        form.AddField("username", username);
        form.AddField("password", password);

        using (UnityWebRequest www = UnityWebRequest.Post(registerUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                resultText.text = "Error: " + www.error;
                yield break;
            }

            string raw = www.downloadHandler.text;
            Debug.Log("REGISTER RESPONSE RAW: " + raw);

            RegisterResponse res;
            try
            {
                res = JsonUtility.FromJson<RegisterResponse>(raw);
            }
            catch
            {
                resultText.text = "Respuesta inválida del servidor.";
                yield break;
            }

            if (res == null || string.IsNullOrEmpty(res.status))
            {
                resultText.text = "Respuesta inválida del servidor.";
                yield break;
            }

            switch (res.status)
            {
                case "success":
                    resultText.text = $"Register successful! (UserId: {res.user_id})";
                    Session.UserId = res.user_id;  // ✅ guardar sesión
                    // Si quieres: cargar escena de login o menú aquí
                    break;

                case "duplicate":
                    resultText.text = "Register failed! Username ya existe.";
                    break;

                default:
                    // "failed" u otros
                    resultText.text = string.IsNullOrEmpty(res.error)
                        ? "Register failed!"
                        : ("Register failed: " + res.error);
                    break;
            }
        }
    }
}
