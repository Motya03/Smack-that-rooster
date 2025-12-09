using Unity.Netcode;
using UnityEngine;

public class HitboxMultiplayer : NetworkBehaviour
{
    public NetworkObject ownerNetObj;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;  // Lógica autoritativa del servidor

        // Obtenemos el NetworkObject del objeto golpeado (buscando en padres por si golpea un hueso)
        var targetNO = other.GetComponentInParent<NetworkObject>();

        // Validaciones básicas
        if (targetNO == null || targetNO == ownerNetObj) return;

        var targetPlayer = targetNO.GetComponent<PlayerMovMultiplayer>();

        // Si no es un jugador, salimos
        if (targetPlayer == null) return;

        // Detectar zona de impacto
        if (other.CompareTag("Culo"))
        {
            // Llamamos a un método directo, NO a un RPC, porque ya estamos en el Server
            targetPlayer.ProcessDamageOnServer(1, ownerNetObj.NetworkObjectId);
        }
        else if (other.CompareTag("Pecho"))
        {
            targetPlayer.ProcessStunOnServer(ownerNetObj.NetworkObjectId);
        }
    }
}