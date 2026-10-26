using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Fyp.Development.Recursion
{
    [DefaultExecutionOrder(-100)]
    public sealed class BridgePracticeController : MonoBehaviour,ISharedInteractionTarget
    {
        public FloatingBridgeMotionRig rig;
        public Transform player,interactionPoint;
        public BridgePracticePanel panel;
        public BridgePracticeCamera presentation;
        public GameObject[] otherModalPanels;
        public float interactionDistance=3f;
        [Min(.15f)] public float stepSeconds=.65f;
        BridgePracticeSession session;
        FloatingIslandMotion[] modules;
        readonly bool[] restored=new bool[3];
        readonly List<int> visited=new List<int>(),actions=new List<int>();
        readonly List<Behaviour> suspended=new List<Behaviour>();
        readonly Dictionary<Renderer,MaterialPropertyBlock> originalBlocks=new Dictionary<Renderer,MaterialPropertyBlock>();
        readonly Dictionary<Renderer,MaterialPropertyBlock> workingBlocks=new Dictionary<Renderer,MaterialPropertyBlock>();
        static readonly int Emission=Shader.PropertyToID("_EmissionColor");
        int highlight=-1,index;
        bool initialized,open,seeded,pendingClose,escapeRequested,wasWaiting,complete;
        float nextAt;
        string selectedQuestion,selectedAnswer,feedback="Choose the next recursive action. Incorrect answers can be retried.";
        bool incorrect;
        CursorLockMode oldLock;
        bool oldVisible,oldAllow;
        GameObject oldSelection;
        public event Action<int,RecursionStep> SnapshotPresented;
        public event Action Completed;
        public RecursionTrace Trace=>session.Lesson.Trace;
        public RecursionStep Current=>Trace.Steps[index];
        public int CheckpointIndex=>index;
        public PracticeDecision CurrentDecision=>session?.CurrentDecision;
        public bool IsOpen=>open;
        public bool ClosePending=>pendingClose;
        public bool IsComplete=>complete;
        public int CompletionCount {get;private set;}
        public int OpenCount {get;private set;}
        public int RestorationCount=>actions.Count;
        public IReadOnlyList<int> VisitedCheckpoints=>visited;
        public IReadOnlyList<int> RestorationParameters=>actions;
        public string Feedback=>feedback;
        public string SelectedAnswerId=>selectedAnswer;
        public bool LastAnswerIncorrect=>incorrect;
        public int DecisionNumber=>complete?6:session==null?1:Mathf.Clamp(session.Lesson.Decisions.IndexOf(session.CurrentDecision)+1,1,6);
        bool Waiting=>modules!=null && Enumerable.Range(0,3).Any(i=>restored[i] && (!modules[i].IsRepaired || modules[i].walkableColliders.Any(c=>c==null || !c.enabled)));
        public bool IsBusy=>!seeded || Waiting || (session!=null && session.IsAdvancing) || pendingClose;
        public bool CanSubmit=>initialized && open && !complete && !IsBusy;
        bool OtherModalOpen=>!CursorManager.allowCursorLock || (otherModalPanels!=null && otherModalPanels.Any(p=>p!=null && p.activeInHierarchy));
        public bool UsesSharedInteraction=>true;
        public Transform InteractionOrigin=>interactionPoint;
        public float SharedInteractionRange=>interactionDistance;
        public bool CanSharedInteract=>initialized && isActiveAndEnabled && !open && player!=null && interactionPoint!=null && !OtherModalOpen;
        public string SharedHint=>complete?"Press [E] to Review the Restored Bridge":"Press [E] to Restore the Bridge";
        public void InteractFromShared(){if(CanSharedInteract)Open();}
        void Start()
        {
            if(rig==null || player==null || interactionPoint==null || panel==null || presentation==null)
            {Debug.LogError("Bridge practice requires explicit rig, player, interaction, panel and camera references.",this);enabled=false;return;}
            modules=new[]{rig.placeStone3,rig.placeStone2,rig.placeStone1};
            if(modules.Any(m=>m==null) || modules.Distinct().Count()!=3 || modules.Any(m=>!m.TryClaimForPractice(this)))
            {Debug.LogError("Bridge practice could not claim its three distinct modules.",this);enabled=false;return;}
            session=new BridgePracticeSession();panel.Bind(this);panel.gameObject.SetActive(false);initialized=true;
            PresentSnapshot(0);Refresh();
        }
        public void Open()
        {
            if(!CanSharedInteract)return;
            oldLock=Cursor.lockState;oldVisible=Cursor.visible;oldAllow=CursorManager.allowCursorLock;
            oldSelection=EventSystem.current!=null?EventSystem.current.currentSelectedGameObject:null;
            open=true;OpenCount++;pendingClose=false;escapeRequested=false;
            InteractionHintManager.Instance?.HideAll();
            foreach(var behaviour in gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MonoBehaviour>(true)))
            {
                if(!behaviour.enabled)continue;
                if(behaviour is PlayerMovement || behaviour is CameraFollow || behaviour is KnowledgeInteractable || behaviour is RuinEntrance || behaviour is PortalInteraction)
                {suspended.Add(behaviour);behaviour.enabled=false;}
            }
            if(CursorManager.Instance!=null)CursorManager.Instance.SetUIOpen(true);
            else{CursorManager.allowCursorLock=false;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
            panel.gameObject.SetActive(true);presentation.Begin(FocusParameter(Current));nextAt=Time.unscaledTime+stepSeconds;
            feedback=complete?"Bridge Restored. All 22 checkpoints completed; the call stack is empty.":session.IsAdvancing?"Resuming the previously approved execution range.":"Choose an answer, then Cast Spell. Your committed progress is preserved.";
            Refresh();EventSystem.current?.SetSelectedGameObject(panel.details.closeButton.gameObject);
        }
        public void Close()
        {
            if(!open)return;
            if(Waiting)
            {pendingClose=true;feedback="Finishing the current island motion before releasing player controls. Progress will be preserved.";Refresh();return;}
            FinishClose();
        }
        void FinishClose()
        {
            if(!open)return;open=false;pendingClose=false;escapeRequested=false;panel.gameObject.SetActive(false);ClearHighlight();presentation.End();
            if(CursorManager.Instance!=null)CursorManager.Instance.SetUIOpen(false);
            CursorManager.allowCursorLock=oldAllow;Cursor.lockState=oldLock;Cursor.visible=oldVisible;
            foreach(var behaviour in suspended)if(behaviour!=null)behaviour.enabled=true;suspended.Clear();
            EventSystem.current?.SetSelectedGameObject(oldSelection);
        }
        public void SelectAnswer(string questionId,string answerId)
        {
            if(!CanSubmit || CurrentDecision==null || CurrentDecision.Id!=questionId || !CurrentDecision.Answers.Any(a=>a.Id==answerId))return;
            selectedQuestion=questionId;selectedAnswer=answerId;Refresh();
        }
        public void SubmitSelected(){SubmitAnswer(selectedQuestion,selectedAnswer);}
        public PracticeSubmission SubmitAnswer(string questionId,string answerId)
        {
            if(complete)return new PracticeSubmission(PracticeSubmissionStatus.Complete,"Bridge is already restored.");
            if(!open)return new PracticeSubmission(PracticeSubmissionStatus.StaleQuestion,"Open practice to answer.");
            if(IsBusy)return new PracticeSubmission(PracticeSubmissionStatus.Advancing,"Wait for the approved execution and motion to settle.");
            var result=session.Submit(questionId,answerId);
            if(result.Status==PracticeSubmissionStatus.Correct || result.Status==PracticeSubmissionStatus.Incorrect || result.Status==PracticeSubmissionStatus.UnknownAnswer)
            {
                feedback=result.Feedback;incorrect=result.Status==PracticeSubmissionStatus.Incorrect;
                if(result.Accepted){selectedQuestion=null;selectedAnswer=null;nextAt=Time.unscaledTime+.25f;}
                Refresh();
            }
            return result;
        }
        void Update()
        {
            if(!initialized)return;
            if(open && Keyboard.current!=null && Keyboard.current.escapeKey.wasPressedThisFrame)escapeRequested=true;
            bool waiting=Waiting;
            if(waiting!=wasWaiting){wasWaiting=waiting;Refresh();}
            if(pendingClose)
            {if(!waiting)FinishClose();return;}
            if(!open)return;
            if(highlight>=0)PulseHighlight();
            if(waiting || Time.unscaledTime<nextAt)return;
            if(!seeded){seeded=true;PresentSnapshot(session.Index);nextAt=Time.unscaledTime+stepSeconds;Refresh();return;}
            if(session.TryAdvanceApprovedStep())
            {
                PresentSnapshot(session.Index);nextAt=Time.unscaledTime+stepSeconds;
                if(session.IsComplete && !Waiting && !complete)
                {
                    if(actions.Count!=3 || Current.Frames.Count!=0 || index!=21)throw new InvalidOperationException("Practice completion invariant failed.");
                    complete=true;CompletionCount++;feedback="Bridge Restored — exactly three placements, all 22 checkpoints completed, and an empty call stack.";
                    ClearHighlight();presentation.Focus(1);Completed?.Invoke();
                }
                Refresh();
            }
        }
        void LateUpdate(){if(escapeRequested){escapeRequested=false;Close();}}
        int FocusParameter(RecursionStep step)=>step.Parameter.HasValue?Mathf.Clamp(step.Parameter.Value,1,3):1;
        void PresentSnapshot(int checkpoint)
        {
            index=checkpoint;var step=Current;
            if(visited.Count==0 || visited[visited.Count-1]!=checkpoint)visited.Add(checkpoint);
            if(step.Phase==RecursionPhase.Action)
            {
                if(!step.Parameter.HasValue || !rig.TryGetSection(step.Parameter.Value,out var module))throw new InvalidOperationException("Unmapped actual placement action.");
                int slot=3-step.Parameter.Value;
                if(!restored[slot])
                {
                    if(step.ActionCount!=actions.Count+1 || !module.RepairForPractice(this))throw new InvalidOperationException("Placement order or motion ownership failed.");
                    restored[slot]=true;actions.Add(step.Parameter.Value);
                }
            }
            if(open)presentation.Focus(FocusParameter(step));
            var projection=new BridgeSnapshotProjection(step,3);SetHighlight(open?projection.ReturningStone:-1);
            SnapshotPresented?.Invoke(checkpoint,step);
        }
        void SetHighlight(int slot)
        {
            if(highlight==slot)return;ClearHighlight();highlight=slot;
            if(slot<0)return;
            foreach(var renderer in modules[slot].GetComponentsInChildren<Renderer>())
            {
                if(!renderer.sharedMaterials.Any(m=>m!=null && m.HasProperty(Emission)))continue;
                if(!originalBlocks.ContainsKey(renderer))
                {
                    var original=new MaterialPropertyBlock();renderer.GetPropertyBlock(original);originalBlocks.Add(renderer,original);
                    var working=new MaterialPropertyBlock();renderer.GetPropertyBlock(working);workingBlocks.Add(renderer,working);
                }
            }
            PulseHighlight();
        }
        void PulseHighlight()
        {
            foreach(var renderer in modules[highlight].GetComponentsInChildren<Renderer>())
            {
                if(!workingBlocks.TryGetValue(renderer,out var block))continue;
                block.SetColor(Emission,new Color(1,.55f,.12f)*(1.2f+.3f*Mathf.Sin(Time.unscaledTime*6)));renderer.SetPropertyBlock(block);
            }
        }
        void ClearHighlight()
        {
            foreach(var pair in originalBlocks)if(pair.Key!=null)pair.Key.SetPropertyBlock(pair.Value);highlight=-1;
        }
        void Refresh(){if(initialized && panel!=null)panel.Present();}
        void OnDisable(){if(open)FinishClose();ClearHighlight();}
    }
}
