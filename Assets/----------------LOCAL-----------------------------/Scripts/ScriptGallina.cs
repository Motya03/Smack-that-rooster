using UnityEngine;

public class ScriptGallina : MonoBehaviour
{
    public float runSpeed = 2.0f;
    public float tiempoCambio = 4f;

    private Quaternion objetivoRotacion;
    private bool colisionando = false;

    public enum States { Movim, Dance }
    public States mystate;

    private Animator ani;
    private float cronometro;
    private int rutina;

    void Start()
    {
        ani = GetComponent<Animator>();
        SetState(States.Movim);
    }

    void Update()
    {
        switch (mystate)
        {
            case States.Movim: Comportamiento_Gallina(); break;
            case States.Dance: Dance(); break;
        }
    }

    private void Dance()
    {
        ani.Play("Dance");
        ani.SetBool("Run", false);
    }

    private void Comportamiento_Gallina()
    {
        cronometro += Time.deltaTime;

        if (cronometro >= tiempoCambio)
        {
            rutina = Random.Range(0, 3);  // 0 = idle, 1 = elegir dirección, 2 = mover
            cronometro = 0;
        }

        switch (rutina)
        {
            // -----------------------------
            // IDLE – NO SE MUEVE
            // -----------------------------
            case 0:
                ani.SetBool("Run", false);
                // Asegura que NO haya movimiento
                return;

            // -----------------------------
            // ELIJE DIRECCIÓN
            // -----------------------------
            case 1:
                ani.SetBool("Run", false);

                if (colisionando)
                {
                    float nuevoGrado = (transform.eulerAngles.y + 180f) % 360f;
                    objetivoRotacion = Quaternion.Euler(0, nuevoGrado, 0);
                    colisionando = false;
                }
                else
                {
                    float grado = Random.Range(0, 360);
                    objetivoRotacion = Quaternion.Euler(0, grado, 0);
                }

                rutina = 2; // pasar a mover
                break;

            // -----------------------------
            // RUN – SOLO AQUÍ SE MUEVE
            // -----------------------------
            case 2:
                ani.SetBool("Run", true);

                // Rotación gradual
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    objetivoRotacion,
                    120 * Time.deltaTime
                );

                // Movimiento SOLO EN RUN
                transform.Translate(Vector3.forward * runSpeed * Time.deltaTime);
                break;
        }
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("ColGallina"))
        {
            colisionando = true;
            rutina = 1; // Forzar cambio inmediato de dirección
        }
    }

    public void SetState(States newState)
    {
        mystate = newState;
    }
}
