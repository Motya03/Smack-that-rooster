using UnityEngine;

public class FadeMaterial : MonoBehaviour
{
    public Material transparentMaterial;
    public float fadeSpeed = 1f;
    private Collider rb;


    public string fadeLayer = "IgnorePlayer";

    private Renderer rend;
    private Material mat;
    private bool startFade = false;

    void Start()
    {
        rb = GetComponent<SphereCollider>();
        rend = GetComponent<Renderer>();

        if (rend == null || transparentMaterial == null)
        {
            Debug.LogError("Falta Renderer o material transparente!");
            return;
        }

        // Instanciamos el material para este objeto
        mat = new Material(transparentMaterial);
        rend.material = mat;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            startFade = true; // activamos el fade
                              // rb.isTrigger = true; // 
            gameObject.layer = LayerMask.NameToLayer(fadeLayer);
        }
       
    }

    void Update()
    {
        if (startFade && mat != null)
        {
            Color c = mat.color;
            c.a -= fadeSpeed * Time.deltaTime;
            c.a = Mathf.Clamp01(c.a);
            mat.color = c;

            
            if (c.a <= 0f)
            {
                Destroy(this.gameObject);
            }
        }
    }

}
