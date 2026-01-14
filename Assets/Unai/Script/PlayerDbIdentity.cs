using Unity.Netcode;
using UnityEngine;

public class PlayerDbIdentity : NetworkBehaviour
{
    // Visible para todos, escrito solo por el server
    public NetworkVariable<int> DbUserId = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        // Solo el dueño (cliente) envía su userId real al servidor
        if (IsOwner)
        {
            if (Session.UserId > 0)
                SetDbUserIdServerRpc(Session.UserId);
            else
                Debug.LogWarning("PlayerDbIdentity: Session.UserId <= 0 (¿login no hecho?)");
        }
    }

    [ServerRpc(RequireOwnership = true)]
    private void SetDbUserIdServerRpc(int userId)
    {
        DbUserId.Value = userId;
    }
}
