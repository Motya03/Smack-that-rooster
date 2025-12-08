using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// PlayerMovMultiplayer
/// - Owner procesa input y movimiento (CharacterController)
/// - Estado (anim) se sincroniza vía NetworkVariable<PlayerState> (Owner write)
/// - Salud se maneja en servidor vía NetworkVariable<int> networkVidas (Server write)
/// - Daño se aplica con ServerRpc (RequireOwnership = false) -> target.ApplyDamageServerRpc(...)
/// - Hitbox cliente -> llama al target.ApplyDamageServerRpc
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovMultiplayer : NetworkBehaviour
{
    // referencias
    private CharacterController controller;
    private Animator myAnimator;

    [Header("VFX / Prefabs")]
    public GameObject PatadaEffectPrefab;
    public GameObject CagePrefab;
    public GameObject dashFrontEffectPrefab;
    public GameObject dashBackEffectPrefab;
    public GameObject StunEffectPrefab;
    public GameObject PowerUpEffectPrefab;

    [Header("Transf & hitbox")]
    public Transform model; // rotar visualmente
    public GameObject kickHitbox; // objeto con collider (desactivado por defecto)
    public GameObject lowKickHitbox; // hitbox de ataque bajo
    public Transform dashPointFront;
    public Transform dashPointBack;
    public Transform footTrigger;

    [Header("UI y Dance")]
    public Slider sliderDance;
    public int dancePoints;
    public Text contadorVida; // opcional (solo owner)

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float smoothTime = 0.1f;
    public float gravity = -9.81f;
    public float jumpForce = 0.1f;
    public float airControl = 0.2f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.15f;
    public LayerMask groundMask;

    [Header("Dash Settings")]
    public float dashDuration = 0.35f;

    // estado local (inputs)
    private Vector2 lastMoveInput;
    private Vector2 moveInput;
    private Vector3 direction;
    private Vector3 velocity;
    private Vector3 airMomentum;
    private float currentVelocityRef;
    private bool isMoving;
    private bool isAttacking;
    private bool isAttackingLow;
    private bool isJumpPressed;
    private bool DancePressed;
    private bool isCrouchPressed;
    private bool dashFrontPressed;
    private bool dashBackPressed;

    private bool canReceiveInput = true;
    private bool canReceiveInputDash = true;
    private bool canReceiveInputAttack = true;
    private bool canReceiveInputMove = true;
    private bool isDashing = false;
    private bool canDash = true;

    // hitbox script
    private HitboxMultiplayer hitboxScript;
    private HitboxMultiplayer lowHitboxScript;

    // --- NETWORK ---
    public enum PlayerState { Idle, Run, AttackPatada, AttackLow, Jump, Fall, DashFront, DashBack, Stunned, Dead, Crouch, ClickBattle, Dance, PowerUp }

    private NetworkVariable<PlayerState> networkState = new NetworkVariable<PlayerState>(
        PlayerState.Idle,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private NetworkVariable<int> networkVidas = new NetworkVariable<int>(
        3,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<int> networkExtraLives = new NetworkVariable<int>(
        1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private bool isGrounded;
    private bool isInPowerUp = false;
    private static GameObject currentCage;

    [HideInInspector] public HealthSystem uiHealth; // solo se usa en owner local para actualizar UI

    // ----------------------------
    // Unity
    // ----------------------------
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        myAnimator = GetComponent<Animator>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (kickHitbox != null)
        {
            hitboxScript = kickHitbox.GetComponent<HitboxMultiplayer>();
            if (hitboxScript != null)
            {
                hitboxScript.ownerPlayer = this;
                kickHitbox.SetActive(false);
            }
        }

        if (lowKickHitbox != null)
        {
            lowHitboxScript = lowKickHitbox.GetComponent<HitboxMultiplayer>();
            if (lowHitboxScript != null)
            {
                lowHitboxScript.ownerPlayer = this;
                lowKickHitbox.SetActive(false);
            }
        }

        if (GameManagerMultiplayer.Instance != null)
        {
            GameManagerMultiplayer.Instance.RegisterPlayer(this);
        }

        if (IsServer)
        {
            networkVidas.Value = networkVidas.Value <= 0 ? 3 : networkVidas.Value;
            networkExtraLives.Value = networkExtraLives.Value <= 0 ? 1 : networkExtraLives.Value;
        }

        networkVidas.OnValueChanged += OnVidasChanged;
        networkState.OnValueChanged += OnStateChanged;
    }

    private void OnDestroy()
    {
        networkVidas.OnValueChanged -= OnVidasChanged;
        networkState.OnValueChanged -= OnStateChanged;
    }

    // ----------------------------
    // Update loop
    // ----------------------------
    private void Update()
    {
        if (sliderDance != null)
            sliderDance.value = dancePoints;

        if (networkState.Value == PlayerState.ClickBattle)
            return;

        if (IsOwner)
            ProcessOwnerUpdate();

        UpdateAnimations();
    }

    // ----------------------------
    // Owner movement & input
    // ----------------------------
    private void ProcessOwnerUpdate()
    {
        bool physGrounded = false;
        if (groundCheck != null) physGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        bool controllerGrounded = controller != null && controller.isGrounded;
        isGrounded = physGrounded || controllerGrounded;

        if (isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        Vector3 inputDir = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        if (isGrounded)
        {
            direction = inputDir;
            airMomentum = direction.magnitude > 0.1f ? direction * moveSpeed : Vector3.zero;
        }
        else
        {
            direction = Vector3.Lerp(airMomentum.normalized, inputDir, airControl).normalized;
        }

        if (direction.sqrMagnitude > 0.01f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref currentVelocityRef, smoothTime);

            if (model != null) model.rotation = Quaternion.Euler(0f, angle, 0f);
            else transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir * moveSpeed * Time.deltaTime);
        }

        if (!isDashing)
            velocity.y += gravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);

        UpdateStateFromInputs();
    }

    private void UpdateStateFromInputs()
    {
        if (!canReceiveInput || isDefinitivelyDead) return;

        PlayerState newState = DetermineState();

        if (newState != networkState.Value)
        {
            networkState.Value = newState;
            ResetInputsIfNeeded(newState);

            switch (newState)
            {
                case PlayerState.DashFront:
                    StartCoroutine(PerformDash(transform.forward, "DashFront", 2.5f, dashFrontEffectPrefab));
                    break;
                case PlayerState.DashBack:
                    StartCoroutine(PerformDash(-transform.forward, "DashBack", 2.5f, dashBackEffectPrefab));
                    break;
                case PlayerState.AttackPatada:
                    myAnimator.Play("AttackPatada");
                    EnableVfxPatada();
                    if (kickHitbox != null) kickHitbox.SetActive(true);
                    break;
                case PlayerState.AttackLow:
                    myAnimator.Play("AttackLow");
                    EnableVfxLowKick();
                    if (lowKickHitbox != null) lowKickHitbox.SetActive(true);
                    break;
                case PlayerState.Jump:
                    if (isGrounded)
                    {
                        velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
                        myAnimator.Play("Jump");
                    }
                    break;
                case PlayerState.Stunned:
                    ApplyStunVfx();
                    break;
                case PlayerState.PowerUp:
                    ApplyPowerUpVfx();
                    break;
                case PlayerState.ClickBattle:
                    SpawnCage();
                    break;
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

    private void ResetInputsIfNeeded(PlayerState state)
    {
        if (state == PlayerState.Jump || state == PlayerState.AttackPatada ||
            state == PlayerState.DashFront || state == PlayerState.DashBack ||
            state == PlayerState.Crouch || state == PlayerState.Dance ||
            state == PlayerState.AttackLow)
        {
            ResetInputs();
        }
    }

    // ----------------------------
    // Animaciones (todos)
    // ----------------------------
    private void UpdateAnimations()
    {
        switch (networkState.Value)
        {
            case PlayerState.Idle:
                myAnimator.SetBool("RUN", false);
                myAnimator.SetBool("Hit", false);
                myAnimator.SetBool("Falling", false);
                break;
            case PlayerState.Run:
                myAnimator.SetBool("RUN", true);
                myAnimator.Play("RUN");
                break;
            case PlayerState.AttackPatada:
                myAnimator.Play("AttackPatada");
                break;
            case PlayerState.AttackLow:
                myAnimator.Play("AttackLow");
                break;
            case PlayerState.Jump:
                if (isGrounded) myAnimator.Play("Jump");
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
            case PlayerState.PowerUp:
                myAnimator.Play("PowerUp");
                break;
            case PlayerState.ClickBattle:
                myAnimator.Play("ClickBattle");
                break;
        }

        if (isGrounded && networkState.Value != PlayerState.Jump && networkState.Value != PlayerState.Fall)
            myAnimator.SetBool("Falling", false);
    }

    // ----------------------------
    // Inputs (New Input System) - solo Owner
    // ----------------------------
    private void OnMove(InputValue value)
    {
        if (!IsOwner) return;
        Vector2 v = value.Get<Vector2>();
        lastMoveInput = v;
        if (!canReceiveInput && !canReceiveInputMove) return;
        moveInput = v;
        isMoving = moveInput.magnitude > 0.1f;
    }

    private void OnCrouch(InputValue value)
    {
        if (!IsOwner || !canReceiveInput) return;
        if (value.isPressed && isGrounded) isCrouchPressed = true;
    }

    private void OnJump(InputValue value)
    {
        if (!IsOwner || !canReceiveInput) return;
        if (value.isPressed && isGrounded) isJumpPressed = true;
    }

    private void OnAttack(InputValue value)
    {
        if (!IsOwner || (!canReceiveInput && !canReceiveInputAttack)) return;
        if (value.isPressed) isAttacking = true;
    }

    private void OnAttackLow(InputValue value)
    {
        if (!IsOwner || !canReceiveInput) return;
        if (value.isPressed && isGrounded) isAttackingLow = true;
    }

    private void OnDashFront(InputValue value)
    {
        if (!IsOwner || (!canReceiveInput && !canReceiveInputDash)) return;
        if (value.isPressed) dashFrontPressed = true;
    }

    private void OnDashBack(InputValue value)
    {
        if (!IsOwner || (!canReceiveInput && !canReceiveInputDash)) return;
        if (value.isPressed) dashBackPressed = true;
    }

    private void OnDance(InputValue value)
    {
        if (!IsOwner) return;
        if (value.isPressed) DancePressed = true;
    }

    // ----------------------------
    // AnimationEvents públicos (owner)
    // ----------------------------
    public void EnableVfxPatada()
    {
        if (!IsOwner) return;
        if (PatadaEffectPrefab != null)
        {
            Vector3 spawnPos = transform.position;
            Quaternion spawnRot = transform.rotation * Quaternion.Euler(90, 0, 0);
            Instantiate(PatadaEffectPrefab, spawnPos, spawnRot);
            PlayKickVfxClientRpc(spawnPos, spawnRot);
        }
    }

    private void EnableVfxLowKick()
    {
        if (!IsOwner) return;
        if (PatadaEffectPrefab != null)
        {
            Vector3 spawnPos = transform.position;
            Quaternion spawnRot = transform.rotation * Quaternion.Euler(90, 0, 0);
            Instantiate(PatadaEffectPrefab, spawnPos, spawnRot);
            PlayLowKickVfxClientRpc(spawnPos, spawnRot);
        }
    }

    private void ApplyStunVfx()
    {
        if (!IsOwner) return;
        if (StunEffectPrefab != null)
        {
            GameObject fx = Instantiate(StunEffectPrefab, transform.position + Vector3.up * 1.5f, Quaternion.identity);
            Destroy(fx, 2f);
            PlayStunVfxClientRpc(transform.position + Vector3.up * 1.5f);
        }
    }

    private void ApplyPowerUpVfx()
    {
        if (!IsOwner) return;
        if (PowerUpEffectPrefab != null)
        {
            GameObject fx = Instantiate(PowerUpEffectPrefab, transform.position, Quaternion.identity);
            Destroy(fx, 2f);
            PlayPowerUpVfxClientRpc(transform.position);
        }
    }

    private void SpawnCage()
    {
        if (!IsOwner) return;
        if (CagePrefab != null && currentCage == null)
        {
            currentCage = Instantiate(CagePrefab, transform.position, Quaternion.identity);
            SpawnCageClientRpc(transform.position, Quaternion.identity);
        }
    }

    // ----------------------------
    // DASH local del owner
    // ----------------------------
    private IEnumerator PerformDash(Vector3 dashDirection, string animName, float dashDistance, GameObject dashVfxPrefab)
    {
        if (!IsOwner) yield break;

        isDashing = true;
        canDash = false;

        float dashTime = dashDuration;
        float elapsedTime = 0f;

        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + dashDirection.normalized * dashDistance;

        if (dashVfxPrefab != null)
            PlayDashVfxClientRpc(controller.transform.position, true); // true = DashFront, false = DashBack


        myAnimator.Play(animName);

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
        if (isGrounded) velocity.y = -2f;
        networkState.Value = isGrounded ? PlayerState.Idle : PlayerState.Fall;
    }

    // ----------------------------
    // DAÑO: server authoritative
    // ----------------------------
    [ServerRpc(RequireOwnership = false)]
    public void ApplyDamageServerRpc(int damage, ulong attackerClientId, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        networkVidas.Value = Mathf.Max(0, networkVidas.Value - damage);

        var target = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { OwnerClientId } } };
        UpdateHealthClientRpc(networkVidas.Value, target);

        if (networkVidas.Value <= 0)
        {
            if (networkExtraLives.Value > 0)
            {
                networkExtraLives.Value = Mathf.Max(0, networkExtraLives.Value - 1);
                networkVidas.Value = 3;
                UpdateHealthClientRpc(networkVidas.Value, target);
                networkState.Value = PlayerState.Idle;
                PlayHitClientRpc();
            }
            else
            {
                isDefinitivelyDead = true;
                networkState.Value = PlayerState.Dead;
                if (GameManagerMultiplayer.Instance != null)
                    GameManagerMultiplayer.Instance.OnPlayerDied(this);
            }
        }
        else
        {
            PlayHitClientRpc();
            RequestHitReactClientRpc(target);
        }
    }

    [ClientRpc]
    private void UpdateHealthClientRpc(int newHealth, ClientRpcParams clientRpcParams = default)
    {
        if (IsOwner)
        {
            if (uiHealth != null)
            {
                uiHealth.health = newHealth;
                uiHealth.ResetHealth();
            }
            if (contadorVida != null)
                contadorVida.text = newHealth.ToString();
        }
    }

    [ClientRpc]
    private void PlayHitClientRpc(ClientRpcParams clientRpcParams = default)
    {
        myAnimator.SetBool("Hit", true);
    }

    [ClientRpc]
    private void RequestHitReactClientRpc(ClientRpcParams clientRpcParams = default)
    {
        if (!IsOwner) return;
        StartCoroutine(PerformDash(transform.forward, "DashFront", 1.5f, null));
    }

    // ----------------------------
    // VFX RPCs
    // ----------------------------
    [ClientRpc]
    private void PlayKickVfxClientRpc(Vector3 pos, Quaternion rot)
    {
        if (IsOwner) return;
        Instantiate(PatadaEffectPrefab, pos, rot);
    }

    [ClientRpc]
    private void PlayLowKickVfxClientRpc(Vector3 pos, Quaternion rot)
    {
        if (IsOwner) return;
        Instantiate(PatadaEffectPrefab, pos, rot);
    }

    [ClientRpc]
    private void PlayDashVfxClientRpc(Vector3 pos, bool frontDash)
    {
        if (IsOwner) return;
        GameObject prefabToUse = frontDash ? dashFrontEffectPrefab : dashBackEffectPrefab;
        if (prefabToUse != null)
            Instantiate(prefabToUse, pos, Quaternion.identity);
    }


    [ClientRpc]
    private void PlayStunVfxClientRpc(Vector3 pos)
    {
        if (IsOwner || StunEffectPrefab == null) return;
        Instantiate(StunEffectPrefab, pos, Quaternion.identity);
    }

    [ClientRpc]
    private void PlayPowerUpVfxClientRpc(Vector3 pos)
    {
        if (IsOwner || PowerUpEffectPrefab == null) return;
        Instantiate(PowerUpEffectPrefab, pos, Quaternion.identity);
    }

    [ClientRpc]
    private void SpawnCageClientRpc(Vector3 pos, Quaternion rot)
    {
        if (IsOwner || CagePrefab == null || currentCage != null) return;
        currentCage = Instantiate(CagePrefab, pos, rot);
    }

    // ----------------------------
    // Helpers / Animation events
    // ----------------------------
    public void CanReceive()
    {
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
        if (!IsOwner) return;
        canReceiveInputAttack = true;
    }

    public void CanMove()
    {
        if (!IsOwner) return;
        canReceiveInputMove = true;
    }

    public void CanReceiveDash()
    {
        if (!IsOwner) return;
        canReceiveInputDash = true;
    }

    public void StopMove()
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

    public int vidas
    {
        get => networkVidas.Value;
        set { if (IsServer) networkVidas.Value = value; }
    }

    public int lives
    {
        get => networkExtraLives.Value;
        set { if (IsServer) networkExtraLives.Value = value; }
    }

    [HideInInspector] public bool isDefinitivelyDead = false;

    private void OnVidasChanged(int oldV, int newV)
    {
        if (contadorVida != null && IsOwner)
            contadorVida.text = newV.ToString();
    }

    private void OnStateChanged(PlayerState oldS, PlayerState newS) { }

    private bool AttackDone => true;
}
