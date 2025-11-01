using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class LobbyJoinManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject[] playerSlots;   // Casillas Jugador1–4
    [SerializeField] private Button startButton;         // Botón Start
    [SerializeField] private GameObject lobbyCanvas;     // Canvas Lobby (gris)
    [SerializeField] private GameObject gameplayCanvas;  // Canvas Local (juego)

    private PlayerInputManager inputManager;

    private void Awake()
    {
        inputManager = Object.FindFirstObjectByType<PlayerInputManager>();
        if (inputManager == null)
        {
            Debug.LogError("❌ No se encontró ningún PlayerInputManager en la escena.");
            return;
        }

        // Por si acaso, activamos las uniones al empezar el lobby
        inputManager.EnableJoining();

        // Estado inicial
        startButton.gameObject.SetActive(false);
        gameplayCanvas.SetActive(false);

        foreach (var slot in playerSlots)
            slot.SetActive(false);
    }

    public void OnPlayerJoinedVisual(int index)
    {
        if (index < playerSlots.Length)
        {
            playerSlots[index].SetActive(true);

            // Si hay al menos 2 jugadores, activar el botón Start
            if (index >= 1)
                startButton.gameObject.SetActive(true);
        }
    }

    public void StartGame()
    {
        Debug.Log("🎮 Empieza la partida");

        // 🔹 Desactivar uniones nuevas de jugadores
        if (inputManager != null)
            inputManager.DisableJoining();

        // 🔹 Ocultar lobby y mostrar el canvas del juego
        lobbyCanvas.SetActive(false);
        gameplayCanvas.SetActive(true);

        // 🔹 Activar el control de los jugadores ya spawneados
        foreach (var player in PlayerSpawn.joinedPlayers)
        {
            PlayerSpawn.TogglePlayerControl(player, true);
        }
    }
}



