using UnityEngine;
using UnityEngine.UI;

/// One row of the post-trial questionnaire: a question with five buttons (1 to 5).
/// Clicking a button selects that value and highlights it; only one value can be chosen.
///
/// Setup: on the row object inside the SurveyPanel; the five UI Buttons are assigned
/// to options in order (1, 2, 3, 4, 5).
public class LikertQuestion : MonoBehaviour
{
    [Tooltip("The five answer buttons, in order from 1 to 5.")]
    public Button[] options;

    public Color normalColor = new Color(0.22f, 0.25f, 0.32f);
    public Color selectedColor = new Color(0.25f, 0.6f, 1f);

    /// The chosen value from 1 to 5, or 0 if nothing has been chosen yet.
    public int Value { get; private set; }

    public bool HasAnswer => Value > 0;

    /// Fired whenever the selection changes (the SurveyPanel uses it to enable SUBMIT).
    public event System.Action OnValueChanged;

    void Awake()
    {
        // Connect each button to its value. The local copy "value" is needed so each
        // listener remembers its own number instead of the last value of i.
        for (int i = 0; i < options.Length; i++)
        {
            int value = i + 1;
            options[i].onClick.AddListener(() => Select(value));
        }
        RefreshColours();
    }

    public void Select(int value)
    {
        Value = value;
        RefreshColours();
        OnValueChanged?.Invoke();
    }

    /// Removes the answer (used when the survey is shown to a new participant).
    public void Clear()
    {
        Value = 0;
        RefreshColours();
    }

    /// Highlights the selected button and resets the others.
    private void RefreshColours()
    {
        if (options == null) return;
        for (int i = 0; i < options.Length; i++)
        {
            if (options[i] != null && options[i].targetGraphic is Image image)
                image.color = (i + 1 == Value) ? selectedColor : normalColor;
        }
    }
}
