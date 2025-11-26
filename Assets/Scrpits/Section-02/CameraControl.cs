using UnityEngine;

public class CameraControl : MonoBehaviour
{
    [Header("Target")]
    private Transform target;          // The Player

    [Header("Distances")]
    public float distance = 10.0f;    // Normal distance
    public float minDistance = 2.0f;  // Closest zoom allowed
    public float maxDistance = 15.0f; // Furthest zoom allowed
    public float height = 2.0f;       // Height above player (Pivot)

    [Header("Input")]
    public float mouseSensitivity = 5.0f;
    public float rotationSmoothTime = 0.1f; // Lower = snappier, Higher = floatier
    public Vector2 pitchLimits = new Vector2(-40, 80); // Look down/up limits

    [Header("Collision (Anti-Clip)")]
    public LayerMask collisionLayers; // What layers block the camera?
    public float collisionRadius = 0.5f; // How "thick" is the camera check
    public float collisionOffset = 0.2f; // Buffer from wall

    // Private State
    private Vector3 rotationSmoothVelocity;
    private Vector3 currentRotation;
    private float yaw;
    private float pitch;

    void Start()
    {
        // Auto-find player if not assigned
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        // Lock cursor so it doesn't fly off screen
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Initialize angles
        Vector3 angles = transform.eulerAngles;
        pitch = angles.x;
        yaw = angles.y;
        currentRotation = new Vector3(pitch, yaw);
    }

    void LateUpdate()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;

            }
            return;
        }

        // 1. Input Handling
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, pitchLimits.x, pitchLimits.y);

        // 2. Smooth Rotation Calculation
        Vector3 targetRotation = new Vector3(pitch, yaw);
        currentRotation = Vector3.SmoothDamp(currentRotation, targetRotation, ref rotationSmoothVelocity, rotationSmoothTime);

        // 3. Calculate Pivot (The point above player head)
        Vector3 pivotPoint = target.position + Vector3.up * height;

        // 4. Calculate Ideal Position (Where camera WANTS to be)
        // Math: Rotate 'Back' vector by our rotation, then multiply by distance
        Vector3 direction = Quaternion.Euler(currentRotation) * Vector3.back;
        Vector3 desiredPosition = pivotPoint + (direction * distance);

        // 5. Wall Collision Check (The Fix for Hills)
        Vector3 finalPosition = CheckCameraCollision(pivotPoint, desiredPosition);

        // 6. Apply
        transform.position = finalPosition;
        transform.LookAt(pivotPoint);
    }

    // Checks if something is between Player and Camera
    Vector3 CheckCameraCollision(Vector3 pivot, Vector3 desiredPos)
    {
        RaycastHit hit;
        Vector3 dir = desiredPos - pivot;
        float dist = dir.magnitude;

        // Shoot a sphere backwards from player to camera
        if (Physics.SphereCast(pivot, collisionRadius, dir.normalized, out hit, dist, collisionLayers))
        {
            // If we hit a wall/ground, return the hit point (minus a small buffer)
            float hitDistance = hit.distance - collisionOffset;
            hitDistance = Mathf.Clamp(hitDistance, minDistance, maxDistance);

            return pivot + (dir.normalized * hitDistance);
        }

        return desiredPos; // Nothing hit, stay at full distance
    }
}