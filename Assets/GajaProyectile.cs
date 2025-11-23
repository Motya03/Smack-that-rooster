using System;
using UnityEngine;

public class GajaProyectile : MonoBehaviour
{
    public GameObject prefabCruz;   // ← Tu cruz
    public float distanciaRaycast = 10f;
    public GameObject cajaRota;
    public GameObject prefabPowerUp;
    public GameObject EfectoCajaRota;
    Rigidbody rb;
    private bool activate = true;

    void OnCollisionEnter(Collision col)
    {

        if (col.transform.root.CompareTag("Player"))

        {
            Debug.Log("Colisionó con Player (hijo o padre)");
            PlayerMovLocal player = col.gameObject.GetComponent<PlayerMovLocal>();
            player.TakeStun();
            GameObject exp = Instantiate(cajaRota, this.transform.position, Quaternion.identity);
            GameObject explol = Instantiate(EfectoCajaRota, this.transform.position, Quaternion.identity);
            GameObject powerUp = Instantiate(prefabPowerUp, this.transform.position, Quaternion.identity);
            Destroy(exp, 0.5f);
            Destroy(this.gameObject);
        }
        else if (col.gameObject.tag == "Ground")
        {
            GameObject exp = Instantiate(cajaRota, this.transform.position, Quaternion.identity);
            GameObject explol = Instantiate(EfectoCajaRota, this.transform.position, Quaternion.identity);
            GameObject powerUp = Instantiate(prefabPowerUp, this.transform.position, Quaternion.identity);
            //SoundManager.PlaySound(SoundType.RockHit);
            Destroy(this.gameObject);
        }
    }

    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    
    void Update()
    {
        this.transform.forward = rb.linearVelocity.normalized;
        Rayo();
    }
    private void Rayo()
    {
        activate = false;

        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * 1f;

        // Dibujar el Ray para depuración
        Debug.DrawRay(origin, Vector3.down * distanciaRaycast, Color.blue, 1f);

        // Solo detectar objetos con layer "Ground"
        int groundMask = LayerMask.GetMask("Ground");

        if (Physics.Raycast(origin, Vector3.down, out hit, distanciaRaycast, groundMask))
        {
            Instantiate(prefabCruz, hit.point + Vector3.up * 0.02f, Quaternion.identity);

            Debug.Log("✔ Cruz instanciada en: " + hit.point);
        }
        else
        {
            Debug.Log("❌ No se detectó Ground.");
        }
    }


}
