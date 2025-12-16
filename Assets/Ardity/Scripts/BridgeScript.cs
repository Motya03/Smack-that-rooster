using UnityEngine;
using System.Collections;

public class ArduinoLobbyBridge : MonoBehaviour
{
    // Ardity llama a esto automáticamente
    void OnMessageArrived(string msg)
    {
        string cleanMsg = msg.Trim();

        // Asumimos:
        // BLUE = Jugador 1 (Índice 0 en la lista)
        // RED  = Jugador 2 (Índice 1 en la lista)

        if (cleanMsg == "BLUE")
        {
            TryClickPlayer(0);
        }
        else if (cleanMsg == "RED")
        {
            TryClickPlayer(1);
        }
    }

    void TryClickPlayer(int playerIndex)
    {
        // 1. Validaciones de seguridad básicas
        if (ClickGameManager.Instance == null) return;

        // Verificamos si la lista de jugadores existe y tiene suficientes jugadores
        if (PlayerSpawn.joinedPlayers == null || playerIndex >= PlayerSpawn.joinedPlayers.Count)
            return;

        // 2. Obtener el objeto del jugador desde la lista estática
        GameObject playerObj = PlayerSpawn.joinedPlayers[playerIndex];

        if (playerObj == null) return;

        // 3. Obtener el script del jugador
        PlayerMovLocal playerScript = playerObj.GetComponent<PlayerMovLocal>();

        if (playerScript == null) return;

        // 4. Lógica de seguridad: Solo funciona si están en ClickBattle
        // Esto evita bugs si tocan el botón mientras están en el lobby o peleando normal
        if (playerScript.mystate == PlayerMovLocal.States.ClickBattle)
        {
            ClickGameManager.Instance.RegisterClick(playerScript);
        }
    }

    void OnConnectionEvent(bool success)
    {
        Debug.Log(success ? "Arduino Conectado (Bridge Dinámico)" : "Error Arduino");
    }
}