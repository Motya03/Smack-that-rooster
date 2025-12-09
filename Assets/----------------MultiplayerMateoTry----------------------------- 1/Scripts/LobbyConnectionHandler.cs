using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class LobbyConnectionHandler : MonoBehaviour
{
    public static LobbyConnectionHandler Instance;

    private LobbyJoinManagerMultiplayer lobby;
    private List<ulong> connectedClients = new List<ulong>();

    private void Start()
    {
        Instance = this;

        lobby = FindFirstObjectByType<LobbyJoinManagerMultiplayer>();

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        // Evita duplicados
        if (!connectedClients.Contains(clientId))
            connectedClients.Add(clientId);

        int index = connectedClients.IndexOf(clientId);

        Debug.Log($"👤 Cliente conectado: {clientId} -> Slot {index}");

        lobby.OnPlayerJoinedVisual(index);
    }
}
