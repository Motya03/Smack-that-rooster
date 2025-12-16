using UnityEngine;
using System.Collections;

public class ArduinoBridge : MonoBehaviour
{
    // Esta función la llama Ardity automáticamente
    void OnMessageArrived(string msg)
    {
        string cleanMsg = msg.Trim(); // Limpiar espacios

        if (cleanMsg == "BLUE")
        {
            // BLUE es el Jugador 1 (Índice 0 en la lista de spawn)
            SimularClickJugador(0);
        }
        else if (cleanMsg == "RED")
        {
            // RED es el Jugador 2 (Índice 1 en la lista de spawn)
            SimularClickJugador(1);
        }
    }

    void SimularClickJugador(int playerIndex)
    {
        // 1. Verificamos que el GameManager exista
        if (ClickGameManager.Instance == null) return;

        // 2. Buscamos al jugador en la lista estática del Spawn
        // Usamos PlayerSpawn.joinedPlayers porque ahí se guardan al crearse en el Lobby
        if (PlayerSpawn.joinedPlayers == null || playerIndex >= PlayerSpawn.joinedPlayers.Count)
            return;

        GameObject playerObj = PlayerSpawn.joinedPlayers[playerIndex];

        if (playerObj == null) return;

        // 3. Obtenemos su script
        PlayerMovLocal playerScript = playerObj.GetComponent<PlayerMovLocal>();

        if (playerScript == null) return;

        // 4. LÓGICA CLAVE: Solo enviamos el click si están en el minijuego
        // Si el jugador está corriendo o saltando, el botón del Arduino no hará nada.
        // Si está en 'States.ClickBattle', empujará el slider.
        if (playerScript.mystate == PlayerMovLocal.States.ClickBattle)
        {
            ClickGameManager.Instance.RegisterClick(playerScript);
        }
    }

    void OnConnectionEvent(bool success)
    {
        Debug.Log(success ? "Arduino Conectado" : "Error de conexión Arduino");
    }
}