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

        // ✅ REACTIVAR HUD del GameManager si estaba apagado
        var gm = FindFirstObjectByType<GameManagerLocal>();
        if (gm != null && gm.canvasLocal != null)
            gm.canvasLocal.SetActive(true);

        // ✅ ELIMINAR jugadores destruidos de la lista
        PlayerSpawn.joinedPlayers.RemoveAll(p => p == null);

        // ✅ ACTIVAR control para jugadores existentes
        foreach (var playerObj in PlayerSpawn.joinedPlayers)
        {
            if (playerObj == null) continue;
            PlayerSpawn.TogglePlayerControl(playerObj, true);
        }

        int playerCount = PlayerSpawn.joinedPlayers.Count;

        // ✅ ACTIVAR SOLO las barras necesarias y asignar salud
        for (int i = 0; i < healthBars.Length; i++)
        {
            bool active = i < playerCount;
            healthBars[i].SetActive(active);

            if (active)
            {
                var playerObj = PlayerSpawn.joinedPlayers[i];
                if (playerObj == null) continue;

                var player = playerObj.GetComponent<PlayerMovLocal>();
                if (player == null) continue;

                var uiHealth = healthBars[i].GetComponent<HealthSystem>();
                if (uiHealth == null) continue;

                player.uiHealth = uiHealth;
                uiHealth.ResetHealth();
            }
        }

        Debug.Log($"❤️ Activadas {playerCount} barras de vida.");

        // ✅ ACTIVAR GAME MANAGER
        gm?.ActivateGame();
    }




}
