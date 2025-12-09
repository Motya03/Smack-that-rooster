using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ScriptGallinaIdleMultiplayer : MonoBehaviour
{
    public enum States { Movim, Dance, Attack }
    public States mystate;

    public Animator ani;

    public float throwSpeed = 10f;
    public bool CanThrow = false;

    public GameObject projectile;
    private GameObject enemy;
    
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
            
            enemy = players[0];
        }
        else
        {
           
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
        if (!CanThrow || enemy == null) return;

        Vector3 dir = (enemyPoint.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime);

        StartCoroutine(AttackSequence());
    }
    private IEnumerator AttackSequence()
    {
        for (int i = 0; i < 3; i++)
        {
            FindEnemy();   

            if (enemy != null)
            {
                Throw(); 
            }

            yield return new WaitForSeconds(1f); 
        }
    }
    void Throw()
    {

        SoundManager.PlaySound(SoundType.Throw);
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
        yield return new WaitUntil(() => check.gameStarted);    
        FindEnemy();  
    }

    public void SetState(States newState)
    {
        mystate = newState;
    }
}
