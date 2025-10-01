using UnityEngine;

public class PowerMaizPicante : MonoBehaviour
{
    public float boostAmount = 3f;       // multiplicador de velocidad
    public float boostDuration = 5f;     // duración del efecto en segundos

    private void OnTriggerEnter(Collider other)
    {
        // Verifica si el objeto que toca es un player
        playermov player = other.GetComponent<playermov>();

        if (player != null)
        {
            player.ActivarBoost(boostAmount, boostDuration);

            // Destruye el maíz al ser recogido
            Destroy(gameObject);
        }
    }
}
