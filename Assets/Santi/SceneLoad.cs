using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoad : MonoBehaviour
{
    public void LoadScene(string SceneLocal)
    {
        SceneManager.LoadScene(SceneLocal);
        Debug.Log("Cargando escena: " + SceneLocal);
    }

    // Este método cierra el juego
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Saliendo del juego...");
    }
}
