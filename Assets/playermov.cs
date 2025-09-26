using UnityEngine;
using UnityEngine.InputSystem;
public class playermov : MonoBehaviour
{
    private Rigidbody rb;
    
    private float movementX;
    private float movementY;
    public float speed = 1;
    public float jumpForce = 5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 movement = new Vector3(movementX, 0.0f, movementY);
        rb.AddForce(movement * speed);

    }
    private void OnMove(InputValue movementValue)
    {
        Vector2 movementVector = movementValue.Get<Vector2>();

        movementX = movementVector.x;
        movementY = movementVector.y;
    }

    private void OnJump(InputValue value)
    {
        // Solo saltar si se presiona el botón y está en el suelo
        if (value.isPressed && IsGrounded())
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    private bool IsGrounded()
    {
        // Usamos un raycast hacia abajo para detectar si estamos tocando el suelo
        return Physics.Raycast(transform.position, Vector3.down, 1.1f);
    }

}
