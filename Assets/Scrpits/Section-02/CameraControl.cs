using UnityEngine;

public class CameraControl : MonoBehaviour
{
    [Header("Target")]
    private Transform target;          

    [Header("Distances")]
    public float distance = 10.0f;    
    public float minDistance = 2.0f;  
    public float maxDistance = 15.0f; 
    public float height = 2.0f;       

    [Header("Input")]
    public float mouseSensitivity = 5.0f;
    public float rotationSmoothTime = 0.1f; 
    public Vector2 pitchLimits = new Vector2(-40, 80); 

    [Header("Collision (Anti-Clip)")]
    public LayerMask collisionLayers; 
    public float collisionRadius = 0.5f; 
    public float collisionOffset = 0.2f; 

    // Private State
    private Vector3 rotationSmoothVelocity;
    private Vector3 currentRotation;
    private float yaw;
    private float pitch;

    void Start()
    {
      
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

       
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        
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

        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, pitchLimits.x, pitchLimits.y);

        Vector3 targetRotation = new Vector3(pitch, yaw);
        currentRotation = Vector3.SmoothDamp(currentRotation, targetRotation, ref rotationSmoothVelocity, rotationSmoothTime);

    
        Vector3 pivotPoint = target.position + Vector3.up * height;

        
        Vector3 direction = Quaternion.Euler(currentRotation) * Vector3.back;
        Vector3 desiredPosition = pivotPoint + (direction * distance);

        
        Vector3 finalPosition = CheckCameraCollision(pivotPoint, desiredPosition);

      
        transform.position = finalPosition;
        transform.LookAt(pivotPoint);
    }

    
    Vector3 CheckCameraCollision(Vector3 pivot, Vector3 desiredPos)
    {
        RaycastHit hit;
        Vector3 dir = desiredPos - pivot;
        float dist = dir.magnitude;

        
        if (Physics.SphereCast(pivot, collisionRadius, dir.normalized, out hit, dist, collisionLayers))
        {
            
            float hitDistance = hit.distance - collisionOffset;
            hitDistance = Mathf.Clamp(hitDistance, minDistance, maxDistance);

            return pivot + (dir.normalized * hitDistance);
        }

        return desiredPos; 
    }
}