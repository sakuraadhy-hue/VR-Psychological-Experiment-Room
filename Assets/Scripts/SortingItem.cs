using UnityEngine;

// The three colours used in the sorting task. Shared by SortingItem and SortingBin.

public enum ItemColor { Red, Green, Blue }

// A grabbable ball for the sorting task. It knows its colour, whether it is being held,
// and where it started, so it can be put back when the experiment is reset.
//
// Setup: Sphere + Rigidbody + this script. The colour is set in the Inspector.

[RequireComponent(typeof(Rigidbody))]
public class SortingItem : MonoBehaviour
{
    [Tooltip("Which bin this item belongs in.")]
    public ItemColor itemColor = ItemColor.Red;

    // True while the participant is holding this item.
    public bool IsHeld { get; private set; }

    private Rigidbody rb;
    private Collider col;
    private Vector3 startPosition;
    private Quaternion startRotation;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        // Remember the starting position so ResetItem() can put the item back.
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    // Called by RaycastGrabber when the item is picked up.
    public void OnGrabbed()
    {
        IsHeld = true;

        // Kinematic - physics stops moving it; the grabber script moves it instead.
        rb.isKinematic = true;

        // Collider off while held, so the ball doesn't knock other objects over
        // and isn't counted by a bin while it is only being carried past it.
        col.enabled = false;
    }

    // Called by RaycastGrabber when the item is dropped.
    public void OnReleased()
    {
        IsHeld = false;
        col.enabled = true;
        rb.isKinematic = false; // gravity takes over again and the ball falls

        // Start falling from rest rather than with leftover velocity.
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    // Puts the item back on the table, ready for the next participant.
    public void ResetItem()
    {
        IsHeld = false;
        gameObject.SetActive(true); // bins hide sorted items, so make it visible again
        col.enabled = true;
        rb.isKinematic = false;

        // Move both the Rigidbody and the Transform so physics and visuals agree.
        rb.position = startPosition;
        rb.rotation = startRotation;
        transform.SetPositionAndRotation(startPosition, startRotation);

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}