using UnityEngine;

public class HitboxPower : MonoBehaviour
{
    public GameObject owner; // Jugador que lanza el ataque


    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("noo");
        Debug.Log($"KickHitbox tocó: {other.name}");

        if (other.gameObject == owner || other.transform.IsChildOf(owner.transform)) return;

        PlayerMovLocal player = other.GetComponentInParent<PlayerMovLocal>();

        
        if (other.gameObject.CompareTag("Pecho"))
        {
            Debug.Log("Stunned");

            player.TakeStun();
        }
    }

}
//if (other.gameObject == owner) return; // No golpearse a sí mismo

