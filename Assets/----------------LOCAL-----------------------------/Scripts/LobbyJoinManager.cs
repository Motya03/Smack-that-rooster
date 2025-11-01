using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class LobbyJoinManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject[] playerSlots;   // Casillas Jugador1–4
    [SerializeField] private Button startButton;         // Botón Start
    [SerializeField] private GameObject lobbyCanvas;     // Canvas Lobby (gris)
    [SerializeField] private GameObject gameplayCanvas;  // Canvas del juego (HUD, barras de vida)

    [Header("Gameplay UI")]
    [SerializeField] private GameObject[] healthBars;    // Barras de vida jugador1–4 dentro del GameplayCanvas

    private PlayerInputManager inputManager;

    private void Awake()
    {
        inputManager = Object.FindFirstObjectByType<PlayerInputManager>();
        if (inputManager == null)
        {
            Debug.LogError("❌ No se encontró ningún PlayerInputManager en la escena.");
            return;
        }

        // Activar uniones al iniciar
        inputManager.EnableJoining();

        // Estado inicial del lobby
        startButton.gameObject.SetActive(false);
        gameplayCanvas.SetActive(false);

        foreach (var slot in playerSlots)
            slot.SetActive(false);

        // Ocultar barras de vida al inicio
        foreach (var bar in healthBars)
            bar.SetActive(false);
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

        // 🔹 Desactivar nuevas uniones
        if (inputManager != null)
            inputManager.DisableJoining();

        // 🔹 Cambiar canvas
        lobbyCanvas.SetActive(false);
        gameplayCanvas.SetActive(true);

        // 🔹 Activar control de jugadores
        foreach (var player in PlayerSpawn.joinedPlayers)
        {
            PlayerSpawn.TogglePlayerControl(player, true);
        }

        // 🔹 Activar las barras de vida según la cantidad de jugadores
        int playerCount = PlayerSpawn.joinedPlayers.Count;

        for (int i = 0; i < healthBars.Length; i++)
        {
            bool active = i < playerCount;
            healthBars[i].SetActive(active);
        }

        Debug.Log($"❤️ Se activaron {playerCount} barras de vida.");
    }
}
