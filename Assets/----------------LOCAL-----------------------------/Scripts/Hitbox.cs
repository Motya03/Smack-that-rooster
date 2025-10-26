using UnityEngine;

public class Hitbox : MonoBehaviour
{
    public GameObject owner; // Jugador que lanza el ataque

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"KickHitbox tocó: {other.name}");

        if (other.gameObject == owner || other.transform.IsChildOf(owner.transform)) return;

        PlayerMovLocal player = other.GetComponent<PlayerMovLocal>();
        if (player != null)
        {
            Debug.Log($"{other.name} recibió daño!");
            player.TakeHit();
        }
    }

}
//if (other.gameObject == owner) return; // No golpearse a sí mismo

