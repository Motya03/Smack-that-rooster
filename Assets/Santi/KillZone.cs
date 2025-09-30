using UnityEngine;
using UnityEngine.Events;

public class KillZone : MonoBehaviour
{
    public UnityEvent<string> onPlayerFell; // pasa nombre o ID

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            onPlayerFell?.Invoke(other.name);
            // respawn simple al centro
            other.attachedRigidbody.linearVelocity = Vector3.zero;
            other.transform.position = new Vector3(Random.Range(-1f, 1f), 1.2f, Random.Range(-1f, 1f));
        }
    }
}
