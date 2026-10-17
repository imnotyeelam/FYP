
using UnityEngine;
using TMPro;

public class BaseCaseChallenge : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField answerInput;
    public TMP_Text feedbackText;

    [Header("Portal Reference")]
    public PortalInteraction portalInteraction;

    public void CheckAnswer()
    {
        Debug.Log("NEW CheckAnswer() CALLED!");

        if (answerInput == null || feedbackText == null)
        {
            Debug.LogError("Missing UI references!");
            return;
        }
         
        string playerAnswer = answerInput.text;

        Debug.Log("Player submitted: " + playerAnswer);

        bool isCorrect =
            CodeValidator.ValidateBaseCase(playerAnswer);

        if (isCorrect)
        {
            feedbackText.color = new Color(
                0.1f, 0.5f, 0.2f
            );

            feedbackText.text =
                "Correct! The spell stops when n reaches 0.";

            Debug.Log("Base Case Challenge Completed!");

            // We will activate the portal after
            // showing the explanation to the player.
        }
        else
        {
            feedbackText.color = new Color(
                0.7f, 0.2f, 0.2f
            );

            feedbackText.text =
                "Analyzing your spell...";

            Debug.Log("Incorrect answer: " + playerAnswer);

            // AI Feedback will be added here.
        }
    }
}
