using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public void CambiarEscena(string nombreS)
    {
        Debug.Log("Se llamó a CambiarEscena con: " + nombreS);
        SceneManager.LoadScene(nombreS);
    }
}
