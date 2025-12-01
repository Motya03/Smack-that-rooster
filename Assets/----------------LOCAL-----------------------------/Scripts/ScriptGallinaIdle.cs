using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

    private Transform enemyPoint;




    void Start()
    {
        StartCoroutine(WaitForGameStart());
        ani = GetComponent<Animator>();
        SetState(States.Movim);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            SetAttack();
            
        }
        
           

           // float distanceToEnemy = Vector3.Distance(transform.position, enemyPoint.position);

            switch (mystate)
            {
                case States.Movim: Comportamiento_Gallina(); break;
                case States.Dance: Dance(); break;
                case States.Attack: AttackState(); break;
            }
        

    }


    GameObject lastEnemy;

    public void FindEnemy()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        if (players.Length <= 1)
        {
            // Si solo hay uno, no se puede evitar
            enemy = players[0];
        }
        else
        {
            // Evita elegir el mismo enemigo
            List<GameObject> list = new List<GameObject>(players);
            list.Remove(lastEnemy);
            enemy = list[Random.Range(0, list.Count)];
        }

        lastEnemy = enemy;

        PlayerMovLocal p = enemy.GetComponent<PlayerMovLocal>();
        enemyPoint = p.GallinaApunta;
    }

    private void AttackState()
     {

         FindEnemy();
        if (!CanThrow || enemy == null) return;

        
        Vector3 dir = (enemyPoint.position - transform.position).normalized;
         Quaternion lookRotation = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z));
         transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime);

        Throw();
       //  ani.Play("Throw");





     }

    void Throw()
    {
      

        GameObject thrownProjectile = Instantiate(projectile, posicionTiro.position, Quaternion.identity);
        Rigidbody rb = thrownProjectile.GetComponent<Rigidbody>();

        Vector3 enemyPos = enemyPoint.position;
        Vector3 direction = (enemyPos - posicionTiro.position).normalized;   
        Vector3 velocity = direction * throwSpeed;

        float distance = Vector3.Distance(posicionTiro.position, enemyPos);   
        float arc = Mathf.Clamp(distance * 0.5f, 1f, 6f);

        velocity.y += arc;
        rb.linearVelocity = velocity;

        Debug.DrawRay(posicionTiro.position, direction * 10f, Color.green, 2f);
        CanThrow = false;
        SetState(States.Movim);
        enemy = null;      
        enemyPoint = null;
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


      public IEnumerator WaitForGameStart()
    {
       
        GameManagerLocal check = Object.FindAnyObjectByType<GameManagerLocal>();

        
        // Espera hasta que el juego empiece
        yield return new WaitUntil(() => check.gameStarted);
        
        FindEnemy();  // ya hay players en escena
    }

    public void SetState(States newState)
    {
        mystate = newState;
    }
}
