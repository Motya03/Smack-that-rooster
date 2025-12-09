using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class TestingUiMultiplayer : MonoBehaviour
{
    [SerializeField] private Button startHostButton;
    [SerializeField] private Button startClientButton;
    [SerializeField] private LobbyJoinManagerMultiplayer lobbyManager; // Referencia al lobby manager

    private void Awake()
    {
        startHostButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
            Hide();

            // El host se registra inmediatamente
            if (lobbyManager != null)
                lobbyManager.OnPlayerJoinedVisual(0); // Host es jugador 0
        });

        startClientButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
            Hide();
        });
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}