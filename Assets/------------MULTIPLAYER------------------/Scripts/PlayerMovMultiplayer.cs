using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using static UnityEngine.Rendering.DebugUI;

public class PlayerMovMultiplayer : NetworkBehaviour
{
    private CharacterController controller;
    private Animator myAnimator;

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
    private bool dashFrontPressed;
    private bool dashBackPressed;
    private bool canReceiveInput = true;
    public enum States { Idle, Run, AttackPatada, Jump, DashFront, DashBack, Stunned, Dead }
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
        if (!IsOwner) return;

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
            case States.Jump: Jump(); break;
            case States.DashFront: DashFront(); break;
            case States.DashBack: DashBack(); break;
            case States.Stunned: Stunned(); break;
            case States.Dead: Dead(); break;
        }
    }

    // --- INPUTS DEL NEW INPUT SYSTEM ---
    private void OnMove(InputValue value)
    {
        if (!canReceiveInput) return;
        moveInput = value.Get<Vector2>();
        isMoving = moveInput.magnitude > 0.1f;
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
        myAnimator.SetTrigger("JumpEnded");


        if (isAttacking)
        {
            SetState(States.AttackPatada);

        }

        else if (isJumpPressed && isGrounded) SetState(States.Jump);
        else if (isMoving) SetState(States.Run);
        else if (dashFrontPressed) SetState(States.DashFront);
        else if (dashBackPressed) SetState(States.DashBack);

        ResetInputs();
    }

    private void Run()
    {
        myAnimator.SetBool("RUN", true);

        if (!isMoving) SetState(States.Idle);
        else if (isAttacking) SetState(States.AttackPatada);
        else if (isJumpPressed && isGrounded) SetState(States.Jump);
        else if (dashFrontPressed) SetState(States.DashFront);
        else if (dashBackPressed) SetState(States.DashBack);

        ResetInputs();
    }

    private void Jump()
    {
        if (isGrounded)
        {
            myAnimator.SetTrigger("JUMP1");
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);


            SetState(States.Idle);
            ResetInputs();
        }



    }


    private void AttackPatada()
    {

        myAnimator.SetTrigger("ATTACK");

        StartCoroutine(MoveNull());
        ResetInputs();
        SetState(States.Idle);

    }
    IEnumerator MoveNull()
    {
        moveSpeed *= 0f;
        direction = Vector3.zero;
        yield return new WaitForSeconds(0.2f);
        moveSpeed = defaultSpeed;
    }

    private void DashFront()
    {
        myAnimator.SetTrigger("DashFront");
        StartCoroutine(ReturnToIdleAfterAnimation("DashFront"));
    }

    private void DashBack()
    {
        myAnimator.SetTrigger("DashBack");
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
    }

    private void ResetInputs()
    {
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