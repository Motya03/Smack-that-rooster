using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController: MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float maxSpeed = 8f;
    public float dashForce = 12f;
    public float dashCooldown = 1.2f;

    [Header("Combat")]
    public float punchRadius = 1.2f;
    public float punchForce = 10f;
    public LayerMask playerMask;
    public float punchCooldown = 0.5f;

    Rigidbody rb;
    Vector2 moveInput;
    float lastDashTime = -999f;
    float lastPunchTime = -999f;

    void Awake() => rb = GetComponent<Rigidbody>();

    void FixedUpdate()
    {
        // movimiento en plano XZ
        Vector3 wish = new Vector3(moveInput.x, 0, moveInput.y) * moveSpeed;
        Vector3 vel = rb.linearVelocity;
        Vector3 target = new Vector3(wish.x, vel.y, wish.z);
        rb.linearVelocity = Vector3.ClampMagnitude(new Vector3(target.x, 0, target.z), maxSpeed) + Vector3.up * vel.y;
        // opcional: rotar hacia dirección de movimiento
        Vector3 look = new Vector3(wish.x, 0, wish.z);
        if (look.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), 0.2f);
    }

    // Input System
    public void OnMove(InputValue v) => moveInput = v.Get<Vector2>();
    public void OnDash()
    {
        if (Time.time - lastDashTime < dashCooldown) return;
        lastDashTime = Time.time;
        Vector3 dir = new Vector3(moveInput.x, 0, moveInput.y).normalized;
        if (dir.sqrMagnitude < 0.01f) dir = transform.forward; // dash hacia donde miras
        rb.AddForce(dir * dashForce, ForceMode.VelocityChange);
    }

    public void OnPunch()
    {
        if (Time.time - lastPunchTime < punchCooldown) return;
        lastPunchTime = Time.time;

        // esfera delante
        Vector3 center = transform.position + transform.forward * (punchRadius * 0.8f);
        Collider[] hits = Physics.OverlapSphere(center, punchRadius, playerMask, QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            if (h.attachedRigidbody && h.gameObject != gameObject)
            {
                Vector3 dir = (h.transform.position - transform.position).normalized + Vector3.up * 0.2f;
                h.attachedRigidbody.AddForce(dir * punchForce, ForceMode.VelocityChange);
            }
        }
        // TODO: animación/FX
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        if (Application.isPlaying)
            Gizmos.DrawWireSphere(transform.position + transform.forward * (punchRadius * 0.8f), punchRadius);
    }
}