using UnityEngine;
using System.Collections;

public class ArduinoBridge : MonoBehaviour
{
    private SerialController serialController;
    private int lastSentPosition = -100;

    void Start()
    {
        serialController = GetComponent<SerialController>();

        // 1. PRUEBA DE CONEXIÓN AL INICIAR
        // Esto envía una señal de prueba nada más darle al Play.
        // Si las luces cambian al iniciar, la conexión está perfecta.
        if (serialController != null)
        {
            Debug.Log("🔌 Intentando enviar prueba de luces (SET:4)...");
            serialController.SendSerialMessage("SET:4\n");
        }
        else
        {
            Debug.LogError("❌ ERROR GRAVE: No encuentro el script SerialController. ¿Está en el mismo objeto?");
        }
    }

    void Update()
    {
        // ---------------------------------------------------------
        // PROTECCIÓN CONTRA ERRORES (NULL CHECK)
        // ---------------------------------------------------------

        // 1. ¿Existe el GameManager?
        if (ClickGameManager.Instance == null)
        {
            // Si no existe, no hacemos nada y esperamos al siguiente frame
            return;
        }

        // 2. ¿Tiene asignado el Slider? (Esta es la causa de tu error)
        if (ClickGameManager.Instance.battleSlider == null)
        {
            // Si llegamos aquí, es que se te olvidó arrastrar el Slider en el Inspector
            Debug.LogWarning("⚠️ AVISO: La variable 'Battle Slider' en ClickGameManager está vacía.");
            return;
        }

        // ---------------------------------------------------------
        // LÓGICA DEL JUEGO
        // ---------------------------------------------------------

        // Ahora es seguro acceder a .gameObject porque ya comprobamos que no es null
        bool isSliderActive = ClickGameManager.Instance.battleSlider.gameObject.activeInHierarchy;

        if (!isSliderActive)
        {
            // Si el slider está apagado, mandamos RESET (apagar luces) una vez
            if (lastSentPosition != -1)
            {
                if (serialController != null)
                    serialController.SendSerialMessage("RESET\n");

                lastSentPosition = -1;
            }
            return;
        }

        // Si el slider está activo, calculamos la luz
        float val = ClickGameManager.Instance.battleSlider.value;
        int ledIndex = Mathf.RoundToInt(val * 9);
        ledIndex = Mathf.Clamp(ledIndex, 0, 9);

        if (ledIndex != lastSentPosition)
        {
            string mensaje = "SET:" + ledIndex + "\n";

            if (serialController != null)
            {
                serialController.SendSerialMessage(mensaje);
                Debug.Log("✅ [ARDUINO] Enviando: " + mensaje.Trim());
            }

            lastSentPosition = ledIndex;
        }
    }

    // --- RECEPCIÓN DE INPUTS ---
    void OnMessageArrived(string msg)
    {
        string cleanMsg = msg.Trim();
        if (cleanMsg == "BLUE") TryClickPlayer(0);
        else if (cleanMsg == "RED") TryClickPlayer(1);
    }

    void TryClickPlayer(int playerIndex)
    {
        if (ClickGameManager.Instance == null || PlayerSpawn.joinedPlayers == null) return;
        if (playerIndex >= PlayerSpawn.joinedPlayers.Count) return;

        GameObject playerObj = PlayerSpawn.joinedPlayers[playerIndex];
        if (playerObj == null) return;

        PlayerMovLocal playerScript = playerObj.GetComponent<PlayerMovLocal>();
        if (playerScript != null && playerScript.mystate == PlayerMovLocal.States.ClickBattle)
        {
            ClickGameManager.Instance.RegisterClick(playerScript);
        }
    }
}