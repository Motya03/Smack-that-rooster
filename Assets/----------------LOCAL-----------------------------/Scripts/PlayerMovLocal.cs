using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SocialPlatforms.Impl;
using static UnityEngine.Rendering.DebugUI;
using UnityEngine.UI;


public class PlayerMovLocal : MonoBehaviour
{
    private CharacterController controller;
    private Animator myAnimator;

    private GameObject currentStunEffect;

    private Vector2 lastMoveInput;
    private Vector2 moveInput;
    private Vector3 direction;
    private Vector3 velocity;
    private Vector3 airMomentum;
    [SerializeField] private bool isGrounded;
    public GameObject stunEffectPrefab;
    public GameObject dashFrontEffectPrefab;
    public GameObject dashBackEffectPrefab;
    public Transform dashPointFront;
    public Transform dashPointBack;

    public Text contadorVida;
    public GameObject kickWindPrefab;
    public Transform footTrigger;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float gravity = -9.81f;
    public float jumpForce = 0.1f;
    public float airControl = 0.2f;
    public float smoothTime = 0.1f;

    [Header("Dash Settings")]
    public float dashSpeed = 2f;
    public float dashDuration = 0.3f;
    public float dashCooldown = 1f;
    private bool isDashing = false;
    private bool canDash = true;


    private float currentVelocity;
    private float defaultSpeed;
    private Coroutine boostCoroutine;

    private bool isMoving;
    private bool isAttacking;
    private bool isAttackingLow;
    private bool isJumpPressed;
    private bool isCrouchPressed;
    private bool dashFrontPressed;
    private bool dashBackPressed;
    private bool canReceiveInput = true;



    public enum States { Idle, Run, AttackPatada, Jump, DashFront, DashBack, Stunned, Dead, Crouch, AttackLow }
    public States mystate;

    public Transform model; // Para rotar solo el modelo visual

    [Header("Vida y Daño")]
    public int vidas = 3;
    // REFERENCIA AL HealthSystem del UI que se asignará en StartGame del LobbyJoinManager
    [HideInInspector] public HealthSystem uiHealth;


    [Header("Hitbox de Ataque")]
    public GameObject kickHitbox; // Asignar el objeto hijo con collider
    private Hitbox hitboxScript;
    void Start()
    {
        
        controller = GetComponent<CharacterController>();
        myAnimator = GetComponent<Animator>();
        defaultSpeed = moveSpeed;
        mystate = States.Idle;

        if (kickHitbox != null)
        {
            hitboxScript = kickHitbox.GetComponent<Hitbox>();
            if (hitboxScript != null) hitboxScript.owner = gameObject; // <-- aquí ya es la instancia
            kickHitbox.SetActive(false);
        }


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
            isAttackingLow = true;
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
        else if (isAttackingLow) SetState(States.AttackLow);


        ResetInputs();

    }

    private void Run()
    {
        myAnimator.SetBool("RUN", true);

        myAnimator.Play("RUN");

        if (!isMoving) SetState(States.Idle);
        else if (isAttacking) SetState(States.AttackPatada);
        else if (isAttackingLow) SetState(States.AttackLow);
        else if (isJumpPressed && isGrounded) SetState(States.Jump);
        else if (dashFrontPressed) SetState(States.DashFront);
        else if (dashBackPressed) SetState(States.DashBack);
        else if (isCrouchPressed) SetState(States.Crouch);

        ResetInputs();
    }

    private void Jump()
    {
        if (isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            // myAnimator.SetTrigger("JUMP1");    
            myAnimator.Play("Jump");
            ResetInputs();
            StopMove();
            SetState(States.Idle);

        }




    }
    private void Crouch()
    {
        Debug.Log("nyam");
        if (isGrounded)
        {

            myAnimator.Play("Crouch");
            StopMove();
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
        myAnimator.Play("AttackPatada");
        StopMove();
       // SpawnKickFX();
        SetState(States.Idle);

    
    }

    private void AttackLow()
    {
        myAnimator.Play("AttackLow");
        Debug.Log("Loh");
        StopMove();
        SetState(States.Idle);
    }

    private void StopMove()
    {
        canReceiveInput = false;
        isMoving = false;
        moveInput = Vector2.zero;
        direction = Vector3.zero;
    }
    
    private void DashFront()
    {
        if (!isDashing && canDash)
        {
            SpawnDashFrontFX();
            StopMove();
            StartCoroutine(PerformDash(transform.forward, "DashFront"));
            

        }

    }

    private void DashBack()
    {
        if (!isDashing && canDash)
        {
            SpawnDashBackFX();
            StopMove();
            StartCoroutine(PerformDash(-transform.forward, "DashBack"));
            
        }

    }

    public void TakeStun()
    {
        
        SetState(States.Stunned);
    }
    private void Stunned()
    {
        // Si ya hay un efecto activo, no crear otro
        if (currentStunEffect == null)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 0.7f;
            currentStunEffect = Instantiate(stunEffectPrefab, spawnPos, Quaternion.identity, transform);
            currentStunEffect.transform.localRotation = Quaternion.identity;
        }

        myAnimator.Play("Stunned");
        StopMove();
        SetState(States.Idle);


    }

    public void AnimStunStop()
    {
        if (currentStunEffect != null)
        {
            Debug.Log("You");
            Destroy(currentStunEffect);
            currentStunEffect = null;
            
        }
    }

    public void SpawnKickFX()
    {
        // Instanciar en la posición y rotación del pie
        GameObject fx = Instantiate(kickWindPrefab, footTrigger.position, footTrigger.rotation);

        // Destruir automáticamente después de un tiempo (para limpiar)
        Destroy(fx, 1f);
    }

    public void SpawnDashFrontFX()
    {
        Vector3 spawnPos = dashPointFront != null ? dashPointFront.position : transform.position + Vector3.up * 0.5f;
        Quaternion spawnRot = Quaternion.LookRotation(transform.forward) * Quaternion.Euler(0, 180, 0);

        GameObject fx = Instantiate(dashFrontEffectPrefab, spawnPos, spawnRot);
        Destroy(fx, 1f);
    }

    public void SpawnDashBackFX()
    {
        Vector3 spawnPos = dashPointBack != null ? dashPointBack.position : transform.position + Vector3.up * 0.5f;
        Quaternion spawnRot = Quaternion.LookRotation(transform.forward) * Quaternion.Euler(0, 0, 0);

        GameObject fx = Instantiate(dashBackEffectPrefab, spawnPos, spawnRot);
        Destroy(fx, 1f);
    }





    /* private IEnumerator ExitStunAfterSeconds(float seconds)
     {
         yield return new WaitForSeconds(seconds);

         // Aquí "sale del stun"
         if (currentStunEffect != null)
         {
             Destroy(currentStunEffect);
             currentStunEffect = null;
         }

         SetState(States.Idle);
     }
    */
    private void Dead()
    {
        myAnimator.Play("Dead");
       
        StopMove();
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
        isAttackingLow = false;
    }

   
    private IEnumerator PerformDash(Vector3 dashDirection, string animName)
    {
        isDashing = true;
        canDash = false;

        myAnimator.Play(animName);

        float dashDistance = 1f;        // Distancia total en metros
        float dashTime = 0.35f;         // Duración total del dash
        float elapsedTime = 0f;

        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + dashDirection.normalized * dashDistance;

        // Desactivamos gravedad durante el dash
        float originalGravity = gravity;
        gravity = 0f;

        while (elapsedTime < dashTime)
        {
            float t = elapsedTime / dashTime;

            // Curva de suavizado (ease-in / ease-out)
            float speedFactor = Mathf.SmoothStep(0f, 1f, t);

            // Mover de forma progresiva
            Vector3 newPos = Vector3.Lerp(startPos, targetPos, speedFactor);
            controller.Move(newPos - transform.position);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Restaurar gravedad
        gravity = originalGravity;

        // Pequeño cooldown
        //  yield return new WaitForSeconds(dashCooldown);

        isDashing = false;
        canDash = true;

        SetState(States.Idle);
        ResetInputs();
    }





    public void TakeHit()
    {
        // Prioriza la UI asignada (que controla sprites y parpadeo).
        if (uiHealth != null)
        {
            uiHealth.TakeDamage(1);
            vidas = uiHealth.health; // opcional, sincronizar valor local con UI
        }
        else
        {
            // fallback si no está asignado (por si algo falla)
            vidas--;
        }

        Debug.Log($"{gameObject.name} recibió daño. Vidas restantes: {vidas}");

        if (vidas <= 0)
        {
            SetState(States.Dead);
        }
        else
        {
            myAnimator.SetTrigger("Hit");
            StartCoroutine(PerformDash(transform.forward, "DashFront"));
        }
    }


    private void OnDestroy()
    {
        Destroy(gameObject);
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



