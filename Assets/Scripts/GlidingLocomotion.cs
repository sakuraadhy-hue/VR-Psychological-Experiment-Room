using UnityEngine;

/// Desktop gliding navigation for the participant.
/// The player glides smoothly in whatever direction the camera is looking (including up and down).
/// Movement speeds up and slows down gradually instead of starting/stopping instantly, 
/// which gives the gliding feel.
///
/// Controls:
///   W / S  ->  glide forward / backward (in the direction you look)
///   A / D. ->  glide left / right
///   E / Q. ->  rise / descend
///   Left Shift. ->  glide faster
///   Hold right mouse button + move mouse  -> look around
///   Arrow keys  -> turn left/right, look up/down (alternative to the mouse)


[RequireComponent(typeof(CharacterController))]
public class GlidingLocomotion : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The camera that is a child of this rig. If left empty, Camera.main is used.")]
    public Transform cameraTransform;

    [Header("Gliding")]
    [Tooltip("Normal glide speed in metres per second.")]
    public float glideSpeed = 2f;

    [Tooltip("Glide speed while Left Shift is held.")]
    public float fastGlideSpeed = 4f;

    [Tooltip("Speed for rising (E) and descending (Q), in metres per second.")]
    public float verticalSpeed = 1.5f;

    [Tooltip("How quickly the glide reaches full speed / comes to a stop.")]
    public float smoothing = 4f;

    [Header("Looking")]
    [Tooltip("Mouse look sensitivity (degrees per unit of mouse movement).")]
    public float mouseSensitivity = 2f;

    [Tooltip("Turning speed with the arrow keys, in degrees per second.")]
    public float keyTurnSpeed = 90f;

    [Tooltip("Limits for looking up/down so the camera can't flip over.")]
    public float minPitch = -80f;
    public float maxPitch = 80f;

    private CharacterController controller; // moves the rig and stops it passing through walls/table
    private Vector3 currentVelocity;        // smoothed velocity, carried over between frames
    private float yaw;                      // left/right rotation of the whole rig
    private float pitch;                    // up/down rotation of the camera only

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        // Start from the rotation the rig and camera already have in the scene.
        yaw = transform.eulerAngles.y;
        pitch = cameraTransform.localEulerAngles.x;
        if (pitch > 180f) pitch -= 360f;
    }

    void Update()
    {
        HandleLook();
        HandleGlide();
    }

    
    /// Rotates the view with the mouse (while the right button is held) or with the arrow keys.
    
    private void HandleLook()
    {
        // Lock and hide the cursor only while looking, so the left mouse button
        // stays free for grabbing objects and clicking the UI buttons.
        if (Input.GetMouseButtonDown(1)) Cursor.lockState = CursorLockMode.Locked;
        if (Input.GetMouseButtonUp(1))   Cursor.lockState = CursorLockMode.None;

        if (Input.GetMouseButton(1))
        {
            yaw   += Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity; // moving the mouse up looks up
        }

        // Arrow keys as an alternative way to look around.
        if (Input.GetKey(KeyCode.LeftArrow))  yaw   -= keyTurnSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.RightArrow)) yaw   += keyTurnSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.UpArrow))    pitch -= keyTurnSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.DownArrow))  pitch += keyTurnSpeed * Time.deltaTime;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // The rig turns left/right; only the camera tilts up/down.
        // Keeping separate to stop CharacterController from tilting.
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    
    /// Reads WASD/QE and glides the rig smoothly in the direction the camera faces.
   
    private void HandleGlide()
    {
        // 1. Read the keys into -1 / 0 / +1 per axis.
        //    We read keys directly instead of Input.GetAxis("Horizontal"), because those
        //    built-in axes also include the arrow keys, which we use for turning.
        float forward = 0f, sideways = 0f, vertical = 0f;
        if (Input.GetKey(KeyCode.W)) forward  += 1f;
        if (Input.GetKey(KeyCode.S)) forward  -= 1f;
        if (Input.GetKey(KeyCode.D)) sideways += 1f;
        if (Input.GetKey(KeyCode.A)) sideways -= 1f;
        if (Input.GetKey(KeyCode.E)) vertical += 1f;
        if (Input.GetKey(KeyCode.Q)) vertical -= 1f;

        // 2. Build the direction. Using the camera's forward (not the rig's) means that
        //    looking up and pressing W flies you upward.
        Vector3 direction = cameraTransform.forward * forward + cameraTransform.right * sideways;
        if (direction.sqrMagnitude > 1f) direction.Normalize();

        float speed = Input.GetKey(KeyCode.LeftShift) ? fastGlideSpeed : glideSpeed;
        Vector3 targetVelocity = direction * speed + Vector3.up * vertical * verticalSpeed;

        // 3. Ease towards the target velocity instead of jumping straight to it.
        //    To make the motion feel like gliding rather than walking.
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, smoothing * Time.deltaTime);

        // 4. Move. CharacterController.Move slides along walls instead of passing through them.
        controller.Move(currentVelocity * Time.deltaTime);
    }
}
