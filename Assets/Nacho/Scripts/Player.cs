using UnityEngine;
using TMPro;

public class Player : MonoBehaviour
{
    public int vidas = 2;
    public TMP_Text  vidasui;        
    public Vector3 spawn;
    public float speed = 5f;
    void Start()
    {
        spawn = transform.position;
        UpdatevidasUI();
    }

    public void TakeHit()
    {
        vidas--;

        if (vidas > 0)
        {
            Respawn();
        }
        else
        {
            Die();
        }
        UpdatevidasUI();
    }

    void Respawn()
    {
        transform.position = spawn;
        Debug.Log("Respawn: " + vidas + " vidas restantes");
    }

    void Die()
    {
        Debug.Log("Moriste");
        gameObject.SetActive(false); 
    }


    void UpdatevidasUI()
    {
        if (vidasui != null)
        {
            vidasui.text = "Vidas: " + vidas;
        }
    }
    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = new Vector3(h, 0, v);
        transform.Translate(move * speed * Time.deltaTime, Space.World);
    }

}
