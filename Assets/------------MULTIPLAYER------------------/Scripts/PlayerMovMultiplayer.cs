using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections.Generic;

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

    public GameObject canvasEscape;

    public bool AttackDone = true;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.15f;
    public LayerMask groundMask;

    [Header("Movement Settings")]
    public float gravity = -9.81f;
    public float jumpForce = 0.1f;
    public float airControl = 0.2f;
    public float smoothTime = 0.1f;

    private bool boostGiven = true;
    private float defaultSpeed;
    private float speedBoostMultiplier = 1f;
    private float animBoostMultiplier = 1f;
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

    // Estado sincronizado en red
    private NetworkVariable<PlayerState> networkState = new NetworkVariable<PlayerState>(
        PlayerState.Idle,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    public enum PlayerState
    {
        Idle, Run, AttackPatada, Jump, Fall, DashFront, DashBack,
        Stunned, Dead, Crouch, AttackLow, ClickBattle, Dance, PowerUp
    }

    public Transform model;

    [Header("Vida y Daño")]
    public int vidas = 3;
    public int lives = 1;
    [HideInInspector] public HealthSystem uiHealth;
    [HideInInspector] public bool isDefinitivelyDead = false;

    [Header("Hitbox de Ataque")]
    public GameObject kickHitbox;
    private Hitbox hitboxScript;

    GameObject lastNumero;
    GameObject numero;

    void Start()
    {
        if (sliderDance != null)
        {
            sliderDance.value = dancePoints;
            sliderDance.gameObject.SetActive(false);
        }
        boostGiven = true;
        controller = GetComponent<CharacterController>();
        myAnimator = GetComponent<Animator>();
        defaultSpeed = moveSpeed;

        if (kickHitbox != null)
        {
            hitboxScript = kickHitbox.GetComponent<Hitbox>();
            if (hitboxScript != null) hitboxScript.owner = gameObject;
            kickHitbox.SetActive(false);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            SetupLocalPlayer();
        }
        else
        {
            // Para jugadores remotos, desactivamos el PlayerInput
            PlayerInput playerInput = GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                playerInput.enabled = false;
            }
        }
    }

    private void SetupLocalPlayer()
    {
        Debug.Log($"Jugador local configurado - ID: {OwnerClientId}");

        // Habilitar PlayerInput
        PlayerInput playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            playerInput.enabled = true;

            // Forzar detección de dispositivo
            StartCoroutine(DetectInputDevice());
        }

        // Registrar en GameManager si existe
        if (GameManagerMultiplayer.Instance != null)
        {
            GameManagerMultiplayer.Instance.RegisterPlayer(this);
        }
    }

    private IEnumerator DetectInputDevice()
    {
        yield return new WaitForSeconds(0.5f);

        PlayerInput playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            Debug.Log($"Control scheme actual: {playerInput.currentControlScheme}");

            // Listar todos los dispositivos disponibles
            Debug.Log("Dispositivos disponibles:");
            foreach (var device in InputSystem.devices)
            {
                Debug.Log($"- {device.name} ({device.layout})");
            }

            // Intentar forzar el mando si hay uno conectado
            if (Gamepad.current != null)
            {
                Debug.Log($"Mando detectado: {Gamepad.current.name}");
                // No es necesario cambiar el scheme manualmente, Unity lo hace automáticamente
            }
        }
    }

    private void Update()
    {
        // --- Animación especial del Power Up ---
        if (networkState.Value == PlayerState.PowerUp && IsOwner)
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

        if (networkState.Value == PlayerState.ClickBattle)
            return;

        // Solo el dueño procesa inputs y movimiento
        if (IsOwner)
        {
            ProcessOwnerUpdate();
        }

        // Todos actualizan animaciones
        UpdateAnimations();
    }

    private void ProcessOwnerUpdate()
    {
        bool physGrounded = false;
        if (groundCheck != null)
            physGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        bool controllerGrounded = controller != null && controller.isGrounded;
        isGrounded = physGrounded || controllerGrounded;

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        // --- Dirección del input ---
        Vector3 inputDir = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        if (isGrounded)
        {
            direction = inputDir;
            if (direction.magnitude > 0.1f)
                airMomentum = direction * moveSpeed;
            else
                airMomentum = Vector3.zero;
        }
        else
        {
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

        // --- Actualizar estado según inputs ---
        UpdateStateFromInputs();
    }

    private void UpdateStateFromInputs()
    {
        if (!canReceiveInput || isDefinitivelyDead) return;

        // Determinar nuevo estado basado en inputs
        PlayerState newState = DetermineState();

        // Solo actualizar si cambió
        if (newState != networkState.Value)
        {
            networkState.Value = newState;

            // Reset inputs después de cambiar estado
            if (newState == PlayerState.Jump || newState == PlayerState.AttackPatada ||
                newState == PlayerState.DashFront || newState == PlayerState.DashBack ||
                newState == PlayerState.Crouch || newState == PlayerState.Dance)
            {
                ResetInputs();
            }
        }
    }

    private PlayerState DetermineState()
    {
        if (isAttacking && AttackDone) return PlayerState.AttackPatada;
        if (isAttackingLow) return PlayerState.AttackLow;
        if (isJumpPressed && isGrounded) return PlayerState.Jump;
        if (dashFrontPressed && canDash) return PlayerState.DashFront;
        if (dashBackPressed && canDash) return PlayerState.DashBack;
        if (DancePressed) return PlayerState.Dance;
        if (isCrouchPressed && isGrounded) return PlayerState.Crouch;
        if (isMoving) return PlayerState.Run;

        return PlayerState.Idle;
    }

    private void UpdateAnimations()
    {
        // Actualizar animaciones según estado de red
        switch (networkState.Value)
        {
            case PlayerState.Idle:
                myAnimator.SetBool("RUN", false);
                myAnimator.SetBool("Hit", false);
                myAnimator.SetBool("Falling", false);
                break;

            case PlayerState.Run:
                myAnimator.SetBool("RUN", true);
                break;

            case PlayerState.AttackPatada:
                if (isGrounded)
                {
                    myAnimator.Play("AttackPatada");
                }
                else
                {
                    myAnimator.Play("AttackCircAire");
                }
                break;

            case PlayerState.Jump:
                if (isGrounded)
                {
                    myAnimator.Play("Jump");
                    velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
                }
                break;

            case PlayerState.Fall:
                myAnimator.SetBool("Falling", true);
                break;

            case PlayerState.DashFront:
                myAnimator.Play("DashFront");
                break;

            case PlayerState.DashBack:
                myAnimator.Play("DashBack");
                break;

            case PlayerState.Crouch:
                myAnimator.Play("Crouch");
                break;

            case PlayerState.Dance:
                myAnimator.Play("Dance");
                break;

            case PlayerState.Dead:
                myAnimator.Play("Dead");
                break;

            case PlayerState.Stunned:
                myAnimator.Play("Stunned");
                break;
        }

        // Resetear falling cuando está en el suelo
        if (isGrounded && networkState.Value != PlayerState.Jump && networkState.Value != PlayerState.Fall)
        {
            myAnimator.SetBool("Falling", false);
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
        if (!IsOwner || !canReceiveInput) return;
        if (value.isPressed && isGrounded)
            isCrouchPressed = true;
    }

    private void OnJump(InputValue value)
    {
        if (!IsOwner || !canReceiveInput) return;
        if (value.isPressed && isGrounded)
            isJumpPressed = true;
    }

    private void OnAttack(InputValue value)
    {
        if (!IsOwner || !canReceiveInput && canReceiveInputAttack) return;
        if (value.isPressed)
            isAttacking = true;
    }

    private void OnAttackLow(InputValue value)
    {
        if (!IsOwner || !canReceiveInput) return;
        if (value.isPressed && isGrounded)
            isAttackingLow = true;
    }

    private void OnDashFront(InputValue value)
    {
        if (!IsOwner || !canReceiveInput && !canReceiveInputDash) return;
        if (value.isPressed)
            dashFrontPressed = true;
    }

    private void OnDashBack(InputValue value)
    {
        if (!IsOwner || !canReceiveInput && !canReceiveInputDash) return;
        if (value.isPressed)
            dashBackPressed = true;
    }

    private void OnDance(InputValue value)
    {
        if (!IsOwner) return;
        if (value.isPressed)
        {
            DancePressed = true;
        }
    }

    // --- MÉTODOS DE ANIMATIONEVENTS (IMPORTANTE: estos deben estar públicos) ---

    public void JumpSound()
    {
        Debug.Log("AnimationEvent: JumpSound");
        // SoundManager.PlaySound(SoundType.Jump);
    }

    public void RunSteps()
    {
        Debug.Log("AnimationEvent: RunSteps");
        // SoundManager.PlaySound(SoundType.Run);
    }

    public void AnimStunStop()
    {
        Debug.Log("AnimationEvent: AnimStunStop");
        if (currentStunEffect != null)
        {
            Destroy(currentStunEffect);
            currentStunEffect = null;
        }
    }

    public void CanReceive()
    {
        Debug.Log("AnimationEvent: CanReceive");
        if (!IsOwner) return;

        canReceiveInput = true;
        canReceiveInputMove = true;
        canReceiveInputAttack = true;
        canReceiveInputDash = true;

        moveInput = lastMoveInput;
        isMoving = moveInput.magnitude > 0.1f;
    }

    public void CanAttack()
    {
        Debug.Log("AnimationEvent: CanAttack");
        if (!IsOwner) return;
        canReceiveInputAttack = true;
    }

    public void CanMove()
    {
        Debug.Log("AnimationEvent: CanMove");
        if (!IsOwner) return;
        canReceiveInputMove = true;
    }

    public void CanReceiveDash()
    {
        Debug.Log("AnimationEvent: CanReceiveDash");
        if (!IsOwner) return;
        canReceiveInputDash = true;
    }

    public void EnableVfxPatada()
    {
        Debug.Log("AnimationEvent: EnableVfxPatada");
        if (PatadaEffectPrefab != null)
        {
            Vector3 spawnPos = transform.position;
            GameObject fx = Instantiate(PatadaEffectPrefab, spawnPos,
                transform.rotation * Quaternion.Euler(90, 0, 0));
            Destroy(fx, 1f);
        }
    }

    public void DanceSlider()
    {
        Debug.Log("AnimationEvent: DanceSlider");

        if (dancePoints >= 3)
        {
            player = this.gameObject;
            // player.gameObject.tag = "Invicible"; // Cuidado con tags en multiplayer
            StartCoroutine(TemporaryTagRoutine());
            Debug.Log("Tiraaa");
            myAnimator.Play("IDLE");

            // Lógica de gallinas (simplificada para multiplayer)
            if (sliderDance != null)
            {
                dancePoints = 0;
                sliderDance.gameObject.SetActive(false);
            }
        }
        else
        {
            dancePoints++;
        }
    }

    private IEnumerator TemporaryTagRoutine()
    {
        yield return new WaitForSeconds(3f);
        // player.gameObject.tag = "Player";
    }

    public void DanceEnded()
    {
        Debug.Log("AnimationEvent: DanceEnded");
    }

    public void SpawnKickFX()
    {
        Debug.Log("AnimationEvent: SpawnKickFX");
        // if (kickWindPrefab != null)
        // {
        //     GameObject fx = Instantiate(kickWindPrefab, footTrigger.position, footTrigger.rotation);
        //     Destroy(fx, 1f);
        // }
    }

    public void SpawnDashFrontFX()
    {
        Debug.Log("AnimationEvent: SpawnDashFrontFX");
        // if (dashFrontEffectPrefab != null)
        // {
        //     Vector3 spawnPos = dashPointFront != null ? dashPointFront.position : transform.position + Vector3.up * 0.5f;
        //     Quaternion spawnRot = Quaternion.LookRotation(transform.forward) * Quaternion.Euler(0, 180, 0);
        //     GameObject fx = Instantiate(dashFrontEffectPrefab, spawnPos, spawnRot);
        //     Destroy(fx, 1f);
        // }
    }

    public void SpawnDashBackFX()
    {
        Debug.Log("AnimationEvent: SpawnDashBackFX");
        // if (dashBackEffectPrefab != null)
        // {
        //     Vector3 spawnPos = dashPointBack != null ? dashPointBack.position : transform.position + Vector3.up * 0.5f;
        //     Quaternion spawnRot = Quaternion.LookRotation(transform.forward) * Quaternion.Euler(0, 0, 0);
        //     GameObject fx = Instantiate(dashBackEffectPrefab, spawnPos, spawnRot);
        //     Destroy(fx, 1f);
        // }
    }

    public void TakeStun()
    {
        Debug.Log("AnimationEvent: TakeStun");
        // SoundManager.PlaySound(SoundType.HitBody);
        networkState.Value = PlayerState.Stunned;
    }

    public void StunnedSound()
    {
        Debug.Log("AnimationEvent: StunnedSound");
        // SoundManager.PlaySound(SoundType.StunStars);
    }

    // --- MÉTODOS DE UTILIDAD ---

    private void StopMove()
    {
        if (!IsOwner) return;

        canReceiveInputMove = false;
        canReceiveInputAttack = false;
        canReceiveInputDash = false;
        canReceiveInput = false;
        isMoving = false;
        moveInput = Vector2.zero;
        direction = Vector3.zero;
    }

    public void ResetInputs()
    {
        if (!IsOwner) return;

        isCrouchPressed = false;
        isAttacking = false;
        isJumpPressed = false;
        dashFrontPressed = false;
        dashBackPressed = false;
        isAttackingLow = false;
        DancePressed = false;
    }

    public void SetState(PlayerState newState)
    {
        if (!IsOwner) return;

        if (networkState.Value == PlayerState.PowerUp && isInPowerUp) return;

        networkState.Value = newState;
        Debug.Log("Estado cambiado a: " + newState);

        if (newState != PlayerState.Idle)
        {
            if (sliderDance != null)
            {
                dancePoints = 0;
                sliderDance.gameObject.SetActive(false);
            }
        }
    }

    // --- DASH ---
    private IEnumerator PerformDash(Vector3 dashDirection, string animName, float dashDistance)
    {
        if (!IsOwner) yield break;

        isDashing = true;
        canDash = false;

        // myAnimator.Play(animName); // Ya se reproduce por el estado

        float dashTime = 0.35f;
        float elapsedTime = 0f;

        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + dashDirection.normalized * dashDistance;

        while (elapsedTime < dashTime)
        {
            float t = elapsedTime / dashTime;
            float speedFactor = Mathf.SmoothStep(0f, 1f, t);
            Vector3 newPos = Vector3.Lerp(startPos, targetPos, speedFactor);
            controller.Move(newPos - transform.position);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        isDashing = false;
        canDash = true;

        if (isGrounded)
            velocity.y = -2f;

        SetState(isGrounded ? PlayerState.Idle : PlayerState.Fall);
    }

    // --- DAÑO ---
    public void TakeHit(int damage, PlayerMovMultiplayer attacker)
    {
        if (!IsOwner) return;

        // SoundManager.PlaySound(SoundType.HitCulo);
        lastAttacker = attacker;

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

        if (vidas <= 0)
        {
            if (lives > 0)
            {
                // Click battle (simplificado para multiplayer)
                lives--;
                vidas = 3;
                myAnimator.SetBool("Hit", true);
            }
            else
            {
                isDefinitivelyDead = true;
                SetState(PlayerState.Dead);

                // Avisar al GameManager
                if (GameManagerMultiplayer.Instance != null)
                {
                    GameManagerMultiplayer.Instance.OnPlayerDied(this);
                }
            }
        }
        else
        {
            myAnimator.SetBool("Hit", true);
            StartCoroutine(PerformDash(transform.forward, "DashFront", 1.5f));
        }
    }

    // --- BOOST TEMPORAL ---
    public void ActivarSpeedBoost(float amountBoost, float duration)
    {
        if (!IsOwner) return;

        boost = amountBoost;
        boostGiven = false;
        StartCoroutine(SpeedBoostCoroutine(boost, 5f));
    }

    private IEnumerator SpeedBoostCoroutine(float amount, float duration)
    {
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
        if (networkState.Value == PlayerState.PowerUp)
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
        if (networkState.Value == PlayerState.PowerUp)
        {
            AttackDone = true;
            yield break;
        }
        moveSpeed = defaultSpeed;
        boostCoroutine = null;

        if (networkState.Value == PlayerState.AttackPatada)
        {
            Debug.Log("hola3");
            AttackDone = true;
            SetState(PlayerState.Idle);
        }

        AttackDone = true;
    }

    // --- POWER UP ---
    private IEnumerator PowerSpeed()
    {
        StopMove();
        moveSpeed = 15;
        CanMove();
        yield return new WaitForSeconds(3);
        moveSpeed = defaultSpeed;
        CanReceive();
        isInPowerUp = false;
        SetState(PlayerState.Idle);
    }

    public void ResetVidas()
    {
        vidas = 3;
        if (uiHealth != null)
            uiHealth.ResetHealth();
    }

    public bool CanReceiveInput => canReceiveInput;

    // --- NETWORK ---
    public override void OnNetworkDespawn()
    {
        if (GameManagerMultiplayer.Instance != null)
        {
            GameManagerMultiplayer.Instance.UnregisterPlayer(this);
        }
        base.OnNetworkDespawn();
    }
}