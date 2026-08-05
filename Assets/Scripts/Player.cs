using UnityEngine;

public class Player : MonoBehaviour
{
    public float speed = 5f;
    public float runMultiplier = 1.7f;
    public float mouseSensitivity = 2f;
    private Rigidbody rb;
    private Animator anim;
    private float xRotation;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.linearDamping = 0f;
        anim = GetComponentInChildren<Animator>();
        //Cursor.visible = false;
        //Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        transform.Rotate(Vector3.up * mouseX);
        xRotation = Mathf.Clamp(xRotation - mouseY, -80f, 80f);
        if (Camera.main != null)
            Camera.main.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        if (anim != null)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            bool run = Input.GetKey(KeyCode.LeftShift);
            bool moving = Mathf.Abs(h) + Mathf.Abs(v) > 0.01f;
            anim.SetBool("isRun", run && moving);
            anim.SetBool("isWalk", moving);
        }
    }

    private void FixedUpdate()
    {
        if (Time.timeScale == 0f)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            if (anim != null)
            {
                anim.SetBool("isRun", false);
                anim.SetBool("isWalk", false);
            }
            return;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        bool run = Input.GetKey(KeyCode.LeftShift);

        if (Mathf.Abs(h) < 0.01f && Mathf.Abs(v) < 0.01f)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            return;
        }

        Vector3 move = (transform.forward * v + transform.right * h).normalized;
        float currentSpeed = speed * (run ? runMultiplier : 1f);
        rb.linearVelocity = new Vector3(
            move.x * currentSpeed,
            rb.linearVelocity.y,
            move.z * currentSpeed
        );
    }

    public void ResetMovement()
    {
        xRotation = 0f;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (anim != null)
        {
            anim.SetBool("isRun", false);
            anim.SetBool("isWalk", false);
        }

        if (Camera.main != null)
            Camera.main.transform.localRotation = Quaternion.identity;
    }
}
