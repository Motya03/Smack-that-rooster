using UnityEngine;

public class scriptProyectile : MonoBehaviour
{
    public GameObject explosion;
    Rigidbody rb;

    void OnCollisionEnter(Collision col)
    {

        if (col.transform.root.CompareTag("Player"))   
       
        {
            Debug.Log("Colisionó con Player (hijo o padre)");
            PlayerMovLocal player = col.gameObject.GetComponent<PlayerMovLocal>();
            player.TakeStun();
            GameObject exp = Instantiate(explosion, this.transform.position, Quaternion.identity);
            Destroy(exp, 0.5f);
            Destroy(this.gameObject);
        }
        else if (col.gameObject.tag == "Ground")
        {
            GameObject exp = Instantiate(explosion, this.transform.position, Quaternion.identity);
            //SoundManager.PlaySound(SoundType.RockHit);
            Destroy(this.gameObject);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.forward = rb.linearVelocity.normalized;
    }
}
