using UnityEngine;

public class ScriptGallinaIdle : MonoBehaviour
{
  

    public enum States { Movim, Dance }
    public States mystate;

    private Animator ani;
 

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
        ani.Play("Idle");
    }


  

    public void SetState(States newState)
    {
        mystate = newState;
    }
}
