using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuControllerMultiplayer : MonoBehaviour
{
    public void CambiarEscena(string nombreS)
    {
        Debug.Log("Se llamó a CambiarEscena con: " + nombreS);
        SceneManager.LoadScene(nombreS);
    }
}
