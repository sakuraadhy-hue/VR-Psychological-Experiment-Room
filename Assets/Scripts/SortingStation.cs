using UnityEngine;

// Groups the whole sorting task (table, bins, items) and gives the ExperimentManager
// simple controls: reset everything, switch grabbing on/off, and count remaining items.
//
// Setup: located on the SortingStation prefab root. All SortingItems must be its children.

public class SortingStation : MonoBehaviour
{
    [Tooltip("The participant's grabber. If left empty, it is found automatically in the scene.")]
    public RaycastGrabber grabber;

    private SortingItem[] items;

    void Awake()
    {
        // true = also find items that are currently hidden (already sorted).
        items = GetComponentsInChildren<SortingItem>(true);

        if (grabber == null) grabber = FindFirstObjectByType<RaycastGrabber>();
    }

    // Puts every item back in its starting place. Call this from the Reset button.
    public void ResetStation()
    {
        foreach (SortingItem item in items) item.ResetItem();
    }

    // Allows or blocks picking up items
    // (e.g. blocked before Start, allowed during the trial, blocked when time runs out).

    public void SetGrabbingEnabled(bool enabled)
    {
        if (grabber != null) grabber.SetGrabbingEnabled(enabled);
    }

    // How many items are still on the table (not yet sorted).
    public int RemainingItems
    {
        get
        {
            int count = 0;
            foreach (SortingItem item in items)
                if (item.gameObject.activeSelf) count++;
            return count;
        }
    }
}
