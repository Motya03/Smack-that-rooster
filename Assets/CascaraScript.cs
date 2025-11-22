using UnityEngine;

public class FadeMaterial : MonoBehaviour
{
    public Material transparentMaterial;
    public float fadeSpeed = 1f;

    private Renderer rend;
    private Material mat;
    private bool startFade = false;

    void Start()
    {
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
        }
    }
}
