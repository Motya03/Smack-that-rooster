using System;
using Unity.Netcode;
using UnityEngine;
using System.Collections;
using UnityEngine.UIElements;

public class GajaProyectileMultiplayer : NetworkBehaviour
{
    public GameObject prefabCruz;   // ← Tu cruz
    public float distanciaRaycast = 10f;
    public GameObject cajaRota;
    public GameObject prefabPowerUp;
    public GameObject EfectoCajaRota;
    Rigidbody rb;
    private bool activate = true;

    private bool RayoIntantiated = false;

    void OnCollisionEnter(Collision col)
    {

        if (col.transform.root.CompareTag("Player"))

        {
            Debug.Log("Colisionó con Player (hijo o padre)");
            PlayerMovMultiplayer player = col.gameObject.GetComponent<PlayerMovMultiplayer>();
            player.TakeStunLocal();
            GameObject exp = Instantiate(cajaRota, this.transform.position, Quaternion.identity);
            GameObject explol = Instantiate(EfectoCajaRota, this.transform.position, Quaternion.identity);
            RequestThrowPowerServerRpc();
            // GameObject powerUp = Instantiate(prefabPowerUp, this.transform.position, Quaternion.identity);

            Destroy(exp, 0.5f);
            Destroy(this.gameObject);
        }
        else if (col.gameObject.tag == "Ground")
        {
            GameObject exp = Instantiate(cajaRota, this.transform.position, Quaternion.identity);
            GameObject explol = Instantiate(EfectoCajaRota, this.transform.position, Quaternion.identity);
            //  GameObject powerUp = Instantiate(prefabPowerUp, this.transform.position, Quaternion.identity);
            RequestThrowPowerServerRpc();
            SoundManager.PlaySound(SoundType.BoxDestroyed);
            //Destroy(this.gameObject);
            DestroyCageServerRpc();
        }
    }
    [ServerRpc(RequireOwnership = false)]
    private void RequestThrowPowerServerRpc(ServerRpcParams serverRpcParams = default)
    {
        // El servidor inicia la corrutina

        // 1. INSTANCIAR (Solo ocurre en el servidor)
        //   GameObject cajaInstance = Instantiate(Box, spawnPos, Quaternion.identity);
        GameObject powerUp = Instantiate(prefabPowerUp, this.transform.position, Quaternion.identity);

        // 2. SPAWNEAR (Esto es lo que hace que se vea en todos los clientes)
        NetworkObject netObj = powerUp.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
            Debug.Log("🔥 Caja spawneada en red correctamente");
        }
        else
        {
            Debug.LogError("❌ El prefab de la caja NO tiene el componente NetworkObject");
        }
    }

    private IEnumerator ServerThrowPowerCoroutine()
    {

        yield return new WaitForSeconds(0.1f);
       // Vector3 spawnPos = transform.position + Vector3.up * 10f;

        // 1. INSTANCIAR (Solo ocurre en el servidor)
     //   GameObject cajaInstance = Instantiate(Box, spawnPos, Quaternion.identity);
        GameObject powerUp = Instantiate(prefabPowerUp, this.transform.position, Quaternion.identity);

        // 2. SPAWNEAR (Esto es lo que hace que se vea en todos los clientes)
        NetworkObject netObj = powerUp.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
            Debug.Log("🔥 Caja spawneada en red correctamente");
        }
        else
        {
            Debug.LogError("❌ El prefab de la caja NO tiene el componente NetworkObject");
        }
    }
    [ServerRpc]
    public void DestroyCageServerRpc()
    {
        Destroy(this.gameObject);
    }
   // [ServerRpc]
   
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        SoundManager.PlaySound(SoundType.BoxGoingDown);
    }

    
    void Update()
    {
        this.transform.forward = rb.linearVelocity.normalized;
        Rayo();
    }
    private void Rayo()
    {
        if (RayoIntantiated) return;
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
            RayoIntantiated = true;

            Debug.Log("✔ Cruz instanciada en: " + hit.point);
        }
        else
        {
            Debug.Log("❌ No se detectó Ground.");
        }
    }


}
