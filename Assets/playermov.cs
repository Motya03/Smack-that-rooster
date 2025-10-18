using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class playermov : NetworkBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 4.5f;       // Velocidad horizontal
    [SerializeField] private float jumpForce = 8f;         // Velocidad vertical inicial (salto corto)
    [SerializeField] private float gravity = -20f;         // Caída rápida
    [SerializeField] private float smoothTime = 0.15f;     // Rotación más suave
    [Range(0f, 1f)][SerializeField] private float airControl = 0.7f; // Control en el aire

    [Header("Referencias")]
    public CharacterController controller;
    public Transform model; // Asigna el modelo 3D si está en un hijo

    private Vector2 moveInput;
    private Vector3 direction;
    private Vector3 velocity;
    private Vector3 airMomentum;
    private float currentVelocity;
    private bool isGrounded;

    private float defaultSpeed;
    private Coroutine boostCoroutine;

    private void Awake()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();

        defaultSpeed = moveSpeed;
    }

    private void Update()
    {
        if (!IsOwner) return;

        // --- Comprobar si está tocando el suelo ---
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
            // En el aire: mezcla entre inercia y input
            direction = Vector3.Lerp(airMomentum.normalized, inputDir, airControl).normalized;
        }

        // --- Rotación y movimiento horizontal ---
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

        // --- Gravedad y movimiento vertical ---
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed && isGrounded)
        {
            // Salto muy corto: asignar velocidad vertical directamente
            velocity.y = jumpForce;
        }
    }

    // --- PowerUp boost ---
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
