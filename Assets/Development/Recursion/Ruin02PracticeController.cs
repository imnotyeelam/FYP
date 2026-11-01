using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Fyp.Development.Recursion
{
    [DefaultExecutionOrder(-100)]
    public sealed class Ruin02PracticeController : MonoBehaviour, ISharedInteractionTarget
    {
        public Transform player, interactionPoint;
        public Ruin02PracticePanel panel;
        public Ruin02FeedbackClient feedbackClient;
        public MonoBehaviour playerMovement, cameraFollow;
        public float interactionDistance=3f;
        [Min(.1f)] public float checkpointSeconds=.3f;
        Ruin02PracticeSession session;
        readonly List<Behaviour> suspended=new List<Behaviour>();
        readonly Dictionary<string,int> attempts=new Dictionary<string,int>();
        int feedbackVersion;
        Coroutine feedbackRoutine;
        string pendingFallback;
        float nextAt;
        bool open, movementBefore, cameraBefore, oldAllow, oldVisible;
        bool cinematicActive, closeAfterCinematic;
        CursorLockMode oldLock;
        GameObject oldSelection;
        string message="Enter the next operation, then submit.";
        bool incorrect;
        public event Action<int> AbsorbMagic;
        public event Action ChallengeCompleted;
        public Ruin02PracticeSession Session=>session;
        public bool IsOpen=>open;
        public bool IsCinematicActive=>cinematicActive;
        public bool IsSolved=>session!=null && session.Complete;
        public string Feedback=>message;
        public Ruin02FeedbackSource LastFeedbackSource { get; private set; } = Ruin02FeedbackSource.LocalFallback;
        public bool Incorrect=>incorrect;
        public bool CanSubmit=>open && !cinematicActive && session!=null && !session.Advancing && !session.Complete;
        public bool UsesSharedInteraction=>true;
        public Transform InteractionOrigin=>interactionPoint;
        public float SharedInteractionRange=>interactionDistance;
        public bool CanSharedInteract=>isActiveAndEnabled && player!=null && interactionPoint!=null &&
            panel!=null && session!=null && !open && CursorManager.allowCursorLock;
        public string SharedHint=>"Press [E] to Seal the Magic";
        public void InteractFromShared(){if(CanSharedInteract)Open();}

        void Start()
        {
            if(player==null || interactionPoint==null || panel==null || playerMovement==null || cameraFollow==null)
            {Debug.LogError("Ruin 2 challenge requires player, interaction, panel and control references.",this);enabled=false;return;}
            session=new Ruin02PracticeSession();
            session.AbsorbMagic+=n=>AbsorbMagic?.Invoke(n);
            session.ChallengeCompleted+=()=>ChallengeCompleted?.Invoke();
            session.SnapshotPresented+=(index,step)=>{if(open)panel.Present();};
            panel.Bind(this);panel.gameObject.SetActive(false);
        }

        public void Open()
        {
            if(!CanSharedInteract)return;
            open=true;oldLock=Cursor.lockState;oldVisible=Cursor.visible;oldAllow=CursorManager.allowCursorLock;
            oldSelection=EventSystem.current!=null?EventSystem.current.currentSelectedGameObject:null;
            movementBefore=playerMovement.enabled;cameraBefore=cameraFollow.enabled;
            playerMovement.enabled=false;cameraFollow.enabled=false;
            // Legacy world interactions must not poll E while this modal is open.
            foreach(var behaviour in gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MonoBehaviour>(true)))
                if(behaviour.enabled && (behaviour is RuinEntrance || behaviour is PortalInteraction || behaviour is KnowledgeInteractable))
                {suspended.Add(behaviour);behaviour.enabled=false;}
            InteractionHintManager.Instance?.HideAll();
            if(CursorManager.Instance!=null)CursorManager.Instance.SetUIOpen(true);
            else {CursorManager.allowCursorLock=false;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
            panel.gameObject.SetActive(true);panel.Present();
            EventSystem.current?.SetSelectedGameObject(panel.input.gameObject);
        }

        public void Close()
        {
            if(!open)return;
            if(cinematicActive){closeAfterCinematic=true;return;}
            CancelFeedback();open=false;panel.gameObject.SetActive(false);
            if(CursorManager.Instance!=null)CursorManager.Instance.SetUIOpen(false);
            CursorManager.allowCursorLock=oldAllow;Cursor.lockState=oldLock;Cursor.visible=oldVisible;
            playerMovement.enabled=movementBefore;cameraFollow.enabled=cameraBefore;
            foreach(var behaviour in suspended)if(behaviour!=null)behaviour.enabled=true;
            suspended.Clear();EventSystem.current?.SetSelectedGameObject(oldSelection);
        }

        // The cinematic borrows the existing modal; it never releases world interactions or cursor ownership.
        public void BeginCinematic()
        {
            if(!open || cinematicActive)throw new InvalidOperationException("Ruin 2 cinematic requires an open challenge.");
            cinematicActive=true;closeAfterCinematic=false;
            panel.gameObject.SetActive(false);
            EventSystem.current?.SetSelectedGameObject(null);
        }

        public void EndCinematic()
        {
            if(!cinematicActive)return;
            cinematicActive=false;
            if(closeAfterCinematic){closeAfterCinematic=false;Close();return;}
            if(!open)return;
            panel.gameObject.SetActive(true);panel.Present();
            EventSystem.current?.SetSelectedGameObject(panel.input.gameObject);
        }

        public Ruin02AnswerResult Submit(string answer)
        {
            if(!CanSubmit)return new Ruin02AnswerResult(Ruin02AnswerStatus.Busy,"Wait for the current sequence.");
            var q=session.Question;
            var result=session.Submit(q.Id,answer);
            CancelFeedback();
            if(result.Status==Ruin02AnswerStatus.Incorrect)
            {
                incorrect=true;
                int count=attempts.TryGetValue(q.Id,out var previous)?previous+1:1;
                attempts[q.Id]=count;
                pendingFallback=count==1?result.Message:result.Message+" Review the highlighted statement and the top stack frame.";
                if(Ruin02PracticeLesson.TryGetLocalInputHint(answer,q.Expected,out var syntaxHint))
                {
                    message=syntaxHint;LastFeedbackSource=Ruin02FeedbackSource.LocalFallback;
                }
                else if(feedbackClient!=null && feedbackClient.enabled)
                {
                    message="Generating hint...";LastFeedbackSource=Ruin02FeedbackSource.Pending;
                    feedbackRoutine=StartCoroutine(RequestFeedback(q,answer,count,feedbackVersion));
                }
                else {message=pendingFallback;LastFeedbackSource=Ruin02FeedbackSource.LocalFallback;}
            }
            else if(result.Status==Ruin02AnswerStatus.Correct)
            {
                incorrect=false;message=result.Message;LastFeedbackSource=Ruin02FeedbackSource.LocalFallback;
                nextAt=Time.unscaledTime+checkpointSeconds;
                panel.input.SetTextWithoutNotify("");
            }
            else message=result.Message;
            panel.Present();return result;
        }

        void CancelFeedback()
        {
            feedbackVersion++;
            if(feedbackRoutine!=null){StopCoroutine(feedbackRoutine);feedbackRoutine=null;}
            if(LastFeedbackSource==Ruin02FeedbackSource.Pending)
            {message=pendingFallback??"Review the highlighted statement and the top stack frame.";
                LastFeedbackSource=Ruin02FeedbackSource.LocalFallback;}
        }

        IEnumerator RequestFeedback(Ruin02PracticeQuestion q,string answer,int attempt,int version)
        {
            var step=session.Current;
            var request=new Ruin02FeedbackRequest {
                playerAnswer=answer, currentStep=session.QuestionNumber, currentN=step.Parameter??0,
                phase=q.Id.Contains(".call.")?"Call":q.Id.Contains(".base.")?"Base Case":"Return",
                expectedOperation=q.Expected,attemptCount=attempt,
                functionCode=string.Join("\n",session.Lesson.Trace.Code)
            };
            Ruin02FeedbackResult response=default;
            yield return feedbackClient.Generate(request,result=>response=result);
            if(version!=feedbackVersion || !open || session.Question!=q || !incorrect)yield break;
            feedbackRoutine=null;
            bool usable=response.Source==Ruin02FeedbackSource.LiveAI && !string.IsNullOrWhiteSpace(response.Text);
            if(usable)
            {
                string compact=System.Text.RegularExpressions.Regex.Replace(response.Text,@"\s+","");
                usable=response.Text.IndexOf("placeStone",StringComparison.OrdinalIgnoreCase)<0 &&
                    response.Text.IndexOf("buildBridge",StringComparison.OrdinalIgnoreCase)<0 &&
                    response.Text.IndexOf("bridge",StringComparison.OrdinalIgnoreCase)<0 &&
                    (attempt>1 || compact.IndexOf(q.Expected,StringComparison.OrdinalIgnoreCase)<0);
                if(usable && attempt==1 && q.Id.Contains(".absorb.") &&
                    response.Text.IndexOf("absorbMagic",StringComparison.OrdinalIgnoreCase)>=0 &&
                    System.Text.RegularExpressions.Regex.IsMatch(response.Text,
                        @"\bn\s*(?:=|is|equals)\s*"+(step.Parameter??0)+@"\b",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase))usable=false;
            }
            message=usable?response.Text:pendingFallback;
            LastFeedbackSource=usable?Ruin02FeedbackSource.LiveAI:
                response.Source==Ruin02FeedbackSource.LiveAI?Ruin02FeedbackSource.LocalFallback:response.Source;
            panel.Present();
        }

        void Update()
        {
            if(session==null)return;
            if(open && Keyboard.current!=null && Keyboard.current.escapeKey.wasPressedThisFrame)Close();
            if(cinematicActive)return;
            if(!session.Advancing || Time.unscaledTime<nextAt)return;
            session.TryAdvanceApprovedStep();nextAt=Time.unscaledTime+checkpointSeconds;
            if(session.Advancing)
                message="Tracing each call and return in order…";
            else if(session.Complete)
                message="Ritual solved. Four return actions completed; the stack is empty.";
            else message="Execution paused. Enter the next operation.";
            feedbackVersion++;incorrect=false;
            if(open)panel.Present();
        }
        void OnDisable(){if(open){cinematicActive=false;closeAfterCinematic=false;Close();}}
    }
}
