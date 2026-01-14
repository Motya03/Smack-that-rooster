
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class ChangeSceneBoton : MonoBehaviour
{
    public string nombreEscena;
    public float delay = 1.5f;

    public Animator imagenAnimator1; // arrastra tu primer Animator aquí desde el Inspector
    public Animator imagenAnimator2; // arrastra el segundo Animator

    [Header("DB Matches (opcional)")]
    [SerializeField] private bool createMatchBeforeLoad = false;
    [SerializeField] private bool endMatchBeforeLoad = false;
    [SerializeField] private string matchGameMode = "local"; // "local" o "online"
    [SerializeField] private bool useSessionRelayJoinCode = true;
    [SerializeField] private string relayJoinCodeOverride = "";

    public void CambioEscena()
    {
        MusicManager.StopMusic(MusicType.MainMenuBack);
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


        // --- DB (opcional): cerrar/crear match antes de cambiar de escena ---
        MatchApi matchApi = FindFirstObjectByType<MatchApi>();
        if (matchApi == null && (createMatchBeforeLoad || endMatchBeforeLoad))
        {
            Debug.LogWarning("ChangeSceneBoton: No se encontró MatchApi en escena. Se continúa sin DB.");
        }
        else if (matchApi != null)
        {
            if (endMatchBeforeLoad)
            {
                if (NetworkManager.Singleton == null || NetworkManager.Singleton.IsHost)
                    yield return StartCoroutine(matchApi.EndMatch());
            }


            if (createMatchBeforeLoad)
            {
                string code = relayJoinCodeOverride;
                if (string.IsNullOrWhiteSpace(code) && useSessionRelayJoinCode)
                {
                    // Si luego decides guardar el code en Session, aquí lo cogerías.
                    // Por ahora lo dejamos vacío para local.
                    code = "";
                }
                
                yield return StartCoroutine(matchApi.CreateMatch(matchGameMode, string.IsNullOrWhiteSpace(code) ? null : code));
            }
        }
                // ---------------------------------------------------------------



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
