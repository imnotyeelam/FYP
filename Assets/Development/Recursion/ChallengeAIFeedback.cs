using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace Fyp.Development.Recursion
{
    /// <summary>Structured context for one already-rejected answer (POST /feedback, JsonUtility).</summary>
    [Serializable]
    public sealed class ChallengeFeedbackRequest
    {
        public string playerAnswer;
        public string challengeId;
        public string ruinId;
        public string objective;
        public string question;
        public string functionCode;
        public string phase;
        public int currentStep;
        public string parameters;
        public string callStack;
        public string errorType;
        public int attemptCount;
        public string localHint;
        [Tooltip("Never shown to the model; the backend and this client use it only to withhold replies that leak it.")]
        public string protectedAnswer;
    }

    public enum ChallengeFeedbackSource { None, Pending, LiveAI, Mock, Reused, Offline, Timeout, HttpError, InvalidResponse, Withheld, LocalOnly }

    /// <summary>
    /// Routes an already-rejected challenge answer through the established AI feedback pipeline
    /// (AIFeedbackManager.backendURL -> local backend POST /feedback) with challenge context.
    /// The local hint is shown at once and kept as the offline/timeout/leak fallback. AI text never
    /// decides correctness, progression or rewards. Stale replies (newer request, Cancel on close or
    /// advance) are ignored; an identical repeat request reuses the pending/previous reply.
    /// </summary>
    public sealed class ChallengeAIFeedback : MonoBehaviour
    {
        public AIFeedbackManager manager;
        [Tooltip("Backend profile id: Ruin01Bridge, Ruin01Jump, Ruin02Staff, Ruin02Farm, FinalPortal.")]
        public string challengeId = "";
        public string ruinId = "";
        [Tooltip("Short challenge name (presentation/logging only).")]
        public string challengeName = "Recursion challenge";
        [TextArea] public string objective;
        [Tooltip("Function shown to the player, sent as context (C++ teaching text only).")]
        [TextArea] public string functionCode;
        [Min(1)] public int timeoutSeconds = 12;

        const string TutorLabel = "<b>Magic Tutor:</b> ";
        const string ThinkingNote = "\n<size=85%><i>The Magic Tutor is thinking...</i></size>";
        const string OfflineNote = "\n<size=80%><i>(Magic Tutor offline - local hint)</i></size>";

        [Serializable] sealed class Response { public string feedback; public string source; }

        int version;
        Coroutine routine;
        string pendingKey, lastKey, lastText;
        Action<string, bool> pendingApply;
        string pendingLocal;
        readonly Dictionary<string, int> attempts = new Dictionary<string, int>();

        public bool Pending { get; private set; }
        public bool LastWasLiveAI { get; private set; }
        public ChallengeFeedbackSource LastSource { get; private set; }
        public int RequestCount { get; private set; }
        public int ReusedCount { get; private set; }
        public string LastPayload { get; private set; }

        /// <summary>Legacy overload kept for existing callers: free-text situation only.</summary>
        public void Request(string situation, string playerAnswer, string localHint, Action<string, bool> apply)
        {
            Request(new ChallengeFeedbackRequest { question = situation, playerAnswer = playerAnswer }, localHint, apply);
        }

        /// <summary>Shows localHint immediately, then replaces it with the AI hint if one arrives.</summary>
        public void Request(ChallengeFeedbackRequest data, string localHint, Action<string, bool> apply)
        {
            if (apply == null || data == null) return;
            Fill(data, localHint);
            string key = DedupeKey(data);
            if (Pending && key == pendingKey)
            {
                // Same rejected answer while a reply is in flight: keep that request, refresh its target.
                pendingApply = apply; pendingLocal = localHint;
                apply(localHint + ThinkingNote, false);
                return;
            }
            Cancel();
            if (key == lastKey && lastText != null)
            {
                ReusedCount++; LastSource = ChallengeFeedbackSource.Reused; LastWasLiveAI = true;
                apply(lastText, true);
                return;
            }
            if (manager == null || string.IsNullOrWhiteSpace(manager.backendURL) || !isActiveAndEnabled)
            { LastSource = ChallengeFeedbackSource.LocalOnly; LastWasLiveAI = false; apply(localHint, false); return; }

            data.attemptCount = data.attemptCount > 0 ? data.attemptCount : NextAttempt(data);
            Pending = true; pendingKey = key; pendingApply = apply; pendingLocal = localHint;
            LastSource = ChallengeFeedbackSource.Pending;
            apply(localHint + ThinkingNote, false);
            LastPayload = JsonUtility.ToJson(data);
            RequestCount++;
            routine = StartCoroutine(Run(version, key, data.protectedAnswer, LastPayload));
        }

        void Fill(ChallengeFeedbackRequest d, string localHint)
        {
            if (string.IsNullOrEmpty(d.challengeId)) d.challengeId = challengeId;
            if (string.IsNullOrEmpty(d.ruinId)) d.ruinId = ruinId;
            if (string.IsNullOrEmpty(d.objective)) d.objective = objective;
            if (string.IsNullOrEmpty(d.functionCode)) d.functionCode = functionCode;
            if (string.IsNullOrEmpty(d.localHint)) d.localHint = StripTags(localHint);
            // Respect the backend's field limits so a long field never turns into an HTTP 422.
            d.playerAnswer = Clip(d.playerAnswer ?? "", 500);
            d.challengeId = Clip(d.challengeId, 40); d.ruinId = Clip(d.ruinId, 20);
            d.objective = Clip(d.objective, 300); d.question = Clip(d.question, 300);
            d.functionCode = Clip(d.functionCode, 800); d.phase = Clip(d.phase, 40);
            d.parameters = Clip(d.parameters, 200); d.callStack = Clip(d.callStack, 200);
            d.errorType = Clip(d.errorType, 60); d.localHint = Clip(d.localHint, 300);
            d.protectedAnswer = Clip(d.protectedAnswer, 120);
            // Some local hints name the answer; never hand those to the model (it would echo them).
            if (Leaks(d.localHint, d.protectedAnswer)) d.localHint = null;
            d.currentStep = Mathf.Clamp(d.currentStep, 0, 100);
        }

        int NextAttempt(ChallengeFeedbackRequest d)
        {
            string q = d.challengeId + "|" + d.question;
            attempts.TryGetValue(q, out int count);
            attempts[q] = ++count;
            return Mathf.Min(count, 1000);
        }

        static string DedupeKey(ChallengeFeedbackRequest d) =>
            string.Join("|", d.challengeId, d.question, d.phase, d.parameters, d.errorType,
                Regex.Replace(d.playerAnswer ?? "", @"\s+", "").ToLowerInvariant());

        IEnumerator Run(int requestVersion, string key, string protectedAnswer, string json)
        {
            ChallengeFeedbackSource source;
            string text = null;
            using (var request = new UnityWebRequest(manager.backendURL, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = timeoutSeconds;
                yield return request.SendWebRequest();
                if (requestVersion != version) yield break;
                if (request.result != UnityWebRequest.Result.Success)
                {
                    source = request.responseCode > 0 ? ChallengeFeedbackSource.HttpError :
                        request.error != null && request.error.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0 ?
                        ChallengeFeedbackSource.Timeout : ChallengeFeedbackSource.Offline;
                }
                else
                {
                    Response parsed = null;
                    try { parsed = JsonUtility.FromJson<Response>(request.downloadHandler.text); } catch (ArgumentException) { }
                    if (parsed == null) source = ChallengeFeedbackSource.InvalidResponse;
                    else if (parsed.source == "withheld") source = ChallengeFeedbackSource.Withheld;
                    else if (string.IsNullOrWhiteSpace(parsed.feedback) || (parsed.source != "openai" && parsed.source != "mock"))
                        source = ChallengeFeedbackSource.InvalidResponse;
                    else if (Leaks(parsed.feedback, protectedAnswer)) source = ChallengeFeedbackSource.Withheld;
                    else { source = parsed.source == "mock" ? ChallengeFeedbackSource.Mock : ChallengeFeedbackSource.LiveAI; text = parsed.feedback.Trim(); }
                }
            }
            var apply = pendingApply; string local = pendingLocal;
            routine = null; Pending = false; pendingKey = null; pendingApply = null;
            LastSource = source;
            LastWasLiveAI = text != null;
            if (text != null)
            {
                lastKey = key;
                lastText = TutorLabel + Fyp.Development.UI.FypUiTheme.ForFont(text);
                apply?.Invoke(lastText, true);
            }
            else
            {
                Debug.LogWarning($"{challengeName} AI feedback: {source}; local hint shown.", this);
                apply?.Invoke(local + (source == ChallengeFeedbackSource.Withheld ? "" : OfflineNote), false);
            }
        }

        /// <summary>Defence in depth (the backend also checks): reject a reply containing the protected answer.</summary>
        public static bool Leaks(string reply, string protectedAnswer)
        {
            if (string.IsNullOrWhiteSpace(reply) || string.IsNullOrWhiteSpace(protectedAnswer)) return false;
            string secret = Regex.Replace(protectedAnswer, @"\s+", "").ToLowerInvariant();
            if (Regex.IsMatch(secret, @"^\d+$"))
                return Regex.IsMatch(reply, $@"\bn\s*(=|==|is|equals|of|to|should be|must be)\s*{secret}\b|\({secret}\)|\b{secret}\s+(leaps?|jumps?|times|platforms?|destinations?|calls?)\b|\b(enter|type|use|choose|try)\s+{secret}\b", RegexOptions.IgnoreCase);
            return secret.Length >= 3 && Regex.Replace(reply, @"\s+", "").ToLowerInvariant().Contains(secret);
        }

        /// <summary>Pure classification of a typed call answer (staff/farm), sent as errorType context.</summary>
        public static string ClassifyCallAnswer(string answer, string expectedName)
        {
            string value = Regex.Replace(answer ?? "", @"\s+", "").TrimEnd(';');
            if (value.Length == 0) return "empty answer";
            int depth = 0;
            foreach (char c in value) { if (c == '(') depth++; else if (c == ')' && --depth < 0) return "invalid syntax (unbalanced parentheses)"; }
            if (depth != 0) return "invalid syntax (unbalanced parentheses)";
            var call = Regex.Match(value, @"^(?<name>[A-Za-z_]\w*)\((?<args>.*)\)$");
            if (call.Success)
                return string.Equals(call.Groups["name"].Value, expectedName, StringComparison.Ordinal) ?
                    "incorrect argument" : "wrong function name";
            if (Regex.IsMatch(value, @"^[A-Za-z_]\w*\d+$") || Regex.IsMatch(value, @"^[A-Za-z_]\w*$") && value != "return")
                return value.StartsWith(expectedName, StringComparison.OrdinalIgnoreCase) ? "invalid syntax (missing parentheses)" : "wrong statement or function name";
            return "incorrect operation";
        }

        public static string FormatStack(string function, IEnumerable<int> frames)
        {
            var parts = new List<string>();
            foreach (int f in frames) parts.Add(function + "(" + f + ")");
            return parts.Count == 0 ? "(empty)" : string.Join(" > ", parts) + " (top last)";
        }

        static string StripTags(string s) => s == null ? null : Regex.Replace(s, "<.*?>", "");
        static string Clip(string s, int max) => s != null && s.Length > max ? s.Substring(0, max) : s;

        /// <summary>Invalidates any in-flight request (call on close, advance or a new submission).</summary>
        public void Cancel()
        {
            version++;
            if (routine != null) { StopCoroutine(routine); routine = null; }
            if (Pending) LastSource = ChallengeFeedbackSource.None;
            Pending = false; pendingKey = null; pendingApply = null;
        }

        void OnDisable() { Cancel(); }
    }
}
