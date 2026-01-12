using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerMovMultiplayer : NetworkBehaviour
{
    private CharacterController controller;
    private Animator myAnimator;

    private bool isInPowerUp = false;

    private GameObject player;
    public Transform GallinaApunta;
    private GameObject currentStunEffect;
    public GameObject PatadaEffectPrefab;
    public GameObject CagePrefab;

    private static GameObject currentCage;

    private Vector2 lastMoveInput;
    private Vector2 moveInput;
    private Vector3 direction;
    private Vector3 velocity;
    private Vector3 airMomentum;
    [SerializeField] private bool isGrounded;

    public GameObject dashFrontEffectPrefab;
    public GameObject dashBackEffectPrefab;
    public Transform dashPointFront;
    public Transform dashPointBack;

    public Text contadorVida;
    public GameObject kickWindPrefab;
    public Transform footTrigger;

    public Slider sliderDance;
    public int dancePoints;

    public GameObject canvasEscape; //canvas escape

    public bool AttackDone = true;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.15f;
    public LayerMask groundMask;

    [Header("Movement Settings")]
    public float gravity = -9.81f;
    public float jumpForce = 12f;
    public float airControl = 0.2f;
    public float smoothTime = 0.1f;

    private bool boostGiven = true;

    private float defaultSpeed;
    private float speedBoostMultiplier = 1f; //Power Ups
    private float animBoostMultiplier = 1f;  //Otros Cambios de velocidad

    public float moveSpeed = 5f;
    private float boost = 1f;

    [Header("Dash Settings")]
    public float dashSpeed = 2f;
    public float dashDuration = 0.3f;
    public float dashCooldown = 1f;
    private bool isDashing = false;
    private bool canDash = true;

    [Header("ClickGame")]
    public PlayerMovMultiplayer lastAttacker;

    private float currentVelocity;
    private Coroutine boostCoroutine;

    private bool isMoving;
    private bool isAttacking;
    private bool isAttackingLow;
    public bool isJumpPressed;
    private bool DancePressed;
    private bool isCrouchPressed;
    public bool dashFrontPressed;
    private bool dashBackPressed;
    private bool canReceiveInput = true;
    private bool canReceiveInputDash = true;
    private bool canReceiveInputAttack = true;
    private bool canReceiveInputMove = true;

    public enum States
    {
        Idle, Run, AttackPatada, Jump, Fall, DashFront, DashBack,
        Stunned, Dead, Crouch, AttackLow, ClickBattle, Dance, PowerUp
    }
    public States mystate;

    public Transform model; // Para rotar solo el modelo visual

    [Header("Vida y Daño")]
    public int vidas = 3;
    public int lives = 1;

    // REFERENCIA AL HealthSystem del UI que se asignará en StartGame del LobbyJoinManager
    [HideInInspector] public HealthSystemMultiplayer uiHealth;
    [HideInInspector] public bool isDefinitivelyDead = false;

    [Header("Hitbox de Ataque")]
    public GameObject kickHitbox; // Asignar el objeto hijo con collider
    private HitboxMultiplayer hitboxScript;

    void Start()
    {
        // 🔹 Inicialización que debe ocurrir SIEMPRE (host y clientes, owner o no)
        controller = GetComponent<CharacterController>();
        myAnimator = GetComponent<Animator>();
        defaultSpeed = moveSpeed;
        mystate = States.Idle;

        if (kickHitbox != null)
        {
            hitboxScript = kickHitbox.GetComponent<HitboxMultiplayer>();
            if (hitboxScript != null)
                hitboxScript.ownerNetObj = GetComponent<NetworkObject>(); // importante en el SERVER también

            kickHitbox.SetActive(false);
        }

        // 🔹 A partir de aquí, SOLO el dueño procesa inputs / UI local
        if (!IsOwner) return;

        if (sliderDance != null)
        {
            sliderDance.value = dancePoints;
            sliderDance.gameObject.SetActive(false);
        }

        boostGiven = true;

        // Si tienes cosas especiales de canvas/pause, aquí
        // canvasEscape = ...
    }



    private void Update()
    {
        if (!IsOwner) return;

        // --- Animación especial del Power Up ---
        if (mystate == States.PowerUp)
        {
            Vector3 currentMoveDir = new Vector3(moveInput.x, 0f, moveInput.y);

            if (currentMoveDir.magnitude < 0.1f)
                myAnimator.Play("Idle");
            else
                myAnimator.Play("RunFast");
        }



        if (sliderDance != null)
        {
            sliderDance.value = dancePoints;
        }

        if (mystate == States.ClickBattle)
            return;

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
            case States.Dance: Dance(); break;
            case States.PowerUp: PowerUp(); break;
                case States.ClickBattle: ClickBattle(); break;

        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            ClickGameManagerMultiplayer.Instance.RegisterClickServerRpc(NetworkObjectId);
        }

    }


    // --- INPUTS DEL NEW INPUT SYSTEM ---
    private void OnMove(InputValue value)
    {
        if (!IsOwner) return;
        Vector2 currentInput = value.Get<Vector2>();
        lastMoveInput = currentInput;

        if (!canReceiveInput && !canReceiveInputMove) return;

        moveInput = currentInput;
        isMoving = moveInput.magnitude > 0.1f;
    }

    private void OnCrouch(InputValue value)
    {
        if (!IsOwner) return;
        if (!canReceiveInput) return;
        if (value.isPressed && isGrounded)
            isCrouchPressed = true;
    }

    private void OnJump(InputValue value)
    {
        if (!IsOwner) return;
        if (!canReceiveInput) return;

        if (value.isPressed && isGrounded)
            isJumpPressed = true;

    }


    private void OnAttack(InputValue value)
    {
        if (!IsOwner) return;
        if (!canReceiveInput && canReceiveInputAttack) return;
        if (value.isPressed)
            isAttacking = true;
    }
    private void OnAttackLow(InputValue value)
    {
        if (!IsOwner) return;
        if (!canReceiveInput) return;
        if (value.isPressed && isGrounded)
            isAttackingLow = true;
    }

    private void OnDashFront(InputValue value)
    {
        if (!IsOwner) return;
        if (!canReceiveInput && !canReceiveInputDash) return;
        if (value.isPressed)
            dashFrontPressed = true;
    }

    private void OnDashBack(InputValue value)
    {
        if (!IsOwner) return;
        if (!canReceiveInput && !canReceiveInputDash) return;
        if (value.isPressed)
            dashBackPressed = true;
    }
    private void OnMash(InputValue value)
    {
        if (!IsOwner) return;

        if (mystate == States.ClickBattle && value.isPressed)
        {
            ClickGameManagerMultiplayer.Instance.RegisterClickServerRpc(NetworkObjectId);
        }
    }

    private void OnDance(InputValue value)
    {
        if (!IsOwner) return;
        if (value.isPressed)
        {
            DancePressed = true;
        }

    }
    /*private void OnEscape(InputValue value)
    {
        Debug.Log("ESCAPE PRESSED");
        if (SceneManager.GetActiveScene().name == "SceneLocal")
        {
            canvasEscape.SetActive(true);
            Time.timeScale = 0f;   // Pausar
            Cursor.lockState = CursorLockMode.None;  // libera el cursor
            Cursor.visible = true;                   // muestra el cursor

        }
        else
        {
            canvasEscape.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
     public void SetCanvasEscape(GameObject c)
    {
        canvasEscape = c;
    }
    */

    // --- ESTADOS ---
    private void Idle()
    {
        // dancePoints = 0;
        if (dashFrontPressed) SetState(States.DashFront);
        if (dashBackPressed) SetState(States.DashBack);
        if (isAttacking) SetState(States.AttackPatada);

        if (!isGrounded)
            return;



        myAnimator.SetBool("RUN", false);
        //myAnimator.Play("IDLE");
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

        else if (DancePressed)
            SetState(States.Dance);
        ResetInputs();





    }

    private void Run()
    {


        SoundManager.StopSound(SoundType.Dance);
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
        else if (DancePressed)
            SetState(States.Dance);


        ResetInputs();
    }

    private void Jump()
    {

        velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        isJumpPressed = false;
        // myAnimator.SetTrigger("JUMP1");    
        myAnimator.Play("Jump");

        // ResetInputs();
        StartCoroutine(AnimBoostCoroutine(0.5f, 1f));
        if (dashFrontPressed) SetState(States.DashFront);
        if (dashBackPressed) SetState(States.DashBack);
        StartCoroutine(JumpRoutine());


    }
    private void JumpSound()
    {
        SoundManager.PlaySound(SoundType.Jump);
    }
    private IEnumerator JumpRoutine()
    {
        if (!IsOwner) yield break;
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
        StartCoroutine(AnimBoostCoroutine(0.5f, 0.5f));

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
            SoundManager.PlaySound(SoundType.Crouch);

        }
    }

    public void CanReceive()
    {
        canReceiveInput = true;

        moveInput = lastMoveInput;
        isMoving = moveInput.magnitude > 0.1f;
    }


    private void AttackPatada()
    {
        if (isGrounded)
        {
            if (dashFrontPressed) SetState(States.DashFront);
            if (dashBackPressed) SetState(States.DashBack);
            Debug.Log("Patada1");
            if (!AttackDone) return;
            isGrounded = false;
            isAttacking = false;
            AttackDone = false;
            Debug.Log("Patada");

            myAnimator.Play("AttackPatada");
            SoundManager.PlaySound(SoundType.AttackPrime);
            //  StopMove();
            StartCoroutine(AnimBoostCoroutine(0.3f, 0.8f));
            // SetState(States.Idle);

        }
        else
        {
            isAttacking = false;
            myAnimator.Play("AttackCircAire");
            StopMove();
            SetState(States.Idle);
            SoundManager.PlaySound(SoundType.AttackPrime);
        }

    }
    private void Dance()
    {
        if (sliderDance != null)
        {
            sliderDance.gameObject.SetActive(true);
        }
        // sliderDance.gameObject.SetActive(true);
        myAnimator.Play("Dance");
        SoundManager.PlaySound(SoundType.Dance);
        StopMove();
        SetState(States.Idle);

    }
    GameObject lastNumero;
    GameObject numero;
    private void DanceSlider()
    {

        if (dancePoints >= 3)
        {
            player = this.gameObject;
            player.gameObject.tag = "Invicible";
            StartCoroutine(TemporaryTagRoutine());
            Debug.Log("Tiraaa");
            myAnimator.Play("IDLE");
            GameObject[] gallina = GameObject.FindGameObjectsWithTag("Gallina");

            if (gallina.Length <= 1)
            {
                numero = gallina[0];

            }
            else
            {


                // Evita elegir el mismo enemigo
                List<GameObject> list = new List<GameObject>(gallina);
                if (lastNumero != null)
                    list.Remove(lastNumero);

                if (list.Count == 0)
                    list = new List<GameObject>(gallina);
                numero = list[Random.Range(0, list.Count)];

            }
            lastNumero = numero;


            ScriptGallinaIdle gall = numero.GetComponent<ScriptGallinaIdle>();
            gall.SetAttack();
            if (sliderDance != null)
            {
                dancePoints = 0;
                sliderDance.gameObject.SetActive(false);
            }
            //Call gallinas script throw
            //Stop dancing
            //reset dancePoints en idle si te mueves 
            // Find all GameObjects with the tag "GallinaBaile"
            GameObject[] gallinasBaile = GameObject.FindGameObjectsWithTag("GallinaBaile");

            // Loop through each GameObject
            foreach (GameObject gallinaBaile in gallinasBaile)
            {
                // Get the ScriptGallinaIdle component on that GameObject
                ScriptGallina script = gallinaBaile.GetComponent<ScriptGallina>();

                // Make sure the component exists before calling the method
                if (script != null)
                {
                    script.DanceState();
                }
            }

        }
        else
        {
            dancePoints++;
        }



    }
    private IEnumerator TemporaryTagRoutine()
    {
        if (!IsOwner) yield break;
        yield return new WaitForSeconds(3f);
        player.gameObject.tag = "Player";

    }
    private void DanceEnded()
    {
        Debug.Log("Nice");
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
        SoundManager.PlaySound(SoundType.AttackSecond);
    }

    private void StopMove()
    {
        canReceiveInputMove = false;
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
    private void CanMove()
    {
        canReceiveInputMove = true;
    }
    private void CanReceiveDash()
    {
        canReceiveInputDash = true;
    }
    private void StopMove2()
    {
        //canReceiveInput = false;
        // moveSpeed = defaultSpeed * 0.5f;
        StartCoroutine(AnimBoostCoroutine(0.5f, 1));
    }

    private void DashFront()
    {

        dashFrontPressed = false;
        if (!isDashing && canDash)
        {
            SoundManager.PlaySound(SoundType.Dash);
            SpawnDashFrontFX();
            StartCoroutine(AnimBoostCoroutine(0.5f, 0.5f));
            StopMove();
            StartCoroutine(PerformDash(transform.forward, "DashFront", 2.5f));


        }

    }

    private void DashBack()
    {
        dashBackPressed = false;
        if (!isDashing && canDash)
        {
            SoundManager.PlaySound(SoundType.Dash);
            SpawnDashBackFX();
            StartCoroutine(AnimBoostCoroutine(0.5f, 0.5f));
            StopMove();
            StartCoroutine(PerformDash(-transform.forward, "DashBack", 2.5f));

        }

    }
    public void RunSteps()
    {
        SoundManager.PlaySound(SoundType.Run);
    }
   
    private void Stunned()
    {
        myAnimator.Play("Stunned");
        StopMove();
        SetState(States.Idle);
    }
    private void StunnedSound()
    {
        SoundManager.PlaySound(SoundType.StunStars);
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
        // GameObject fx = Instantiate(kickWindPrefab, footTrigger.position, footTrigger.rotation);

        // Destruir automáticamente después de un tiempo (para limpiar)
        //Destroy(fx, 1f);
    }

    public void SpawnDashFrontFX()
    {
        Vector3 spawnPos = dashPointFront != null ? dashPointFront.position : transform.position + Vector3.up * 0.5f;
        Quaternion spawnRot = Quaternion.LookRotation(transform.forward) * Quaternion.Euler(0, 180, 0);

        // GameObject fx = Instantiate(dashFrontEffectPrefab, spawnPos, spawnRot);
        //Destroy(fx, 1f);
    }

    public void SpawnDashBackFX()
    {
        Vector3 spawnPos = dashPointBack != null ? dashPointBack.position : transform.position + Vector3.up * 0.5f;
        Quaternion spawnRot = Quaternion.LookRotation(transform.forward) * Quaternion.Euler(0, 0, 0);

        // GameObject fx = Instantiate(dashBackEffectPrefab, spawnPos, spawnRot);
        // Destroy(fx, 1f);
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
        SoundManager.PlaySound(SoundType.Dead);
        StopMove();
        PlayerIsDeadServerRpc();
    }
    [ServerRpc]
    private void PlayerIsDeadServerRpc()
    {
        // Busamos el Manager
        var clickMgr = FindFirstObjectByType<GameManageMultiplayer>();

        if (clickMgr != null)
        {
            // Le pasamos el ID del cliente dueño de este personaje (el que murió)
            clickMgr.HandlePlayerDeathServer(this.OwnerClientId);
        }
    }

    // --- UTILIDADES ---
    public void SetState(States newState)
    {
        if (!IsOwner) return;
        if (mystate == States.PowerUp && isInPowerUp) return;

        mystate = newState;
        Debug.Log("Estado cambiado a: " + mystate);
        if (newState == States.ClickBattle)
            ClickBattle();

        if (newState != States.Idle)
        {
            if (sliderDance != null)
            {
                dancePoints = 0;
                sliderDance.gameObject.SetActive(false);
            }
        }



    }

    public void ResetInputs()
    {
        isCrouchPressed = false;
        isAttacking = false;
        isJumpPressed = false;
        dashFrontPressed = false;
        dashBackPressed = false;
        isAttackingLow = false;
        DancePressed = false;

    }


    private IEnumerator PerformDash(Vector3 dashDirection, string animName, float dashDistance)
    {
        if (!IsOwner) yield break;
        SoundManager.StopSound(SoundType.StunStars);
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


    public void RegisterAttacker(PlayerMovMultiplayer attacker)
    {
        lastAttacker = attacker;
    }


    // ----------------------------
    // CORRECCIÓN IMPORTANTE: MUERTE DEFINITIVA
    // ----------------------------











    // --- BOOST TEMPORAL ---
    public void ActivarSpeedBoost(float amountBoost, float duration)
    {
        boost = amountBoost;
        boostGiven = false;



        StartCoroutine(SpeedBoostCoroutine(boost, 5f));
    }

    private IEnumerator SpeedBoostCoroutine(float amount, float duration)
    {
        if (!IsOwner) yield break;
        if (!boostGiven) yield return new WaitForSeconds(0.5f);
        Debug.Log("speeed");
        moveSpeed = defaultSpeed * amount;
        AttackDone = true;
        yield return new WaitForSeconds(duration);
        moveSpeed = defaultSpeed;
        boostCoroutine = null;

        boostGiven = true;
        AttackDone = true;


    }
    private IEnumerator AnimBoostCoroutine(float amount, float duration)
    {
        if (!IsOwner) yield break;
        if (mystate == States.PowerUp)
        {
            AttackDone = true;
            yield break;
        }

        if (!boostGiven)
        {
            AttackDone = true;
            yield break;
        }



        Debug.Log("hola2");
        moveSpeed = defaultSpeed * amount;
        yield return new WaitForSeconds(duration);
        if (mystate == States.PowerUp)
        {
            AttackDone = true;
            yield break;
        }
        moveSpeed = defaultSpeed;
        boostCoroutine = null;


        if (mystate == States.AttackPatada)
        {
            Debug.Log("hola3");
            AttackDone = true;
            SetState(States.Idle);

        }

        AttackDone = true;

    }






    /*private IEnumerator Boost(float BoostAmount, float Duration)
    {
        Debug.Log("hola2");
        moveSpeed = defaultSpeed * BoostAmount;
        yield return new WaitForSeconds(Duration);
        moveSpeed = defaultSpeed;
        boostCoroutine = null;
    }*/
    public void ClickBattle()
    {
        StopMove();
        myAnimator.Play("IDLE");

        //if (IsOwner)
           // SpawnCageServerRpc(transform.position);
    }

   
   

  

    private void PowerUp()
    {


        if (isInPowerUp) return;

        isInPowerUp = true;

        StartCoroutine(PowerSpeed());

    }
    private IEnumerator PowerSpeed()
    {
        if (!IsOwner) yield break;
        StopMove();
        moveSpeed = 15;
        CanMove();
        yield return new WaitForSeconds(3);
        moveSpeed = defaultSpeed;
        CanReceive();
        isInPowerUp = false;
        SetState(States.Idle);

    }
    public void ResetVidas()
    {
        vidas = 3;
        if (uiHealth != null)
            uiHealth.ResetHealthFromNetwork();
        lives--;
    }

    public bool CanReceiveInput => canReceiveInput;

    // --------------------------
    // DAÑO AUTORITATIVO EN SERVER
    // --------------------------
    public void ProcessDamageOnServer(int damage, ulong attackerId)
    {
        if (!IsServer)
            return;

        // Si quisieras usar NetworkHealth podrías descomentar esto:
        // int newHealth = Mathf.Clamp(NetworkHealth.Value - damage, 0, maxHealth);
        // NetworkHealth.Value = newHealth;  // sincroniza a todos

        // Por ahora usamos el ClientRpc para actualizar UI y lógica local
        TakeHitClientRpc(damage, attackerId);
    }

    public void ProcessStunOnServer(ulong attackerId)
    {
        if (mystate == States.Dead) return;
        TakeStunClientRpc(attackerId);
    }

    // 2. El ClientRpc se ejecuta en TODOS los ordenadores conectados
    [ClientRpc]
    private void TakeHitClientRpc(int damage, ulong attackerId)
    {
        // Buscar al atacante localmente para tener la referencia
        PlayerMovMultiplayer attackerScript = null;
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(attackerId, out NetworkObject attackerObj))
        {
            attackerScript = attackerObj.GetComponent<PlayerMovMultiplayer>();
        }

        // Ejecutar la lógica visual y de datos localmente
        TakeHitLocal(damage, attackerScript);
    }

    [ClientRpc]
    private void TakeStunClientRpc(ulong attackerId)
    {
        TakeStunLocal();
    }

    // 3. Lógica Local (Visuales, UI, Sonido, Animación)
    private void TakeHitLocal(int damage, PlayerMovMultiplayer attacker)
    {
        SoundManager.PlaySound(SoundType.HitCulo);
        lastAttacker = attacker;

        // Actualizar UI y vida local
        if (uiHealth != null)
        {
            uiHealth.TakeDamage(damage);   // Cambia los corazones
            vidas = uiHealth.health;
        }
        else
        {
            vidas -= damage;
        }

        Debug.Log($"{gameObject.name} recibió daño. Vidas restantes: {vidas}");

        if (vidas <= 0)
        {
            if (lives > 0)
            {
               // SetState(States.Dead);
                // Solo el dueño debería iniciar la lógica compleja del ClickBattle
                 
                 {
                      var clickMgr = FindFirstObjectByType<ClickGameManagerMultiplayer>();
                      if (clickMgr != null)
                       clickMgr.StartBattle(lastAttacker, this);
                    
                 }
            }
            else
            {
                isDefinitivelyDead = true;
                SetState(States.Dead);
            }
        }
        else
        {
            // Pequeño dash hacia delante al recibir golpe (solo dueño para evitar jitter)
            if (IsOwner)
                StartCoroutine(PerformDash(transform.forward, "DashFront", 1.5f));
        }
    }

    public void TakeStunLocal()
    {
        SoundManager.PlaySound(SoundType.HitBody);
        SetState(States.Stunned);
    }

    // --------------------------
    // NETWORK HEALTH (opcional, por si lo quieres usar después)
    // --------------------------
    public NetworkVariable<int> NetworkHealth = new NetworkVariable<int>(
        value: 3,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Cuando cambia la vida → actualizar la UI desde NetworkHealth (si lo usas)
        NetworkHealth.OnValueChanged += (oldValue, newValue) =>
        {
            if (uiHealth != null)
            {
                uiHealth.health = newValue;
                uiHealth.RefreshHeartsFromNetwork();
            }
        };
    }
}








