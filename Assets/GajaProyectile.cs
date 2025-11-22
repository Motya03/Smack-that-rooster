using System;
using UnityEngine;

public class GajaProyectile : MonoBehaviour
{
    public GameObject prefabCruz;   // ← Tu cruz
    public float distanciaRaycast = 10f;
    public GameObject cajaRota;
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
            Destroy(exp, 0.5f);
            Destroy(this.gameObject);
        }
        else if (col.gameObject.tag == "Ground")
        {
            GameObject exp = Instantiate(cajaRota, this.transform.position, Quaternion.identity);
            GameObject explol = Instantiate(EfectoCajaRota, this.transform.position, Quaternion.identity);
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
      //  Debug.DrawRay(transform.position, Vector3.down * distanciaRaycast, Color.red);
       // if (!activate) return;
        activate = false;
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down , out hit, distanciaRaycast))
        {
            // Si queremos instanciar cuando toque el suelo
            if (hit.collider.CompareTag("Ground"))
            {
                // Instanciar la cruz en el punto exacto
                Instantiate(prefabCruz, hit.point, Quaternion.identity);

              
            }
        }
    }
}
