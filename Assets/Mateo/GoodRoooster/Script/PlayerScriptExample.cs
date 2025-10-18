using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerScriptExample : NetworkBehaviour
{
    private Rigidbody rb;

    private Vector2 moveInput; // <-- guardamos el input completo
    public float speed = 5f;
    public float jumpForce = 5f;

    private float defaultSpeed;
    private Coroutine boostCoroutine;
    private Animator myAnimator;

    public float lerpFactor = 0.1f;
    public enum States { Idle, AttackPatada, DashFront, Jump, DashBack, Run, Stunned, Dead }
    public States mystate;

    void Start()
    {
        myAnimator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        defaultSpeed = speed;
        mystate = States.Idle;
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;
        Vector3 targetVelocity = new Vector3(moveInput.x * speed, rb.linearVelocity.y, moveInput.y * speed);
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, lerpFactor);

        switch (mystate)
        {
            case States.Idle: Idle(); break;
            case States.Run: Run(); break;
            case States.AttackPatada: AttackPatada(); break;
            case States.DashFront: DashFront(); break;
            case States.Stunned: Stunned(); break;
            case States.Jump: Jump(); break;
            case States.DashBack: DashBack(); break;
            case States.Dead: Dead(); break;
            default: Debug.Log("Incorrect state"); break;
        }
    }

    private void SetState(States newstate)
    {
        mystate = newstate;
    }
    private void OnMove(InputValue movementValue)
    {
        moveInput = movementValue.Get<Vector2>(); // <-- siempre guarda el input más reciente
    }
    private void Idle()
    {
        myAnimator.Play("Idle");
        // transicion a run,  ataque,  dashback y dashfont.
      
    }
    private void Run()
    {
        // transicion a idle,  ataque,  dashback y dashfont. 
        myAnimator.SetBool("RUN", true);
        //if se cambia el estado a idle
        // myAnimator.SetBool("RUN", false);
        //if se cambia el estado a Attack
        // myAnimator.SetBool("Attack", true);
        //if se cambia el estado a Jump
        // myAnimator.SetBool("JUMP", true);
    }
    private void AttackPatada()
    {
        //transicion a idle al terminar la animacion
    }
    private void DashFront()
    {
        //transicion a idle 
    }
    private void Stunned()
    {
        //transicion a idle 
    }
    private void Jump()
    {
        //transicion a idle 
    }
    private void DashBack()
    {
        //transicion a idle 
    }
    private void Dead()
    {
        //trasicion a tu casa
    }

    private void OnJump(InputValue value)
    {
        if (value.isPressed && IsGrounded())
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, 1.1f);
    }

    // 👉 llamado desde el MaizPicante
    public void ActivarBoost(float boostAmount, float duration)
    {
        if (boostCoroutine != null)
            StopCoroutine(boostCoroutine);

        boostCoroutine = StartCoroutine(BoostCoroutine(boostAmount, duration));
    }

    private IEnumerator BoostCoroutine(float boostAmount, float duration)
    {
        speed = defaultSpeed * boostAmount;
        yield return new WaitForSeconds(duration);
        speed = defaultSpeed;
        boostCoroutine = null;
    }
}
