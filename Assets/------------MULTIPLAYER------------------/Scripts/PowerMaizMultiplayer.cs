using UnityEngine;

public class PowerMaizMultiplayer : MonoBehaviour
{
    public float boostAmount = 3f;       // multiplicador de velocidad
    public float boostDuration = 5f;     // duración del efecto en segundos

    private void OnTriggerEnter(Collider other)
    {
        // Verifica si el objeto que toca es un player
        PlayerMovMultiplayer player = other.GetComponent<PlayerMovMultiplayer>();

        if (player != null)
        {
            player.ActivarBoost(boostAmount, boostDuration);

            // Destruye el maíz al ser recogido
            Destroy(gameObject);
        }
    }
}
