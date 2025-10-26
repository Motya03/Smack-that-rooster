using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.Rendering.DebugUI;


public class PlayerMovLocal : MonoBehaviour
{
    private CharacterController controller;
    private Animator myAnimator;

    private Vector2 lastMoveInput;
    private Vector2 moveInput;
    private Vector3 direction;
    private Vector3 velocity;
    private Vector3 airMomentum;
   [SerializeField] private bool isGrounded;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float gravity = -9.81f;
    public float jumpForce = 0.1f;
    public float airControl = 0.2f;
    public float smoothTime = 0.1f;

    private float currentVelocity;
    private float defaultSpeed;
    private Coroutine boostCoroutine;

    private bool isMoving;
    private bool isAttacking;
    private bool isJumpPressed;
    private bool isCrouchPressed;
    private bool dashFrontPressed;
    private bool dashBackPressed;
    private bool canReceiveInput = true;


    
    public enum States { Idle, Run, AttackPatada, Jump, DashFront, DashBack, Stunned, Dead, Crouch, AttackLow }
    public States mystate;

    public Transform model; // Para rotar solo el modelo visual

    void Start()
    {
        controller = GetComponent<CharacterController>();
        myAnimator = GetComponent<Animator>();
        defaultSpeed = moveSpeed;
        mystate = States.Idle;
    }

    private void Update()
    {
        

        // --- Verificar si está tocando el suelo ---
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f; // Mantener al personaje pegado al suelo

        // --- Dirección del input ---
        Vector3 inputDir = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        if (isGrounded)
        {
            direction = inputDir;

            // Guardar inercia solo si hay movimiento
            if (direction.magnitude > 0.1f)
                airMomentum = direction * moveSpeed;
            else
                airMomentum = Vector3.zero;
        }
        else
        {
            // En el aire: mezcla entre inercia y control del jugador
            direction = Vector3.Lerp(airMomentum.normalized, inputDir, airControl).normalized;
        }

        // --- Movimiento horizontal y rotación ---
        if (direction.sqrMagnitude > 0.01f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref currentVelocity, smoothTime);

            if (model != null)
                model.rotation = Quaternion.Euler(0f, angle, 0f);
            else
                transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir * moveSpeed * Time.deltaTime);
        }

        // --- Movimiento vertical y gravedad ---
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // --- Máquina de estados ---
        switch (mystate)
        {
            case States.Idle: Idle(); break;
            case States.Run: Run(); break;
            case States.AttackPatada: AttackPatada(); break;
            case States.AttackLow: AttackLow(); break;
            case States.Jump: Jump(); break;
            case States.DashFront: DashFront(); break;
            case States.DashBack: DashBack(); break;
            case States.Stunned: Stunned(); break;
            case States.Dead: Dead(); break;
            case States.Crouch: Crouch(); break;

        }
    }

    // --- INPUTS DEL NEW INPUT SYSTEM ---
    private void OnMove(InputValue value)
    {
        Vector2 currentInput = value.Get<Vector2>();
        lastMoveInput = currentInput;

        if (!canReceiveInput) return;

        moveInput = currentInput;
        isMoving = moveInput.magnitude > 0.1f;
    }
    private void OnCrouch(InputValue value)
    {
        if (!canReceiveInput) return;
        if (value.isPressed)
            isCrouchPressed = true;
    }

    private void OnJump(InputValue value)
    {
        if (!canReceiveInput) return;
        if (value.isPressed)
            isJumpPressed = true;
    }

    private void OnAttack(InputValue value)
    {
        if (!canReceiveInput) return;
        if (value.isPressed)
            isAttacking = true;
    }
    private void OnAttackLow(InputValue value)
    {
        if (!canReceiveInput) return;
        if (value.isPressed)
            isAttacking = true;
    }

    private void OnDashFront(InputValue value)
    {
        if (!canReceiveInput) return;
        if (value.isPressed)
            dashFrontPressed = true;
    }

    private void OnDashBack(InputValue value)
    {
        if (!canReceiveInput) return;
        if (value.isPressed)
            dashBackPressed = true;
    }

    // --- ESTADOS ---
    private void Idle()
    {
        
        myAnimator.SetBool("RUN", false);
        
        // myAnimator.SetTrigger("JumpEnded");
        // myAnimator.CrossFade("IDLE", 0.1f);
        //myAnimator.Play("IDLE");




        if (isAttacking)
        {
            SetState(States.AttackPatada);
            
        }
            
        else if (isJumpPressed && isGrounded) SetState(States.Jump);
        else if (isMoving) SetState(States.Run);
        else if (dashFrontPressed) SetState(States.DashFront);
        else if (dashBackPressed) SetState(States.DashBack);
        else if (isCrouchPressed) SetState(States.Crouch);

            ResetInputs();
        
    }

    private void Run()
    {
        myAnimator.SetBool("RUN", true);
        
        myAnimator.Play("RUN");

        if (!isMoving) SetState(States.Idle);
        else if (isAttacking) SetState(States.AttackPatada);
        else if (isJumpPressed && isGrounded) SetState(States.Jump);
        else if (dashFrontPressed) SetState(States.DashFront);
        else if (dashBackPressed) SetState(States.DashBack);
        else if (isCrouchPressed) SetState(States.Crouch);

        ResetInputs();
    }

    private void Jump()
    {
        if (isGrounded )
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            // myAnimator.SetTrigger("JUMP1");    
            myAnimator.Play("Jump");
            ResetInputs();
            canReceiveInput = false;
            isMoving = false;
            moveInput = Vector2.zero;
            direction = Vector3.zero;

            SetState(States.Idle);

        }
      

      
       
    }
    private void Crouch ()
    {
        Debug.Log("nyam");
        if (isGrounded)
        {
                
            myAnimator.Play("Crouch");
            ResetInputs();
            canReceiveInput = false;
            isMoving = false;
            moveInput = Vector2.zero;
            direction = Vector3.zero;

            SetState(States.Idle);
            ResetInputs();

        }
    }
  
    private void CanReceive()
    {
        canReceiveInput = true;
        moveInput = lastMoveInput;
        isMoving = moveInput.magnitude > 0.1f;
    }


    private void AttackPatada()
    {

        //myAnimator.SetTrigger("ATTACK");
        //myAnimator.Play("AttackPatada");
        myAnimator.Play("AttackPatada");
        //ResetInputs();
        Debug.Log("Loh");
        canReceiveInput = false;
        isMoving = false;
        moveInput = Vector2.zero;
        direction = Vector3.zero;
        
        //ReturnToIdleAfterAnimation("AttackPatada");
        SetState(States.Idle);




    }


    private void DashFront()
    {
       // myAnimator.SetTrigger("DashFront");
        myAnimator.Play("DashFront");
        StartCoroutine(ReturnToIdleAfterAnimation("DashFront"));
    }

    private void DashBack()
    {
        //myAnimator.SetTrigger("DashBack");
        myAnimator.Play("DashBack");
        StartCoroutine(ReturnToIdleAfterAnimation("DashBack"));
    }

    private void Stunned()
    {
        myAnimator.Play("Stun");
    }

    private void Dead()
    {
        myAnimator.Play("Die");
    }

    // --- UTILIDADES ---
    private void SetState(States newState)
    {
        mystate = newState;
        Debug.Log("Estado cambiado a: " + mystate);
    }

    private void ResetInputs()
    {
        isCrouchPressed = false;
        isAttacking = false;
        isJumpPressed = false;
        dashFrontPressed = false;
        dashBackPressed = false;
    }
  
    private IEnumerator ReturnToIdleAfterAnimation(string animName)
    {
        yield return new WaitForSeconds(GetAnimationLength(animName));
        SetState(States.Idle);
    }
   
    private IEnumerator PlayAndWaitForAnimation(Animator animator, string clipName)
    {
        // Play the animation
        animator.Play(clipName);


        // Wait until the animation has finished
        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
            yield return null;

        Debug.Log($"{clipName} animation finished!");
      
        SetState(States.Idle);
        ResetInputs();
    }



    private float GetAnimationLength(string animName)
    {
        RuntimeAnimatorController ac = myAnimator.runtimeAnimatorController;
        foreach (var clip in ac.animationClips)
        {
            if (clip.name == animName)
                return clip.length;
        }
        return 0.5f; // fallback
    }

    // --- BOOST TEMPORAL ---
    public void ActivarBoost(float boostAmount, float duration)
    {
        if (boostCoroutine != null)
            StopCoroutine(boostCoroutine);

        boostCoroutine = StartCoroutine(BoostCoroutine(boostAmount, duration));
    }

    private IEnumerator BoostCoroutine(float boostAmount, float duration)
    {
        moveSpeed = defaultSpeed * boostAmount;
        yield return new WaitForSeconds(duration);
        moveSpeed = defaultSpeed;
        boostCoroutine = null;
    }
    
}
