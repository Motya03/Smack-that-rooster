using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;


public class PlayerMovLocal : MonoBehaviour
{
    private CharacterController controller;
    private Animator myAnimator;

    private GameObject currentStunEffect;
    public GameObject PatadaEffectPrefab;
    public  GameObject CagePrefab;

    private static GameObject currentCage;

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
    //public Transform atttackPoint;

    [Header("Ground Check")]
    public Transform groundCheck;            // punto de comprobación (ponlo cerca de los pies)
    public float groundDistance = 0.15f;     // radio de la esfera
    public LayerMask groundMask;             // que capas cuentan como suelo


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

    [Header("ClickGame")]
    public PlayerMovLocal lastAttacker;
    
    private float currentVelocity;
    private float defaultSpeed;
    private Coroutine boostCoroutine;

    

    private bool isMoving;
    private bool isAttacking;
    private bool isAttackingLow;
    public bool isJumpPressed;
    private bool isCrouchPressed;
    public bool dashFrontPressed;
    private bool dashBackPressed;
    private bool canReceiveInput = true;
    private bool canReceiveInputDash = true;
    private bool canReceiveInputAttack = true;
    



    public enum States { Idle, Run, AttackPatada, Jump, Fall, DashFront, DashBack, Stunned, Dead, Crouch, AttackLow, ClickBattle}
    public States mystate;

    public Transform model; // Para rotar solo el modelo visual

    [Header("Vida y Daño")]
    public int vidas = 3;

    public  int lives = 1;
    // REFERENCIA AL HealthSystem del UI que se asignará en StartGame del LobbyJoinManager
    [HideInInspector] public HealthSystem uiHealth;
    [HideInInspector] public bool isDefinitivelyDead = false;


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

      

        bool physGrounded = false;
        if (groundCheck != null)
            physGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        // Fallback con CharacterController
        bool controllerGrounded = controller != null && controller.isGrounded;

        // Combinar ambas lecturas
        isGrounded = physGrounded || controllerGrounded;

        // Evitar que se marque falso durante dash o ataque
       // if (isDashing || mystate == States.AttackPatada || mystate == States.AttackLow)
          //  isGrounded = true;

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;


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
        if (!isDashing)
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
            case States.Fall: Fall(); break;
            case States.DashFront: DashFront(); break;
            case States.DashBack: DashBack(); break;
            case States.Stunned: Stunned(); break;
            case States.Dead: Dead(); break;
            case States.Crouch: Crouch(); break;
            //case States.ClickBattle: ClickBattle(); break;

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
        if (value.isPressed && isGrounded)
            isCrouchPressed = true;
    }

    private void OnJump(InputValue value)
    {
        if (!canReceiveInput ) return;

        if (value.isPressed && isGrounded)
            isJumpPressed = true;
         
    }


    private void OnAttack(InputValue value)
    {
        if (!canReceiveInput && canReceiveInputAttack) return;
        if (value.isPressed )
        isAttacking = true;
    }
    private void OnAttackLow(InputValue value)
    {
        if (!canReceiveInput) return;
        if (value.isPressed && isGrounded)
            isAttackingLow = true;
    }

    private void OnDashFront(InputValue value)
    {
        if (!canReceiveInput && !canReceiveInputDash) return;
        if (value.isPressed )
            dashFrontPressed = true;
    }

    private void OnDashBack(InputValue value)
    {
        if (!canReceiveInput && !canReceiveInputDash) return;
        if (value.isPressed)
            dashBackPressed = true;
    }
    private void OnMash(InputValue value)
    {
        if (mystate == States.ClickBattle && value.isPressed)
        {
            ClickGameManager.Instance.RegisterClick(this);
        }
    }


    // --- ESTADOS ---
    private void Idle()
    {
        
        if (dashFrontPressed) SetState(States.DashFront);
        if (dashBackPressed) SetState(States.DashBack);
        if (isAttacking) SetState(States.AttackPatada);

        if (!isGrounded)
            return;


        
        myAnimator.SetBool("RUN", false);
            myAnimator.SetBool("Hit", false);
          //  myAnimator.SetBool("Falling", false);

        // myAnimator.SetTrigger("JumpEnded");
        // myAnimator.CrossFade("IDLE", 0.1f);
        //myAnimator.Play("IDLE");




       

             if (isJumpPressed && isGrounded) SetState(States.Jump);
            else if (isMoving) SetState(States.Run);
            // else if (dashFrontPressed) SetState(States.DashFront);
           // else if (dashBackPressed) SetState(States.DashBack);
            else if (isCrouchPressed) SetState(States.Crouch);
            else if (isAttackingLow) SetState(States.AttackLow);


            ResetInputs();
        


            

    }

    private void Run()
    {
        
        if (dashFrontPressed)
            SetState(States.DashFront);
        if (dashBackPressed)
            SetState(States.DashBack);
        if (isAttacking)
            SetState(States.AttackPatada);
        if (isAttackingLow)
            SetState(States.AttackLow);
        if (!isGrounded)
            return;
        
        myAnimator.SetBool("RUN", true);
        myAnimator.Play("RUN");

        if (!isMoving)
            SetState(States.Idle);
        else if (isAttacking)
            SetState(States.AttackPatada);
        else if (isAttackingLow)
            SetState(States.AttackLow);
        else if (isJumpPressed && isGrounded)
            SetState(States.Jump);
        else if (dashFrontPressed)
            SetState(States.DashFront);
        else if (dashBackPressed)
            SetState(States.DashBack);
        else if (isCrouchPressed)
            SetState(States.Crouch);

        ResetInputs();
    }

    private void Jump()
    {
       
        velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            isJumpPressed = false;
            // myAnimator.SetTrigger("JUMP1");    
            myAnimator.Play("Jump");
        // ResetInputs();
        StartCoroutine(BoostCoroutine(0.5f, 1f));
        if (dashFrontPressed) SetState(States.DashFront);
            if (dashBackPressed) SetState(States.DashBack);
            StartCoroutine(JumpRoutine());


    }
    private IEnumerator JumpRoutine()
    {
        // Espera un poco para evitar que vuelva a Idle enseguida
        yield return new WaitForSeconds(0.05f);
        SetState(States.Idle);
    }
    private void Fall()
    {
        if (isAttacking)
            SetState(States.AttackPatada);
        myAnimator.SetBool("Falling", true);
        // StopMove2();
        StartCoroutine(BoostCoroutine(0.5f, 0.5f));

        if (isGrounded)
        {
            myAnimator.SetBool("Falling", false);
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
        if (isGrounded)
        {
            isAttacking = false;
            Debug.Log("Patada");

            myAnimator.Play("AttackPatada");
            StopMove();
            SetState(States.Idle);
        }
        else
        {
            isAttacking = false;
            myAnimator.Play("AttackCircAire");
            StopMove();
            SetState(States.Idle);
        }
       
    }

    public void EnableVfxPatada()
    {
        Vector3 spawnPos = transform.position;

        // Instanciar con rotación del personaje + giro en X
        GameObject fx = Instantiate(
            PatadaEffectPrefab,
            spawnPos,
            transform.rotation * Quaternion.Euler(90, 0, 0) // gira 90° en X (ajusta valor)
        );
        Destroy(fx, 1f);
    }
    private void AttackLow()
    {
      //  myAnimator.Play("AttackLow");
        StartCoroutine(PerformDash(transform.forward, "AttackLow", 1.8f));
        Debug.Log("Loh");
        StopMove();
        SetState(States.Idle);
    }

    private void StopMove()
    {
        canReceiveInputAttack = false;
        canReceiveInputDash = false;
        canReceiveInput = false;
        isMoving = false;
        moveInput = Vector2.zero;
        direction = Vector3.zero;
    }
    private void CanAttack()
    {
        canReceiveInputAttack = true;
    }
    private void CanReceiveDash()
    {
        canReceiveInputDash = true;
    }
    private void StopMove2()
    {
        //canReceiveInput = false;
        // moveSpeed = defaultSpeed * 0.5f;
        StartCoroutine(BoostCoroutine(0.5f, 1));
    }
    
    private void DashFront()
    {
        dashFrontPressed = false;
        if (!isDashing && canDash)
        {
            SpawnDashFrontFX();
             StartCoroutine(BoostCoroutine(0.5f, 0.5f));
            StopMove();
            StartCoroutine(PerformDash(transform.forward, "DashFront",2.5f));
            

        }

    }

    private void DashBack()
    {
        dashBackPressed = false;
        if (!isDashing && canDash)
        {
            SpawnDashBackFX();
            StartCoroutine(BoostCoroutine(0.5f, 0.5f));
            StopMove();
            StartCoroutine(PerformDash(-transform.forward, "DashBack", 2.5f));
            
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
    private void StunEffect()
    {
        if (currentStunEffect == null)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 0.7f;
            currentStunEffect = Instantiate(stunEffectPrefab, spawnPos, Quaternion.identity, transform);
            currentStunEffect.transform.localRotation = Quaternion.identity;
        }
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
    public void SetState(States newState)
    {
        mystate = newState;
        Debug.Log("Estado cambiado a: " + mystate);
        if (newState == States.ClickBattle)
            ClickBattle();
    }

    public void ResetInputs()
    {
        isCrouchPressed = false;
        isAttacking = false;
        isJumpPressed = false;
        dashFrontPressed = false;
        dashBackPressed = false;
        isAttackingLow = false;
    }

   
    private IEnumerator PerformDash(Vector3 dashDirection, string animName, float dashDistance)
    {
        
        isDashing = true;
        canDash = false;

        myAnimator.Play(animName);

       // float dashDistance = 3f;        // Distancia total en metros
        float dashTime = 0.35f;         // Duración total del dash
        float elapsedTime = 0f;

        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + dashDirection.normalized * dashDistance;

        // Desactivamos gravedad durante el dash
        

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




        isDashing = false;
        canDash = true;

        // Asegurar que no "rebote"
        if (isGrounded)
            velocity.y = -2f;

        SetState(isGrounded ? States.Idle : States.Fall);
        ResetInputs();

    }


    public void RegisterAttacker(PlayerMovLocal attacker)
    {
        lastAttacker = attacker;
    }


    public void TakeHit(int damage, PlayerMovLocal attacker)
    {
        lastAttacker = attacker;

        // Actualizar UI y vida
        if (uiHealth != null)
        {
            uiHealth.TakeDamage(damage);
            vidas = uiHealth.health;
        }
        else
        {
            vidas -= damage;
        }

        Debug.Log($"{gameObject.name} recibió daño. Vidas restantes: {vidas}");

        // 🔻 Si las vidas bajan a 0
        if (vidas <= 0)
        {
            // Si aún tiene "vida extra", entra a click battle
            if (lives > 0)
            {
                FindFirstObjectByType<ClickGameManager>().StartBattle(lastAttacker, this);
            }
            else
            {
                // Si ya no tiene vidas extra, muere directamente
                SetState(States.Dead);
            }
        }
        else
        {
            // Pequeño retroceso o animación de daño
            myAnimator.SetBool("Hit", true);
            StartCoroutine(PerformDash(transform.forward, "DashFront", 1.5f));
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
    public void ClickBattle()
    {
        if (currentCage != null) return; // ya hay una jaula en escena

        StopMove();
        myAnimator.Play("IDLE");

        Vector3 spawnPos = transform.position  ;
        currentCage = Instantiate(CagePrefab, spawnPos, transform.rotation);
       
    }
    public void CageGone()
    {
        Debug.Log($"CageGone called by: {name}");

        if (currentCage == null)
        {
            Debug.LogWarning("No active cage in scene!");
            return;
        }

        CageScript script = currentCage.GetComponentInChildren<CageScript>();
        if (script != null)
        {
            script.ClickBattleEnd();
        }
        else
        {
            Debug.LogWarning("No CageScript found on current cage!");
        }

        // Optional: destroy cage after animation delay
        
        currentCage = null;
    }


    public void ResetVidas()
    {
        vidas = 3;
        if (uiHealth != null)
            uiHealth.ResetHealth();
    }

    public bool CanReceiveInput => canReceiveInput;

}



