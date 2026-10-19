using UnityEngine;

/// Desktop ray-casting manipulation at an offset distance.
/// Left-click on a sorting item to pick it up: a ray is cast from the camera through
/// the mouse cursor, and the item then floats along that ray at a fixed distance
/// from the camera, following the cursor. Release the mouse button to drop it.
/// The scroll wheel pushes the held item further away or brings it closer.
///
/// Setup: located on the Main Camera (inside the PlayerRig).
/// Other scripts (ExperimentManager / SortingStation) can switch grabbing off with
/// SetGrabbingEnabled(false), for example before Start and when the timer runs out.


public class RaycastGrabber : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Camera used to cast the ray. If empty, the camera on this GameObject is used.")]
    public Camera cam;

    [Header("Ray casting")]
    [Tooltip("How far away an item can be and still be picked up (metres).")]
    public float maxGrabDistance = 6f;

    [Tooltip("Which layers the ray can hit.")]
    public LayerMask grabbableLayers = ~0;

    [Header("Holding")]
    [Tooltip("Closest the held item can be to the camera (metres).")]
    public float minHoldDistance = 0.5f;

    [Tooltip("Furthest the held item can be from the camera (metres).")]
    public float maxHoldDistance = 3f;

    [Tooltip("How much one scroll-wheel notch changes the hold distance.")]
    public float scrollStep = 0.2f;

    [Tooltip("How quickly the held item catches up with the cursor.")]
    public float followSpeed = 15f;

    [Header("Bin assist")]
    [Tooltip("When the cursor points at a bin, float the held item just above the bin's opening, " +
             "so releasing drops it straight in. Removes depth misjudgement from the task.")]
    public bool snapAboveBins = true;

    [Tooltip("Height above the bin's opening where the held item hovers (metres).")]
    public float binHoverHeight = 0.12f;

    [Header("State")]
    [Tooltip("When false, nothing can be picked up (e.g. before Start or after time is up).")]
    public bool grabbingEnabled = true;

    private SortingItem heldItem; // the item currently held, or null if nothing is held
    private float holdDistance;   // current offset distance between the camera and the held item

    void Awake()
    {
        if (cam == null) cam = GetComponent<Camera>();
    }

    void Update()
    {
        // If grabbing was switched off while holding something, drop it.
        if (!grabbingEnabled)
        {
            if (heldItem != null) Release();
            return;
        }

        if (Input.GetMouseButtonDown(0) && heldItem == null) TryGrab();
        if (Input.GetMouseButtonUp(0) && heldItem != null) Release();

        if (heldItem != null) MoveHeldItem();
    }

    
    /// Casts a ray through the mouse cursor and picks up the first SortingItem it hits.
  
    private void TryGrab()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        // QueryTriggerInteraction.Ignore: the ray passes through trigger zones
        // (like the inside of the bins) so they don't block the click.
        if (Physics.Raycast(ray, out RaycastHit hit, maxGrabDistance, grabbableLayers,
                            QueryTriggerInteraction.Ignore))
        {
            SortingItem item = hit.collider.GetComponentInParent<SortingItem>();
            if (item != null)
            {
                heldItem = item;

                // Hold the item at the distance where it was clicked, so it doesn't jump.
                holdDistance = Mathf.Clamp(hit.distance, minHoldDistance, maxHoldDistance);
                heldItem.OnGrabbed();
            }
        }
    }

    
    /// Keeps the held item floating on the mouse ray at the current offset distance.
    private void MoveHeldItem()
    {
        // The scroll wheel moves the item along the ray (further away / closer).
        holdDistance += Input.mouseScrollDelta.y * scrollStep;
        holdDistance = Mathf.Clamp(holdDistance, minHoldDistance, maxHoldDistance);

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Vector3 targetPosition = ray.GetPoint(holdDistance);

        // Bin assist: if the cursor is over a bin, hover the item above that bin instead.
        if (snapAboveBins && TryGetBinHoverPoint(ray, out Vector3 binPoint))
            targetPosition = binPoint;

        // Move smoothly towards the target point
        heldItem.transform.position = Vector3.Lerp(
            heldItem.transform.position, targetPosition, followSpeed * Time.deltaTime);
    }

    /// Finds the point just above the opening of the bin under the cursor, if there is one.
    /// QueryTriggerInteraction.Collide lets the ray hit the bin's trigger zone as well as its walls.
    private bool TryGetBinHoverPoint(Ray ray, out Vector3 point)
    {
        point = Vector3.zero;
        if (!Physics.Raycast(ray, out RaycastHit hit, 20f, ~0, QueryTriggerInteraction.Collide))
            return false;

        SortingBin bin = hit.collider.GetComponentInParent<SortingBin>();
        if (bin == null) return false;

        // The trigger collider on the bin root fills the inside of the bin,
        // so its bounds give the centre and top of the opening.
        Bounds inside = bin.GetComponent<Collider>().bounds;
        point = new Vector3(inside.center.x, inside.max.y + binHoverHeight, inside.center.z);
        return true;
    }

    /// Drops the held item
    private void Release()
    {
        heldItem.OnReleased();
        heldItem = null;
    }


    /// Turns grabbing on or off. Called by the experiment logic.
    public void SetGrabbingEnabled(bool enabled)
    {
        grabbingEnabled = enabled;
    }
}