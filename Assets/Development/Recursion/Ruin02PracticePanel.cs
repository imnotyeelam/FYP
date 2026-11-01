using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fyp.Development.Recursion
{
    public sealed class Ruin02PracticePanel : MonoBehaviour
    {
        public RecursionPanelView details;
        public TMP_Text question,feedback;
        public TMP_InputField input;
        public Button submit;
        Ruin02PracticeController controller;

        public void Bind(Ruin02PracticeController owner)
        {
            controller=owner;details.Disconnect();
            details.closeButton.onClick.AddListener(Close);
            submit.onClick.AddListener(Submit);
            input.onSubmit.AddListener(SubmitText);
        }
        void Close()=>controller.Close();
        void Submit()=>controller.Submit(input.text);
        void SubmitText(string value){if(controller.CanSubmit)controller.Submit(value);}

        public void Present()
        {
            if(controller==null)return;
            var session=controller.Session;
            var step=session.Current;
            var trace=session.Lesson.Trace;
            details.title.text=session.Complete?"Ruin II | Ritual Complete":"Ruin II | Seal the Magic";
            var code=new StringBuilder();
            for(int i=0;i<trace.Code.Count;i++)
            {
                string line=(i+1)+" "+trace.Code[i];
                code.AppendLine(i==step.ActiveLine?"<color=#FFD166><b>>"+line+"</b></color>":" "+line);
            }
            details.code.text=code.ToString();
            details.state.text=step.Phase+" | n = "+(step.Parameter.HasValue?step.Parameter.ToString():"--")+
                " | absorbMagic: "+step.ActionCount+" / 4";
            details.explanation.text=step.Explanation;
            details.emptyStack.gameObject.SetActive(step.Frames.Count==0);
            for(int row=0;row<details.frameLabels.Length;row++)
            {
                bool visible=row<step.Frames.Count;
                details.frameBackgrounds[row].gameObject.SetActive(visible);
                if(!visible)continue;
                int n=step.Frames[step.Frames.Count-1-row];
                details.frameLabels[row].text="sealMagic("+n+")"+(row==0?" [active]":" [waiting]");
                details.frameBackgrounds[row].color=row==0?new Color(.15f,.34f,.4f):new Color(.12f,.16f,.21f);
            }
            question.text=session.Complete?"The call stack is empty. The ritual is solved.":
                session.Advancing?"Watch the current execution range.":session.Question.Prompt;
            feedback.text=controller.Feedback;
            feedback.color=controller.Incorrect?new Color(1,.65f,.55f):new Color(.66f,.94f,.79f);
            details.progress.text="Decision "+session.QuestionNumber+" / 6 | Checkpoint "+(session.Index+1)+" / "+trace.Steps.Count;
            input.interactable=controller.CanSubmit;
            submit.interactable=controller.CanSubmit && !string.IsNullOrWhiteSpace(input.text);
            input.gameObject.SetActive(!session.Complete);
            submit.gameObject.SetActive(!session.Complete);
        }
        void Update(){if(controller!=null && submit.gameObject.activeSelf)submit.interactable=controller.CanSubmit && !string.IsNullOrWhiteSpace(input.text);}
        void OnDestroy()
        {
            if(details!=null && details.closeButton!=null)details.closeButton.onClick.RemoveListener(Close);
            if(submit!=null)submit.onClick.RemoveListener(Submit);
            if(input!=null)input.onSubmit.RemoveListener(SubmitText);
        }
    }
}
