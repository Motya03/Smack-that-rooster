using UnityEngine;

public class PowerMaizLocal : MonoBehaviour
{
    public float amount = 1f;       // multiplicador de velocidad
    public float boostDuration = 5f;
    public float amountBoost = 3f;
    // duración del efecto en segundos

    private void OnTriggerEnter(Collider other)
    {
        // Verifica si el objeto que toca es un player
        PlayerMovLocal player = other.GetComponent<PlayerMovLocal>();

        if (player != null)
        {
            player.ActivarSpeedBoost( amountBoost, boostDuration);

            // Destruye el maíz al ser recogido
            Destroy(gameObject);
        }
    }
}
