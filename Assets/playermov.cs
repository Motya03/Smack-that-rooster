using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class playermov : MonoBehaviour
{
    private Rigidbody rb;

    private Vector2 moveInput; // <-- guardamos el input completo
    public float speed = 5f;
    public float jumpForce = 5f;

    private float defaultSpeed;
    private Coroutine boostCoroutine;

    public float lerpFactor = 0.1f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        defaultSpeed = speed;
    }

    void FixedUpdate()
    {
        Vector3 targetVelocity = new Vector3(moveInput.x * speed, rb.linearVelocity.y, moveInput.y * speed);
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, lerpFactor);
    }

    private void OnMove(InputValue movementValue)
    {
        moveInput = movementValue.Get<Vector2>(); // <-- siempre guarda el input más reciente
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
