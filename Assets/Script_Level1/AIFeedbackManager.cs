
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class AIFeedbackManager : MonoBehaviour
{
    [Header("Backend Settings")]
    public string backendURL =
        "http://127.0.0.1:8000/feedback";

    [Serializable]
    private class FeedbackRequest
    {
        public string playerAnswer;
    }

    [Serializable]
    private class FeedbackResponse
    {
        public string feedback;
    }

    public IEnumerator GenerateFeedback(
        string playerAnswer,
        Action<string> onComplete)
    {
        FeedbackRequest data = new FeedbackRequest
        {
            playerAnswer = playerAnswer
        };

        string json = JsonUtility.ToJson(data);

        using (UnityWebRequest request =
            new UnityWebRequest(backendURL, "POST"))
        {
            byte[] body =
                System.Text.Encoding.UTF8.GetBytes(json);

            request.uploadHandler =
                new UploadHandlerRaw(body);

            request.downloadHandler =
                new DownloadHandlerBuffer();

            request.SetRequestHeader(
                "Content-Type", "application/json");

            request.timeout = 15;

            yield return request.SendWebRequest();

            if (request.result !=
                UnityWebRequest.Result.Success)
            {
                Debug.LogWarning(
                    "AI Feedback Error: " + request.error);

                onComplete?.Invoke(
                    "The Magic Tutor is unavailable. " +
                    "Trace how n changes with each call."
                );

                yield break;
            }

            FeedbackResponse result =
                JsonUtility.FromJson<FeedbackResponse>(
                    request.downloadHandler.text
                );

            if (result != null &&
                !string.IsNullOrWhiteSpace(result.feedback))
            {
                onComplete?.Invoke(result.feedback);
            }
            else
            {
                onComplete?.Invoke(
                    "No feedback received. Please try again."
                );
            }
        }
    }
}
