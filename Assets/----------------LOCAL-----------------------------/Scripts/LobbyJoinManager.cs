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
    [SerializeField] private GameObject[] healthBars;    // HealthUI1..4 (GameObjects con HealthSystem y HeartFlash)

    private PlayerInputManager inputManager;

    private void Awake()
    {
        inputManager = FindFirstObjectByType<PlayerInputManager>();

        if (inputManager == null)
        {
            Debug.LogError("❌ No se encontró ningún PlayerInputManager en la escena.");
            return;
        }

        inputManager.EnableJoining();

        startButton.gameObject.SetActive(false);
        gameplayCanvas.SetActive(false);

        foreach (var slot in playerSlots)
            slot.SetActive(false);

        foreach (var bar in healthBars)
            bar.SetActive(false);
    }

    public void OnPlayerJoinedVisual(int index)
    {
        if (index < playerSlots.Length)
        {
            playerSlots[index].SetActive(true);

            if (index >= 1)
                startButton.gameObject.SetActive(true);
        }
    }

    public void StartGame()
    {
        Debug.Log("🎮 Empieza la partida");

        if (inputManager != null)
            inputManager.DisableJoining();

        lobbyCanvas.SetActive(false);
        gameplayCanvas.SetActive(true);

        // Activar control de jugadores
        foreach (var player in PlayerSpawn.joinedPlayers)
            PlayerSpawn.TogglePlayerControl(player, true);

        // Activar las barras de vida y vincular cada barra al jugador correspondiente
        int playerCount = PlayerSpawn.joinedPlayers.Count;

        for (int i = 0; i < healthBars.Length; i++)
        {
            bool active = i < playerCount;
            healthBars[i].SetActive(active);

            if (active)
            {
                // Obtener el jugador i y su PlayerMovLocal
                var player = PlayerSpawn.joinedPlayers[i].GetComponent<PlayerMovLocal>();
                if (player == null)
                {
                    Debug.LogWarning($"Jugador en índice {i} no tiene PlayerMovLocal.");
                    continue;
                }

                // Obtener el HealthSystem (UI) del HealthUI correspondiente
                var uiHealth = healthBars[i].GetComponent<HealthSystem>();
                if (uiHealth == null)
                {
                    Debug.LogWarning($"HealthUI en índice {i} no tiene componente HealthSystem.");
                    continue;
                }

                // Asignar la referencia UI al jugador
                player.uiHealth = uiHealth;

                // Resetear la vida UI al inicio de la partida
                uiHealth.ResetHealth();

                Debug.Log($"Asignada barra de vida {healthBars[i].name} al jugador {player.gameObject.name}");
            }
        }

        Debug.Log($"❤️ Se activaron {playerCount} barras de vida.");
    }
}
