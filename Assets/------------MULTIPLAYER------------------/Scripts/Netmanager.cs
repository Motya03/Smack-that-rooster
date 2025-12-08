using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetManager : MonoBehaviour
{
    public Button serverBtn;
    public Button hostBtn;
    public Button clientBtn;
    public GameObject connectionPanel;
    public GameObject gameUI; // Referencia a UI del juego (opcional)

    private void Awake()
    {
        serverBtn.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartServer();
            connectionPanel.SetActive(false);
            if (gameUI != null)
                gameUI.SetActive(true);
        });
        hostBtn.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
            connectionPanel.SetActive(false);
        });
        clientBtn.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
            connectionPanel.SetActive(false);
            if (gameUI != null)
                gameUI.SetActive(true);
        });

        // Ocultar UI del juego al inicio
        if (gameUI != null)
            gameUI.SetActive(false);

        
    }

    
}