using Unity.Netcode;
using UnityEngine;

public class HitboxPowerMultiplayer : MonoBehaviour
{
    public NetworkObject netOwner; // Jugador que lanza el ataque


    private void OnTriggerEnter(Collider other)
    {
        // Obtenemos el NetworkObject del objeto golpeado (buscando en padres por si golpea un hueso)
        var targetNO = other.GetComponentInParent<NetworkObject>();

        // Validaciones básicas
        if (targetNO == null || targetNO == netOwner) return;

        var targetPlayer = targetNO.GetComponent<PlayerMovMultiplayer>();
        if (targetPlayer == null) return;

        if (other.gameObject.CompareTag("Pecho"))
        {
            Debug.Log("Stunned");

            targetPlayer.TakeStunLocal();
        }
    }

}
//if (other.gameObject == owner) return; // No golpearse a sн mismo

