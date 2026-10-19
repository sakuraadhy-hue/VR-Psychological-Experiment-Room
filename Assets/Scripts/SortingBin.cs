using UnityEngine;

// A sorting bin. Its trigger collider detects items dropped into it,
// checks whether the colour matches, plays a 3D feedback sound, hides the item,
// and announces the result so the ExperimentManager can update the score and errors.
//
// Setup: this is located on the bin's parent object, together with a BoxCollider
// marked "Is Trigger" that fills the inside of the bin

[RequireComponent(typeof(Collider))]
public class SortingBin : MonoBehaviour
{

    // Fired every time an item lands in any bin.
    // The bool is true for a correct match and false for an error.
    // The ExperimentManager listens to it with:  SortingBin.OnItemSorted += HandleItemSorted;

    public static event System.Action<bool> OnItemSorted;

    [Tooltip("The colour of item that belongs in this bin.")]
    public ItemColor acceptedColor = ItemColor.Red;

    [Header("Feedback sounds (optional)")]
    [Tooltip("Short chime played when the item matches this bin.")]
    public AudioClip correctSound;

    [Tooltip("Short buzzer played when the item is in the wrong bin.")]
    public AudioClip wrongSound;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        // Making sure collider detects items instead of physically blocking them.
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        SortingItem item = other.GetComponentInParent<SortingItem>();

        // Ignore anything that isn't a sorting item, or an item still being held.
        if (item == null || item.IsHeld) return;

        bool isCorrect = item.itemColor == acceptedColor;
        Debug.Log($"{item.itemColor} item dropped in {acceptedColor} bin: {(isCorrect ? "correct" : "error")}");

        PlayFeedback(isCorrect);

        // Hide the item instead of destroying it, so Reset can bring it back.
        item.gameObject.SetActive(false);

        // Tell any listeners (the ExperimentManager) what happened.
        OnItemSorted?.Invoke(isCorrect);
    }

    // Plays the chime or buzzer from the bin's position in 3D space.
    private void PlayFeedback(bool isCorrect)
    {
        AudioClip clip = isCorrect ? correctSound : wrongSound;
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }
}