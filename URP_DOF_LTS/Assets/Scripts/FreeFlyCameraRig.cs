using UnityEngine;
using UnityEngine.EventSystems; // optional, to ignore UI clicks

public class FreeFlyCameraRig : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float fastMultiplier = 3f;   // hold Shift to move faster
    public float upDownSpeed = 3f;      // Q/E or Space/Ctrl

    [Header("Mouse Look")]
    public float lookSensitivity = 2f;
    public bool requireMouseButton = true;    // hold RMB to look

    private Camera currentCam;
    private float yaw;
    private float pitch;

    // Call this when you switch cameras
    public void AttachTo(Camera cam)
    {
        if (cam == null) return;

        currentCam = cam;

        // Put rig at camera position/rotation
        transform.position = cam.transform.position;
        transform.rotation = cam.transform.rotation;

        // Reset yaw/pitch from current rotation
        Vector3 euler = transform.eulerAngles;
        yaw   = euler.y;
        pitch = euler.x;

        // Parent camera to rig so it follows
        cam.transform.SetParent(transform, worldPositionStays: true);
    }

    private void Update()
    {
        // If no camera yet, try main camera once
        if (currentCam == null && Camera.main != null)
        {
            AttachTo(Camera.main);
        }

        if (currentCam == null) return;

        HandleMouseLook();
        HandleMovement();
    }

    private void HandleMouseLook()
    {
        // Optionally ignore if pointer over UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (requireMouseButton && !Input.GetMouseButton(1)) // RMB
            return;

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        yaw   += mouseX * lookSensitivity;
        pitch -= mouseY * lookSensitivity;
        pitch = Mathf.Clamp(pitch, -89f, 89f);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void HandleMovement()
    {
        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            speed *= fastMultiplier;

        float h = Input.GetAxisRaw("Horizontal"); // A/D
        float v = Input.GetAxisRaw("Vertical");   // W/S

        Vector3 move = new Vector3(h, 0f, v);

        // Up/down: Q/E or Space/Ctrl
        if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Space))
            move.y += 1f;
        if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftControl))
            move.y -= 1f;

        if (move.sqrMagnitude > 1e-5f)
            move = move.normalized * speed * Time.deltaTime;

        transform.Translate(move, Space.Self);
    }
}

