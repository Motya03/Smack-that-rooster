using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class ChangeSceneBoton : MonoBehaviour
{
    public string nombreEscena;
    public float delay = 1.5f;

    public Animator imagenAnimator1; // arrastra tu primer Animator aquí desde el Inspector
    public Animator imagenAnimator2; // arrastra el segundo Animator

    public void CambioEscena()
    {
        Debug.Log("CambioEscena ejecutado desde clic"); // 👈
        GameData.ResetGameState();

        if (imagenAnimator1 != null)
            imagenAnimator1.Play("Move");

        if (imagenAnimator2 != null)
            imagenAnimator2.Play("Move");

        StartCoroutine(EsperarYEjecutar());
    }

    IEnumerator EsperarYEjecutar()
    {
        yield return new WaitForSeconds(delay);

        if (string.IsNullOrEmpty(nombreEscena))
        {
            SalirDelJuego();
        }
        else
        {
            SceneManager.LoadScene(nombreEscena);
        }
    }

    void SalirDelJuego()
    {
        Application.Quit();
    }
}
