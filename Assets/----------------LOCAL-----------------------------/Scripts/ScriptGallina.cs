using System.Collections;
using UnityEngine;



public class ScriptGallina : MonoBehaviour
{
    

    public float runSpeed = 2.0f;
    public float tiempoCambio = 4f;

    public Rigidbody rb;
    private float jumpForce = 2f; // Ajusta la altura
    private float jumpInterval = 0.5f; // Tiempo entre saltos
    private bool canJump = true;


    private Quaternion objetivoRotacion;

    private bool colisionando = false;
    private float cronometro;
    private int rutina;

    private GameObject enemy;
    public enum States { Movim, Dance }
    public States mystate;

    private Animator ani;
    private Transform enemyPoint;

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
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        PlayerMovLocal playerScript = player.GetComponent<PlayerMovLocal>();
        Transform targetPoint = playerScript.GallinaApunta;
        Vector3 direction = (targetPoint.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        float rotationSpeed = 2f;
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
        SmallJump();
        StartCoroutine(DanceCorutine());
        ani.Play("Dance");
       
    }
    void SmallJump()
    {
        if (canJump)
        {
            // Resetea la velocidad vertical antes de saltar
            Vector3 vel = rb.linearVelocity;
            vel.y = 0f;
            rb.linearVelocity = vel;

            // Aplica impulso hacia arriba
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);

            // Control de intervalo
            canJump = false;
            StartCoroutine(JumpCooldown());
        }
    }

    IEnumerator JumpCooldown()
    {
        yield return new WaitForSeconds(jumpInterval);
        canJump = true;
    }

    public void DanceState()
    {
        SetState(States.Dance);

     
    }
    private IEnumerator DanceCorutine()
    {

        yield return new WaitForSeconds(3f);
        SetState(States.Movim);
        ani.SetBool("Run", false);
        ani.Play("Idle");

    }
    private void Comportamiento_Gallina()
    {
        cronometro += Time.deltaTime;

        if (cronometro >= tiempoCambio)
        {
            rutina = Random.Range(0, 3);  
            cronometro = 0;
        }

        switch (rutina)
        {
            
            case 0:
                ani.SetBool("Run", false);
                
                return;

           
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

                rutina = 2; 
                break;

            
            case 2:
                ani.SetBool("Run", true);

                
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    objetivoRotacion,
                    120 * Time.deltaTime
                );

                
                transform.Translate(Vector3.forward * runSpeed * Time.deltaTime);
                break;
        }
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("ColGallina"))
        {
            colisionando = true;
            rutina = 1; 
        }
    }

    public void SetState(States newState)
    {
        mystate = newState;
    }
}
