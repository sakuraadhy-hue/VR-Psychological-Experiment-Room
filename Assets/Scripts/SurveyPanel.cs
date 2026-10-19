using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// The post-trial questionnaire shown on the world-space canvas when the trial ends.
/// It holds three Likert questions (Stress, Focus, Comfort). SUBMIT is only clickable
/// once all three are answered; it then sends the answers to the ExperimentManager,
/// which saves them to results.csv.
///
/// Setup: on the SurveyPanel object inside ExperimentUI.
public class SurveyPanel : MonoBehaviour
{
    public ExperimentManager manager;

    [Header("Questions")]
    public LikertQuestion stressQuestion;
    public LikertQuestion focusQuestion;
    public LikertQuestion comfortQuestion;

    [Header("Submit")]
    public Button submitButton;

    [Tooltip("Small message under the Submit button.")]
    public TMP_Text hintText;

    private bool AllAnswered =>
        stressQuestion.HasAnswer && focusQuestion.HasAnswer && comfortQuestion.HasAnswer;

    void Awake()
    {
        stressQuestion.OnValueChanged += Refresh;
        focusQuestion.OnValueChanged += Refresh;
        comfortQuestion.OnValueChanged += Refresh;
        submitButton.onClick.AddListener(Submit);
    }

    void OnDestroy()
    {
        if (stressQuestion != null) stressQuestion.OnValueChanged -= Refresh;
        if (focusQuestion != null) focusQuestion.OnValueChanged -= Refresh;
        if (comfortQuestion != null) comfortQuestion.OnValueChanged -= Refresh;
    }

    /// Shows an empty survey for a new participant.
    public void Show()
    {
        gameObject.SetActive(true);
        stressQuestion.Clear();
        focusQuestion.Clear();
        comfortQuestion.Clear();
        Refresh();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /// Enables SUBMIT only when every question has an answer.
    private void Refresh()
    {
        bool ready = AllAnswered;
        submitButton.interactable = ready;
        if (hintText != null)
            hintText.text = ready ? "" : "Please answer all three questions.";
    }

    private void Submit()
    {
        if (!AllAnswered || manager == null) return;
        manager.SubmitSurvey(stressQuestion.Value, focusQuestion.Value, comfortQuestion.Value);
    }
}
