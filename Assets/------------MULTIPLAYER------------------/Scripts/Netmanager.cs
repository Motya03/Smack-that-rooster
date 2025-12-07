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

    void Start()
    {
        serverBtn.onClick.AddListener(StartServer);
        hostBtn.onClick.AddListener(StartHost);
        clientBtn.onClick.AddListener(StartClient);

        // Ocultar UI del juego al inicio
        if (gameUI != null)
            gameUI.SetActive(false);

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void StartServer()
    {
        connectionPanel.SetActive(false);
        if (gameUI != null)
            gameUI.SetActive(true);

        if (NetworkManager.Singleton.StartServer())
        {
            Debug.Log("Servidor iniciado en esta misma escena");
            SpawnPlayers(); // Método para spawnear jugadores
        }
    }

    private void StartHost()
    {
        connectionPanel.SetActive(false);
        if (gameUI != null)
            gameUI.SetActive(true);

        if (NetworkManager.Singleton.StartHost())
        {
            Debug.Log("Host iniciado en esta misma escena");
            SpawnPlayers(); // El host también es jugador
        }
    }

    private void StartClient()
    {
        connectionPanel.SetActive(false);
        if (gameUI != null)
            gameUI.SetActive(true);

        if (NetworkManager.Singleton.StartClient())
        {
            Debug.Log("Cliente conectando...");
        }
    }

    private void SpawnPlayers()
    {
        // Este método se llamará automáticamente cuando los jugadores se conecten
        // gracias al PlayerSpawnManager o ServerPlayerMove que ya tienes
        Debug.Log("Jugadores listos para spawnear");
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Cliente {clientId} conectado");
    }
}