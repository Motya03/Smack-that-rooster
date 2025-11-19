using UnityEngine;

public class PowerMaizLocal : MonoBehaviour
{
    public float boostAmount = 3f;       // multiplicador de velocidad
    public float boostDuration = 5f;     // duración del efecto en segundos

    private void OnTriggerEnter(Collider other)
    {
        // Verifica si el objeto que toca es un player
        PlayerMovLocal player = other.GetComponent<PlayerMovLocal>();

        if (player != null)
        {
            player.ActivarSpeedBoost(boostAmount, boostDuration);

            // Destruye el maíz al ser recogido
            Destroy(gameObject);
        }
    }
}
