using UnityEngine;

public class ScriptGallinaIdle : MonoBehaviour
{
  

    public enum States { Movim, Dance, Attack }
    public States mystate;

    public Animator ani;
    public bool CanThrow = false;
    public GameObject projectile;
    private GameObject enemy;
    public float throwSpeed = 10f;
    
    public Transform posicionTiro;
    
  

 

    void Start()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        if (players.Length > 0) enemy = players[Random.Range(0, players.Length)];

        ani = GetComponent<Animator>();
        SetState(States.Movim);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            SetAttack();
        }
        
            if (enemy == null)
            {
                FindEnemy();
                return; // esperamos al siguiente frame
            }

            float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);

            switch (mystate)
            {
                case States.Movim: Comportamiento_Gallina(); break;
                case States.Dance: Dance(); break;
                case States.Attack: AttackState(); break;
            }
        

    }

    public void FindEnemy()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        if (players.Length > 0) enemy = players[Random.Range(0, players.Length)];

    }
    private void AttackState()
     {

        if (enemy == null) FindEnemy();
        if (!CanThrow || enemy == null) return;

        
        Vector3 dir = (enemy.transform.position - transform.position).normalized;
         Quaternion lookRotation = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z));
         transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime);

        Throw();
       //  ani.Play("Throw");





     }

    void Throw()
    {
      

        GameObject thrownProjectile = Instantiate(projectile, posicionTiro.position, Quaternion.identity);
        Rigidbody rb = thrownProjectile.GetComponent<Rigidbody>();

        Vector3 enemyPos = enemy.transform.position;
        Vector3 direction = (enemyPos - posicionTiro.position).normalized;

        // Bias opcional
        Vector3 leftBias = -transform.right * 0.2f;
        direction = (direction + leftBias).normalized;

        // Crear velocidad con arco
        Vector3 velocity = direction * throwSpeed;

        velocity.y += 6f;   // <-- Esto le da el arco

        // Nueva forma correcta
        rb.linearVelocity = velocity;

        Debug.DrawRay(posicionTiro.position, direction * 10f, Color.green, 2f);

        CanThrow = false;
        SetState(States.Movim);
    }


    Vector3 CalculateThrowVelocity(Vector3 direction)
     {

         return direction * throwSpeed;
     }
    public void SetAttack()
    {
        CanThrow = true;
        SetState(States.Attack);
    }
    
    private void Dance()
    {
        ani.Play("Dance");
        ani.SetBool("Run", false);
    }

    private void Comportamiento_Gallina()
    {
        ani.Play("Idle");
    }


  

    public void SetState(States newState)
    {
        mystate = newState;
    }
}
