using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class LobbyJoinManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject[] playerSlots;
    [SerializeField] private Button startButton;
    [SerializeField] private GameObject lobbyCanvas;
    [SerializeField] private GameObject gameplayCanvas;

    [Header("Gameplay UI")]
    [SerializeField] private GameObject[] healthBars;

    private PlayerInputManager inputManager;

    private void Awake()
    {
        inputManager = FindFirstObjectByType<PlayerInputManager>();

        if (inputManager == null)
        {
            Debug.LogError("❌ No se encontró PlayerInputManager.");
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

        foreach (var player in PlayerSpawn.joinedPlayers)
            PlayerSpawn.TogglePlayerControl(player, true);

        int playerCount = PlayerSpawn.joinedPlayers.Count;

        for (int i = 0; i < healthBars.Length; i++)
        {
            bool active = i < playerCount;
            healthBars[i].SetActive(active);

            if (active)
            {
                var player = PlayerSpawn.joinedPlayers[i].GetComponent<PlayerMovLocal>();
                var uiHealth = healthBars[i].GetComponent<HealthSystem>();

                player.uiHealth = uiHealth;
                uiHealth.ResetHealth();
            }
        }

        Debug.Log($"❤️ Activadas {playerCount} barras de vida.");

        // 🔥 ACTIVAR GameManagerLocal AHORA
        FindFirstObjectByType<GameManagerLocal>()?.ActivateGame();
    }
}
