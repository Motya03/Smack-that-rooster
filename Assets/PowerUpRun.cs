using UnityEngine;
using System.Collections;

public class PowerUpRun : MonoBehaviour
{
    public float runSpeed = 2.0f;
    public float tiempoEntreCambios = 4f;
    public float tiempoEspera = 1f;

    public GameObject ParticleAbimalitoPrefab;
          
    
    public float amount = 1f;       
    public float boostDuration = 5f;
    public float amountBoost = 3f;
    private Quaternion angulo;
    private bool colisionando = false;

    void Start()
    {
        StartCoroutine(Comportamiento_Enemigo());
    }

    IEnumerator Comportamiento_Enemigo()
    {
        while (true)
        {
            
            if (colisionando)
            {
                float yActual = transform.eulerAngles.y;
                float nuevoGrado = (yActual + 180f) % 360f;
                angulo = Quaternion.Euler(0, nuevoGrado, 0);
                Debug.Log("💥 Choque detectado, nuevo ángulo: " + nuevoGrado);

                colisionando = false; 
            }
            else
            {
                
                float grado = Random.Range(0, 360);
                angulo = Quaternion.Euler(0, grado, 0);
                Debug.Log("Nuevo ángulo aleatorio: " + grado);
            }

           
            yield return new WaitForSeconds(tiempoEspera);

            
            float t = 0;
            while (t < tiempoEntreCambios)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, angulo, 120 * Time.deltaTime);
                transform.Translate(Vector3.forward * runSpeed * Time.deltaTime);
                t += Time.deltaTime;
                yield return null;
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("ColRing"))
        {
            colisionando = true;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        // Verifica si el objeto que toca es un player
        PlayerMovLocal player = other.GetComponentInParent<PlayerMovLocal>();

     
        if (other.CompareTag("Hitbox"))
        {
            if (player != null)
            {
                player.ActivarSpeedBoost( amountBoost, boostDuration);
                Instantiate(ParticleAbimalitoPrefab, transform.position, Quaternion.identity);
                // Destruye el maнz al ser recogido
                Destroy(gameObject);
            }
        }
    }
   

}
