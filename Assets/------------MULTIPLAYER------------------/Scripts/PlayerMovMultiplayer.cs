using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Unity.Netcode;

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

    private static Dictionary<ulong, GameObject> playerCages = new Dictionary<ulong, GameObject>();

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
    public NetworkVariable<int> networkDancePoints = new NetworkVariable<int>(0);
    private int localDancePoints;

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
    public float moveSpeed = 5f;
    private float boost = 1f;

    [Header("Dash Settings")]
    public float dashSpeed = 2f;
    public float dashDuration = 0.3f;
    public float dashCooldown = 1f;
    private bool isDashing = false;
    private bool canDash = true;

    [Header("ClickGame")]
    public ulong lastAttackerId;

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

    public enum States { Idle, Run, AttackPatada, Jump, Fall, DashFront, DashBack, Stunned, Dead, Crouch, AttackLow, ClickBattle, Dance, PowerUp }
    public NetworkVariable<int> networkState = new NetworkVariable<int>((int)States.Idle);
    private States localState;

    public Transform model;

    [Header("Vida y Daño")]
    public NetworkVariable<int> networkVidas = new NetworkVariable<int>(3);
    public NetworkVariable<int> networkLives = new NetworkVariable<int>(1);
    public HealthSystemMulti uiHealth;
    public NetworkVariable<bool> networkIsDefinitivelyDead = new NetworkVariable<bool>(false);
    private bool localIsDefinitivelyDead;

    [Header("Hitbox de Ataque")]
    public GameObject kickHitbox;
    private HitboxMultiplayer hitboxScript;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            localState = States.Idle;
            networkState.Value = (int)States.Idle;

            if (sliderDance != null)
            {
                sliderDance.value = 0;
                sliderDance.gameObject.SetActive(false);
            }
        }

        // Subscribe to network variable changes
        networkState.OnValueChanged += OnStateChanged;
        networkDancePoints.OnValueChanged += OnDancePointsChanged;
        networkVidas.OnValueChanged += OnVidasChanged;
        networkIsDefinitivelyDead.OnValueChanged += OnDefinitivelyDeadChanged;
    }

    void Start()
    {
        boostGiven = true;
        controller = GetComponent<CharacterController>();
        myAnimator = GetComponent<Animator>();
        defaultSpeed = moveSpeed;

        localState = States.Idle;

        if (kickHitbox != null)
        {
            hitboxScript = kickHitbox.GetComponent<HitboxMultiplayer>();
            if (hitboxScript != null)
            {
                hitboxScript.SetOwner(OwnerClientId);
                hitboxScript.ActivateHitbox(false);
            }
        }
    }

    private void Update()
    {
        if (!IsOwner && !IsServer) return;

        // Update local state from network
        localState = (States)networkState.Value;
        localDancePoints = networkDancePoints.Value;
        localIsDefinitivelyDead = networkIsDefinitivelyDead.Value;

        // Only owner processes input
        if (IsOwner)
        {
            ProcessLocalUpdate();
        }

        // Everyone processes visual/animation updates
        ProcessVisualUpdate();
    }

    // Método para activar la hitbox durante el ataque
    private void ActivateKickHitbox()
    {
        if (kickHitbox != null && hitboxScript != null)
        {
            kickHitbox.SetActive(true);
            hitboxScript.ActivateHitbox(true);
        }
    }

    // Método para desactivar la hitbox después del ataque
    private void DeactivateKickHitbox()
    {
        if (kickHitbox != null && hitboxScript != null)
        {
            hitboxScript.ActivateHitbox(false);
            kickHitbox.SetActive(false);
        }
    }

    private void ProcessLocalUpdate()
    {
        if (localState == States.ClickBattle) return;

        // Ground check
        bool physGrounded = false;
        if (groundCheck != null)
            physGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        bool controllerGrounded = controller != null && controller.isGrounded;
        isGrounded = physGrounded || controllerGrounded;

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        // Input direction
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

        // Horizontal movement and rotation
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

        // Vertical movement and gravity
        if (!isDashing)
            velocity.y += gravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);

        // State machine - Métodos vacíos para estados que no necesitan lógica continua
        switch (localState)
        {
            case States.Idle: Idle(); break;
            case States.Run: Run(); break;
            case States.AttackPatada: AttackPatada(); break; // Método vacío
            case States.AttackLow: AttackLow(); break;       // Método vacío
            case States.Jump: Jump(); break;                 // Método vacío
            case States.Fall: Fall(); break;
            case States.DashFront: DashFront(); break;       // Método vacío
            case States.DashBack: DashBack(); break;         // Método vacío
            case States.Stunned: Stunned(); break;
            case States.Dead: Dead(); break;
            case States.Crouch: Crouch(); break;
            case States.Dance: Dance(); break;               // Método vacío
            case States.PowerUp: PowerUp(); break;
        }
    }

    private void ProcessVisualUpdate()
    {
        // Update slider (all clients)
        if (sliderDance != null)
        {
            sliderDance.value = localDancePoints;
        }

        // Special power up animation
        if (localState == States.PowerUp)
        {
            Vector3 currentMoveDir = new Vector3(moveInput.x, 0f, moveInput.y);
            if (currentMoveDir.magnitude < 0.1f)
                myAnimator.Play("Idle");
            else
                myAnimator.Play("RunFast");
        }
    }

    // --- INPUTS ---
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
        {
            isJumpPressed = true;
            JumpServerRpc();
        }
    }

    private void OnAttack(InputValue value)
    {
        if (!IsOwner) return;
        if (!canReceiveInput && !canReceiveInputAttack) return;

        if (value.isPressed)
        {
            isAttacking = true;
            AttackServerRpc();
        }
    }

    private void OnAttackLow(InputValue value)
    {
        if (!IsOwner) return;
        if (!canReceiveInput) return;
        if (value.isPressed && isGrounded)
        {
            isAttackingLow = true;
            AttackLowServerRpc();
        }
    }

    private void OnDashFront(InputValue value)
    {
        if (!IsOwner) return;
        if (!canReceiveInput && !canReceiveInputDash) return;

        if (value.isPressed)
        {
            dashFrontPressed = true;
            DashFrontServerRpc();
        }
    }

    private void OnDashBack(InputValue value)
    {
        if (!IsOwner) return;
        if (!canReceiveInput && !canReceiveInputDash) return;

        if (value.isPressed)
        {
            dashBackPressed = true;
            DashBackServerRpc();
        }
    }

    private void OnMash(InputValue value)
    {
        if (!IsOwner) return;

        if (localState == States.ClickBattle && value.isPressed)
        {
            RegisterClickServerRpc(OwnerClientId);
        }
    }

    private void OnDance(InputValue value)
    {
        if (!IsOwner) return;

        if (value.isPressed)
        {
            DancePressed = true;
            DanceServerRpc();
        }
    }

    // --- ESTADOS (MÉTODOS DE LÓGICA CONTINUA) ---
    private void Idle()
    {
        if (dashFrontPressed) SetStateServerRpc(States.DashFront);
        if (dashBackPressed) SetStateServerRpc(States.DashBack);
        if (isAttacking) SetStateServerRpc(States.AttackPatada);

        if (!isGrounded) return;

        myAnimator.SetBool("RUN", false);
        myAnimator.SetBool("Hit", false);

        if (isJumpPressed && isGrounded) SetStateServerRpc(States.Jump);
        else if (isMoving) SetStateServerRpc(States.Run);
        else if (isCrouchPressed) SetStateServerRpc(States.Crouch);
        else if (isAttackingLow) SetStateServerRpc(States.AttackLow);
        else if (DancePressed) SetStateServerRpc(States.Dance);

        ResetInputs();
    }

    private void Run()
    {
        SoundManager.StopSound(SoundType.Dance);

        if (dashFrontPressed) SetStateServerRpc(States.DashFront);
        if (dashBackPressed) SetStateServerRpc(States.DashBack);
        if (isAttacking) SetStateServerRpc(States.AttackPatada);
        if (isAttackingLow) SetStateServerRpc(States.AttackLow);

        if (!isGrounded) return;

        myAnimator.SetBool("RUN", true);
        myAnimator.Play("RUN");

        if (!isMoving) SetStateServerRpc(States.Idle);
        else if (isAttacking) SetStateServerRpc(States.AttackPatada);
        else if (isAttackingLow) SetStateServerRpc(States.AttackLow);
        else if (isJumpPressed && isGrounded) SetStateServerRpc(States.Jump);
        else if (dashFrontPressed) SetStateServerRpc(States.DashFront);
        else if (dashBackPressed) SetStateServerRpc(States.DashBack);
        else if (isCrouchPressed) SetStateServerRpc(States.Crouch);
        else if (DancePressed) SetStateServerRpc(States.Dance);

        ResetInputs();
    }

    private void Fall()
    {
        if (isAttacking) SetStateServerRpc(States.AttackPatada);

        myAnimator.SetBool("Falling", true);
        StartCoroutine(AnimBoostCoroutine(0.5f, 0.5f));

        if (isGrounded)
        {
            myAnimator.SetBool("Falling", false);
            SetStateServerRpc(States.Idle);
        }
    }

    private void Crouch()
    {
        if (isGrounded)
        {
            myAnimator.Play("Crouch");
            StopMove();
            SetStateServerRpc(States.Idle);
            ResetInputs();
            SoundManager.PlaySound(SoundType.Crouch);
        }
    }

    private void Stunned()
    {
        myAnimator.Play("Stunned");
        StopMove();

        // Coroutine para salir del stun después de un tiempo
        StartCoroutine(ExitStunAfterDelay(2f));
    }

    private void Dead()
    {
        myAnimator.Play("Dead");
        SoundManager.PlaySound(SoundType.Dead);
        StopMove();
    }

    private void PowerUp()
    {
        if (isInPowerUp) return;

        isInPowerUp = true;
        StartCoroutine(PowerSpeed());
    }

    // --- ESTADOS (MÉTODOS VACÍOS - La lógica real está en los RPCs) ---
    private void AttackPatada() { } // Vacío - lógica en AttackPatadaClientRpc()
    private void AttackLow() { }    // Vacío - lógica en AttackLowClientRpc()
    private void Jump() { }         // Vacío - lógica en JumpClientRpc()
    private void DashFront() { }    // Vacío - lógica en DashFrontClientRpc()
    private void DashBack() { }     // Vacío - lógica en DashBackClientRpc()
    private void Dance() { }        // Vacío - lógica en DanceClientRpc()

    // --- RPCs PARA ESTADOS ---
    [ServerRpc]
    private void JumpServerRpc()
    {
        networkState.Value = (int)States.Jump;
        JumpClientRpc();
    }

    [ClientRpc]
    private void JumpClientRpc()
    {
        if (IsOwner)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            isJumpPressed = false;
            myAnimator.Play("Jump");
            SoundManager.PlaySound(SoundType.Jump);
            StartCoroutine(AnimBoostCoroutine(0.5f, 1f));
            StartCoroutine(JumpRoutine());
        }
    }

    private IEnumerator JumpRoutine()
    {
        yield return new WaitForSeconds(0.05f);
        SetStateServerRpc(States.Idle);
    }

    [ServerRpc]
    private void AttackServerRpc()
    {
        networkState.Value = (int)States.AttackPatada;
        AttackPatadaClientRpc();
    }

    [ClientRpc]
    private void AttackPatadaClientRpc()
    {
        if (IsOwner && isGrounded)
        {
            if (!AttackDone) return;
            isGrounded = false;
            isAttacking = false;
            AttackDone = false;

            myAnimator.Play("AttackPatada");
            SoundManager.PlaySound(SoundType.AttackPrime);

            // Activar hitbox
            if (IsServer)
            {
                ActivateKickHitbox();
            }

            StartCoroutine(AnimBoostCoroutine(0.3f, 0.8f));

            // Desactivar hitbox después de un tiempo
            StartCoroutine(DeactivateHitboxAfterDelay(0.5f));
        }
        else if (IsOwner)
        {
            isAttacking = false;
            myAnimator.Play("AttackCircAire");
            StopMove();
            SetStateServerRpc(States.Idle);
            SoundManager.PlaySound(SoundType.AttackPrime);
        }
    }

    private IEnumerator DeactivateHitboxAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (IsServer)
        {
            DeactivateKickHitbox();
        }
    }

    [ServerRpc]
    private void DanceServerRpc()
    {
        networkState.Value = (int)States.Dance;
        DanceClientRpc();
    }

    [ClientRpc]
    private void DanceClientRpc()
    {
        if (IsOwner)
        {
            if (sliderDance != null)
            {
                sliderDance.gameObject.SetActive(true);
            }
            myAnimator.Play("Dance");
            SoundManager.PlaySound(SoundType.Dance);
            StopMove();

            // Only server handles the dance points logic
            if (IsServer)
            {
                DanceSliderLogic();
            }
        }
    }

    private void DanceSliderLogic()
    {
        if (localDancePoints >= 3)
        {
            player = this.gameObject;
            player.gameObject.tag = "Invicible";
            StartCoroutine(TemporaryTagRoutine());

            myAnimator.Play("IDLE");

            // Find all player objects with the tag "Player"
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            List<GameObject> playerList = new List<GameObject>(players);

            // Remove self from list
            playerList.Remove(this.gameObject);

            if (playerList.Count > 0)
            {
                // Choose random player to attack
                GameObject targetPlayer = playerList[Random.Range(0, playerList.Count)];

                // Here you would trigger the attack on the target player
                // You need to implement this based on your game logic
            }

            networkDancePoints.Value = 0;
            if (sliderDance != null)
            {
                sliderDance.gameObject.SetActive(false);
            }
        }
        else
        {
            networkDancePoints.Value = localDancePoints + 1;
        }
    }

    private IEnumerator TemporaryTagRoutine()
    {
        yield return new WaitForSeconds(3f);
        player.gameObject.tag = "Player";
    }

    [ServerRpc]
    private void AttackLowServerRpc()
    {
        networkState.Value = (int)States.AttackLow;
        AttackLowClientRpc();
    }

    [ClientRpc]
    private void AttackLowClientRpc()
    {
        if (IsOwner)
        {
            StartCoroutine(PerformDash(transform.forward, "AttackLow", 1.8f));
            StopMove();
            SetStateServerRpc(States.Idle);
            SoundManager.PlaySound(SoundType.AttackSecond);
        }
    }

    [ServerRpc]
    private void DashFrontServerRpc()
    {
        networkState.Value = (int)States.DashFront;
        DashFrontClientRpc();
    }

    [ClientRpc]
    private void DashFrontClientRpc()
    {
        if (IsOwner && !isDashing && canDash)
        {
            dashFrontPressed = false;
            SoundManager.PlaySound(SoundType.Dash);
            SpawnDashFrontFX();
            StartCoroutine(AnimBoostCoroutine(0.5f, 0.5f));
            StopMove();
            StartCoroutine(PerformDash(transform.forward, "DashFront", 2.5f));
        }
    }

    [ServerRpc]
    private void DashBackServerRpc()
    {
        networkState.Value = (int)States.DashBack;
        DashBackClientRpc();
    }

    [ClientRpc]
    private void DashBackClientRpc()
    {
        if (IsOwner && !isDashing && canDash)
        {
            dashBackPressed = false;
            SoundManager.PlaySound(SoundType.Dash);
            SpawnDashBackFX();
            StartCoroutine(AnimBoostCoroutine(0.5f, 0.5f));
            StopMove();
            StartCoroutine(PerformDash(-transform.forward, "DashBack", 2.5f));
        }
    }

    // --- UTILITIES ---
    [ServerRpc]
    public void SetStateServerRpc(States newState)
    {
        if ((States)networkState.Value == States.PowerUp && isInPowerUp) return;

        networkState.Value = (int)newState;

        if (newState == States.ClickBattle)
        {
            ClickBattle();
        }

        if (newState != States.Idle)
        {
            if (sliderDance != null)
            {
                networkDancePoints.Value = 0;
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

    public void CanReceive()
    {
        canReceiveInput = true;
        moveInput = lastMoveInput;
        isMoving = moveInput.magnitude > 0.1f;
    }

    private IEnumerator ExitStunAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (IsServer)
        {
            SetStateServerRpc(States.Idle);
            CanReceive(); // Reactivar controles
        }
    }

    private IEnumerator PerformDash(Vector3 dashDirection, string animName, float dashDistance)
    {
        SoundManager.StopSound(SoundType.StunStars);
        isDashing = true;
        canDash = false;

        myAnimator.Play(animName);

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

        SetStateServerRpc(isGrounded ? States.Idle : States.Fall);
        ResetInputs();
    }

    // --- NETWORK VARIABLE HANDLERS ---
    private void OnStateChanged(int oldState, int newState)
    {
        localState = (States)newState;
    }

    private void OnDancePointsChanged(int oldPoints, int newPoints)
    {
        localDancePoints = newPoints;
    }

    private void OnVidasChanged(int oldVidas, int newVidas)
    {
        if (uiHealth != null)
        {
            // Update UI health system
            int damage = oldVidas - newVidas;
            if (damage > 0)
            {
                uiHealth.TakeDamage(damage);
            }
        }
    }

    private void OnDefinitivelyDeadChanged(bool oldValue, bool newValue)
    {
        localIsDefinitivelyDead = newValue;
        if (newValue)
        {
            SetStateServerRpc(States.Dead);
        }
    }

    // --- HIT SYSTEM ---
    [ServerRpc(RequireOwnership = false)]
    public void TakeHitServerRpc(int damage, ulong attackerId)
    {
        if (localIsDefinitivelyDead) return;

        SoundManager.PlaySound(SoundType.HitCulo);
        lastAttackerId = attackerId;

        int newVidas = networkVidas.Value - damage;
        networkVidas.Value = Mathf.Max(0, newVidas);

        if (networkVidas.Value <= 0)
        {
            if (networkLives.Value > 0)
            {
                // Enter click battle
                ClickGameManagerMulti.Instance.StartBattleServerRpc(attackerId, OwnerClientId);
            }
            else
            {
                // Definitive death
                networkIsDefinitivelyDead.Value = true;
                networkState.Value = (int)States.Dead;

                // Notify game manager
                GameManagerMultiplayer.Instance.CheckRemainingPlayersServerRpc();
            }
        }
        else
        {
            TakeHitVisualClientRpc();
        }
    }

    [ClientRpc]
    private void TakeHitVisualClientRpc()
    {
        myAnimator.SetBool("Hit", true);
        if (IsOwner)
        {
            StartCoroutine(PerformDash(transform.forward, "DashFront", 1.5f));
        }
    }

    // --- CLICK BATTLE ---
    private void ClickBattle()
    {
        if (playerCages.ContainsKey(OwnerClientId)) return;

        StopMove();
        myAnimator.Play("IDLE");
        SpawnCageServerRpc(OwnerClientId);
    }

    [ServerRpc]
    private void SpawnCageServerRpc(ulong playerId)
    {
        if (playerCages.ContainsKey(playerId)) return;

        var playerObj = NetworkManager.Singleton.ConnectedClients[playerId].PlayerObject;
        Vector3 spawnPos = playerObj.transform.position;

        // Spawn cage as network object
        var cageObj = Instantiate(CagePrefab, spawnPos, playerObj.transform.rotation);
        var networkCage = cageObj.GetComponent<NetworkObject>();
        networkCage.Spawn();

        playerCages[playerId] = cageObj;

        SpawnCageClientRpc(playerId, networkCage.NetworkObjectId);
    }

    [ClientRpc]
    private void SpawnCageClientRpc(ulong playerId, ulong cageNetworkId)
    {
        // Client-side cage tracking if needed
    }

    public void CageGone()
    {
        RemoveCageServerRpc(OwnerClientId);
    }

    [ServerRpc]
    private void RemoveCageServerRpc(ulong playerId)
    {
        if (playerCages.ContainsKey(playerId))
        {
            var cage = playerCages[playerId];
            if (cage != null)
            {
                var networkCage = cage.GetComponent<NetworkObject>();
                if (networkCage != null)
                {
                    networkCage.Despawn();
                }
                Destroy(cage);
            }
            playerCages.Remove(playerId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RegisterClickServerRpc(ulong playerId)
    {
        ClickGameManagerMulti.Instance.RegisterClickServerRpc(playerId);
    }

    // --- BOOST SYSTEM ---
    public void ActivarSpeedBoost(float amountBoost, float duration)
    {
        if (!IsOwner) return;

        boost = amountBoost;
        boostGiven = false;
        StartCoroutine(SpeedBoostCoroutine(boost, duration));
    }

    private IEnumerator SpeedBoostCoroutine(float amount, float duration)
    {
        if (!boostGiven) yield return new WaitForSeconds(0.5f);

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
        if (localState == States.PowerUp)
        {
            AttackDone = true;
            yield break;
        }

        if (!boostGiven)
        {
            AttackDone = true;
            yield break;
        }

        moveSpeed = defaultSpeed * amount;
        yield return new WaitForSeconds(duration);

        if (localState == States.PowerUp)
        {
            AttackDone = true;
            yield break;
        }

        moveSpeed = defaultSpeed;
        boostCoroutine = null;

        if (localState == States.AttackPatada)
        {
            AttackDone = true;
            SetStateServerRpc(States.Idle);
        }

        AttackDone = true;
    }

    private IEnumerator PowerSpeed()
    {
        StopMove();
        moveSpeed = 15;

        yield return new WaitForSeconds(3);

        moveSpeed = defaultSpeed;
        isInPowerUp = false;
        SetStateServerRpc(States.Idle);
    }

    // --- VISUAL EFFECTS ---
    private void SpawnDashFrontFX()
    {
        Vector3 spawnPos = dashPointFront != null ? dashPointFront.position : transform.position + Vector3.up * 0.5f;
        Quaternion spawnRot = Quaternion.LookRotation(transform.forward) * Quaternion.Euler(0, 180, 0);

        // You might want to spawn this as a network object
        // GameObject fx = Instantiate(dashFrontEffectPrefab, spawnPos, spawnRot);
    }

    private void SpawnDashBackFX()
    {
        Vector3 spawnPos = dashPointBack != null ? dashPointBack.position : transform.position + Vector3.up * 0.5f;
        Quaternion spawnRot = Quaternion.LookRotation(transform.forward) * Quaternion.Euler(0, 0, 0);

        // You might want to spawn this as a network object
        // GameObject fx = Instantiate(dashBackEffectPrefab, spawnPos, spawnRot);
    }

    // --- CLEANUP ---
    public override void OnDestroy()
    {
        base.OnDestroy();

        // Unsubscribe from network variable changes
        networkState.OnValueChanged -= OnStateChanged;
        networkDancePoints.OnValueChanged -= OnDancePointsChanged;
        networkVidas.OnValueChanged -= OnVidasChanged;
        networkIsDefinitivelyDead.OnValueChanged -= OnDefinitivelyDeadChanged;

        // Remove from player lists
        if (PlayerSpawnMultiplayer.Instance != null)
        {
            // Usar el método GetClientIdByPlayer que ya existe
            ulong clientId = PlayerSpawnMultiplayer.Instance.GetClientIdByPlayer(gameObject);
            if (clientId != 999)
            {
                PlayerSpawnMultiplayer.Instance.RemovePlayer(clientId);
            }
        }
    }
}