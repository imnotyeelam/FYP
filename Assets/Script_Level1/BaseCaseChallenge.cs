
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
        if (answerInput == null || feedbackText == null)
        {
            Debug.LogError("Challenge UI references missing!");
            return;
        }

        string answer = answerInput.text
            .Replace(" ", "")
            .Replace("\t", "")
            .Replace("\n", "")
            .Replace("\r", "")
            .TrimEnd(':');

        if (answer == "n==0" || answer == "0==n")
        {
            feedbackText.text =
                "Correct! The base case stops recursion when n reaches 0.";

            feedbackText.color = Color.green;

            Debug.Log("Base case challenge completed!");

            if (portalInteraction != null)
                portalInteraction.CompletePortal();
        }
        else
        {
            feedbackText.text =
                "Not quite! Think about when the recursive calls should stop.";

            feedbackText.color = Color.red;
        }
    }

    public void ResetChallenge()
    {
        if (answerInput != null)
            answerInput.text = "";

        if (feedbackText != null)
            feedbackText.text = "";
    }
}
