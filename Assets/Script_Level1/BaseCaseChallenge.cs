
using UnityEngine;
using TMPro;

public class BaseCaseChallenge : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField answerInput;
    public TMP_Text feedbackText;
    public GameObject submitButton;
    public GameObject continueButton;

    [Header("Game References")]
    public PortalInteraction portalInteraction;
    public AIFeedbackManager aiFeedbackManager;

    private bool isGeneratingFeedback = false;
    private bool challengeCompleted = false;

    void Start()
    {
        if (continueButton != null)
            continueButton.SetActive(false);
    }

    public void CheckAnswer()
    {
        Debug.Log("CheckAnswer() CALLED!");

        if (answerInput == null || feedbackText == null)
        {
            Debug.LogError("Missing UI references!");
            return;
        }

        if (isGeneratingFeedback || challengeCompleted)
            return;

        string playerAnswer = answerInput.text.Trim();

        Debug.Log("Player submitted: " + playerAnswer);

        if (string.IsNullOrWhiteSpace(playerAnswer))
        {
            feedbackText.text =
                "Please enter a base case condition.";
            return;
        }

        bool isCorrect =
            CodeValidator.ValidateBaseCase(playerAnswer);

        if (isCorrect)
        {
            challengeCompleted = true;

            if (submitButton != null)
                submitButton.SetActive(false);

            if (continueButton != null)
                continueButton.SetActive(true);

            feedbackText.color = new Color(
                0.1f, 0.5f, 0.2f
            );

            feedbackText.text =
                "Correct! The spell stops when n reaches 0.";

            Debug.Log("Base Case Challenge Completed!");

            // Portal activation will be added later.
        }
        else
        {
            if (aiFeedbackManager == null)
            {
                Debug.LogError(
                    "AI Feedback Manager is not assigned!"
                );
                feedbackText.text =
                    "Magic Tutor is unavailable.";
                return;
            }

            isGeneratingFeedback = true;

            feedbackText.color = new Color(
                0.4f, 0.2f, 0.1f
            );

            feedbackText.text =
                "The Magic Tutor is analyzing your spell...";

            StartCoroutine(
                aiFeedbackManager.GenerateFeedback(
                    playerAnswer,
                    feedback =>
                    {
                        feedbackText.text = feedback;
                        isGeneratingFeedback = false;
                    }
                )
            );
        }
    }
}
