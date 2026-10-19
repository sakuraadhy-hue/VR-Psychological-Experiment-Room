using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// A clickable 3D physical push button (used for START and the researcher panel).
/// Clicking it with the left mouse button plays a short press animation (the cap moves
/// into the wall and springs back), plays an optional click sound and fires onPressed.
///
/// A button can also be:
///   - latched: it stays partly pushed in (shows which condition is selected);
///   - non-interactable: it ignores clicks and its colour is darkened.
///
/// Setup: on the button root together with a Collider (the click target).
/// The moving part (cap + label) is assigned to pressablePart.
/// Note: OnMouseDown needs the old Input Manager, which is enabled because
/// Active Input Handling is set to "Both" in the Player Settings.
[RequireComponent(typeof(Collider))]
public class RemoteButton : MonoBehaviour
{
    [Header("What happens when the button is pressed")]
    public UnityEvent onPressed = new UnityEvent();

    [Header("Press animation")]
    [Tooltip("The part that moves when pressed (the cap and its label).")]
    public Transform pressablePart;

    [Tooltip("How far the cap moves when pressed, in the button's local space (+Z points into the wall).")]
    public Vector3 pressOffset = new Vector3(0f, 0f, 0.02f);

    [Tooltip("Seconds for the cap to go in (and the same to come back out).")]
    public float pressTime = 0.08f;

    [Tooltip("How deep a latched (selected) button stays pushed in, as a fraction of pressOffset.")]
    [Range(0f, 1f)] public float latchedDepth = 0.6f;

    [Header("Look")]
    [Tooltip("Renderer of the cap, darkened when the button is not interactable.")]
    public Renderer capRenderer;
    public Color disabledTint = new Color(0.35f, 0.35f, 0.35f, 1f);

    [Header("Sound (optional)")]
    public AudioClip clickSound;

    [Header("State")]
    [SerializeField] private bool interactable = true;

    private bool initialised;
    private bool latched;
    private Vector3 restPosition;   // cap position when not pressed
    private Material capMaterial;   // per-button copy, so tinting one button does not tint the others
    private Color baseColor;
    private AudioSource audioSource;
    private Coroutine pressRoutine;

    public bool Interactable => interactable;

    // Where the cap rests: fully out, or partly in when latched.
    private Vector3 RestTarget => restPosition + pressOffset * (latched ? latchedDepth : 0f);

    void Awake()
    {
        Initialise();
    }

    /// Reads the starting state once. Also called from the public methods, because the
    /// ExperimentManager may call them before this object's Awake (e.g. if it starts inactive).
    private void Initialise()
    {
        if (initialised) return;
        initialised = true;

        if (pressablePart != null) restPosition = pressablePart.localPosition;

        if (capRenderer != null)
        {
            capMaterial = capRenderer.material;
            baseColor = capMaterial.color;
        }

        audioSource = GetComponent<AudioSource>();
        if (clickSound != null && audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D sound coming from the button
        }
    }

    // Unity calls this when the left mouse button is pressed while the cursor is over the collider.
    void OnMouseDown()
    {
        Press();
    }

    /// Presses the button (can also be called from code, e.g. for testing).
    public void Press()
    {
        Initialise();
        if (!interactable) return;

        if (pressablePart != null && isActiveAndEnabled)
        {
            if (pressRoutine != null) StopCoroutine(pressRoutine);
            pressRoutine = StartCoroutine(PressAnimation());
        }

        if (audioSource != null && clickSound != null) audioSource.PlayOneShot(clickSound);

        // Fire the action last: it may hide this button (START hides itself).
        onPressed.Invoke();
    }

    /// Moves the cap in, then back to its resting place.
    private IEnumerator PressAnimation()
    {
        Vector3 pressed = restPosition + pressOffset;

        yield return MoveCap(pressablePart.localPosition, pressed);
        yield return MoveCap(pressed, RestTarget);

        pressRoutine = null;
    }

    private IEnumerator MoveCap(Vector3 from, Vector3 to)
    {
        for (float t = 0f; t < pressTime; t += Time.deltaTime)
        {
            pressablePart.localPosition = Vector3.Lerp(from, to, t / pressTime);
            yield return null;
        }
        pressablePart.localPosition = to;
    }

    /// Keeps the button pushed in (true) or released (false).
    public void SetLatched(bool value)
    {
        Initialise();
        latched = value;
        if (pressablePart != null && pressRoutine == null)
            pressablePart.localPosition = RestTarget;
    }

    /// Enables or disables clicking; disabled buttons are shown darker.
    public void SetInteractable(bool value)
    {
        Initialise();
        interactable = value;
        if (capMaterial != null)
            capMaterial.color = value ? baseColor : baseColor * disabledTint;
    }

    void OnDisable()
    {
        // If the button was hidden in the middle of the animation, the coroutine stops,
        // so put the cap back where it belongs.
        pressRoutine = null;
        if (pressablePart != null && initialised)
            pressablePart.localPosition = RestTarget;
    }
}
