using UnityEngine;
using Unity.Netcode;

public class HitboxMultiplayer : NetworkBehaviour
{
    public ulong ownerId; // ID del jugador que lanza el ataque
    public int damage = 1;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Hitbox tocó: {other.name}");

        // Solo el servidor procesa el daño
        if (!IsServer) return;

        // Evitar golpearse a sí mismo
        if (other.gameObject.CompareTag("Player"))
        {
            var player = other.GetComponentInParent<PlayerMovMultiplayer>();
            if (player != null && player.OwnerClientId == ownerId)
            {
                Debug.Log("Ignorando auto-golpe");
                return;
            }
        }

        // Comprobar si es el culo (daño)
        if (other.gameObject.CompareTag("Culo"))
        {
            PlayerMovMultiplayer playerHit = other.GetComponentInParent<PlayerMovMultiplayer>();
            if (playerHit != null && playerHit.OwnerClientId != ownerId)
            {
                Debug.Log($"{other.name} recibió daño!");

                // Aplicar daño al jugador golpeado
                playerHit.TakeHitServerRpc(damage, ownerId);

                // Desactivar hitbox después del golpe
                gameObject.SetActive(false);
            }
        }
        // Comprobar si es el pecho (stun)
        else if (other.gameObject.CompareTag("Pecho"))
        {
            PlayerMovMultiplayer playerHit = other.GetComponentInParent<PlayerMovMultiplayer>();
            if (playerHit != null && playerHit.OwnerClientId != ownerId)
            {
                Debug.Log("Stunned!");

                // Aplicar stun al jugador
                playerHit.SetStateServerRpc(PlayerMovMultiplayer.States.Stunned);

                // Desactivar hitbox después del stun
                gameObject.SetActive(false);
            }
        }
    }

    // Método para activar/desactivar la hitbox
    public void ActivateHitbox(bool activate)
    {
        gameObject.SetActive(activate);
    }

    // Método para setear el owner
    public void SetOwner(ulong ownerClientId)
    {
        ownerId = ownerClientId;
    }
}