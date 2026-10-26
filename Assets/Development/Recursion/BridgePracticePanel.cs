using System;
using System.Text;
using TMPro;
using UnityEngine;

namespace Fyp.Development.Recursion
{
    public sealed class BridgePracticePanel : MonoBehaviour
    {
        public RecursionPanelView details;
        public TMP_Text question,feedback;
        public UnityEngine.UI.Button[] answers;
        public TMP_Text[] answerLabels;
        public UnityEngine.UI.Button submit;
        BridgePracticeController controller;
        string boundQuestion;
        public void Bind(BridgePracticeController owner)
        {
            controller=owner;details.Disconnect();
            details.closeButton.onClick.AddListener(Close);
            submit.onClick.AddListener(Submit);
        }
        void Close()=>controller.Close();
        void Submit()=>controller.SubmitSelected();
        public void Present()
        {
            var step=controller.Current;var trace=controller.Trace;
            details.title.text=controller.IsComplete?"Ruin I | Bridge Restored":"Ruin I | Cast a Recursive Spell";
            var code=new StringBuilder();
            for(int i=0;i<trace.Code.Count;i++)
            {
                string line=(i+1)+" "+trace.Code[i];
                code.AppendLine(i==step.ActiveLine?"<color=#FFD166><b>>"+line+"</b></color>":" "+line);
            }
            details.code.text=code.ToString();
            details.state.text=step.Phase+" | n = "+(step.Parameter.HasValue?step.Parameter.ToString():"--")+"\nplaceStone actions: "+step.ActionCount+" / 3";
            details.explanation.text=step.Explanation;
            details.emptyStack.gameObject.SetActive(step.Frames.Count==0);
            for(int row=0;row<details.frameLabels.Length;row++)
            {
                bool visible=row<step.Frames.Count;details.frameBackgrounds[row].gameObject.SetActive(visible);if(!visible)continue;
                int n=step.Frames[step.Frames.Count-1-row];var block=details.frameBackgrounds[row].GetComponent<CallStackBlock>();
                if(block!=null)block.Initialize(trace.FunctionName,n);else details.frameLabels[row].text=trace.FunctionName+"("+n+")";
                details.frameLabels[row].text+=row==0?" ["+step.Phase+"]":" [waiting]";
                details.frameBackgrounds[row].color=row==0?new Color(.15f,.34f,.4f):new Color(.12f,.16f,.21f);
            }
            var q=controller.CurrentDecision;
            question.text=controller.IsComplete?"Bridge Restored — all calls have returned.":controller.ClosePending?"Closing after the current island safely settles…":q!=null?q.Prompt:"Watch the call stack unwind.";
            feedback.text=controller.Feedback;
            feedback.color=controller.LastAnswerIncorrect?new Color(1,.65f,.55f):new Color(.66f,.94f,.79f);
            details.progress.text="Decision "+controller.DecisionNumber+" / 6 | Checkpoint "+(controller.CheckpointIndex+1)+" / 22"+(controller.IsBusy?" | Resolving…":"");
            string id=q==null?"":q.Id;
            if(boundQuestion!=id)
            {
                boundQuestion=id;
                for(int i=0;i<answers.Length;i++)
                {
                    answers[i].onClick.RemoveAllListeners();
                    if(q!=null && i<q.Answers.Count)
                    {
                        string questionId=q.Id,answerId=q.Answers[i].Id;
                        answers[i].onClick.AddListener(()=>controller.SelectAnswer(questionId,answerId));
                    }
                }
            }
            for(int i=0;i<answers.Length;i++)
            {
                bool visible=q!=null && !controller.IsComplete && i<q.Answers.Count;
                answers[i].gameObject.SetActive(visible);if(!visible)continue;
                answerLabels[i].text=q.Answers[i].Text;answers[i].interactable=controller.CanSubmit;
                ((UnityEngine.UI.Image)answers[i].targetGraphic).color=controller.SelectedAnswerId==q.Answers[i].Id?new Color(.2f,.5f,.57f):new Color(.14f,.23f,.31f);
            }
            submit.interactable=controller.CanSubmit && !string.IsNullOrEmpty(controller.SelectedAnswerId);
            submit.gameObject.SetActive(!controller.IsComplete);
        }
        void OnDestroy()
        {
            if(details!=null && details.closeButton!=null)details.closeButton.onClick.RemoveListener(Close);
            if(submit!=null)submit.onClick.RemoveListener(Submit);
        }
    }
}
