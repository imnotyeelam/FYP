using UnityEngine;

namespace Fyp.Development.Recursion
{
    // Scoped camera override; the existing gameplay camera and its controls are restored on close.
    public sealed class BridgePracticeCamera : MonoBehaviour
    {
        public Camera practiceCamera,gameplayCamera;
        public RectTransform panel;
        public FloatingBridgeMotionRig rig;
        public bool IsPresenting { get; private set; }
        public int FocusParameter { get; private set; }=3;
        bool oldGameEnabled,oldPracticeEnabled;
        Rect oldRect;
        Vector3 oldPosition;
        Quaternion oldRotation;
        float oldSize;
        readonly Vector3[] corners=new Vector3[4];
        public void Begin(int parameter)
        {
            if(IsPresenting)return;
            oldGameEnabled=gameplayCamera.enabled;oldPracticeEnabled=practiceCamera.enabled;
            oldRect=practiceCamera.rect;oldPosition=practiceCamera.transform.position;oldRotation=practiceCamera.transform.rotation;oldSize=practiceCamera.orthographicSize;
            IsPresenting=true;gameplayCamera.enabled=false;practiceCamera.enabled=true;Focus(parameter);Frame(true);
        }
        public void Focus(int parameter){FocusParameter=Mathf.Clamp(parameter,1,3);}
        public Bounds FocusBounds()
        {
            rig.TryGetSection(FocusParameter,out var section);
            var renderers=section.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;
            foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            bounds.Encapsulate(section.repairedPose.position);
            return bounds;
        }
        void LateUpdate(){if(IsPresenting)Frame(false);}
        void Frame(bool immediate)
        {
            panel.GetWorldCorners(corners);float edge=RectTransformUtility.WorldToScreenPoint(null,corners[0]).x;
            float width=Mathf.Clamp(edge/Mathf.Max(1,Screen.width),.3f,.95f);practiceCamera.rect=new Rect(0,0,width,1);
            Bounds b=FocusBounds();var direction=new Vector3(-1,.85f,-.65f).normalized;
            Vector3 position=b.center+direction*(b.extents.magnitude*2.8f+30);
            Quaternion rotation=Quaternion.LookRotation(-direction,Vector3.up);
            var right=rotation*Vector3.right;var up=rotation*Vector3.up;var e=b.extents;
            float horizontal=Mathf.Abs(right.x)*e.x+Mathf.Abs(right.y)*e.y+Mathf.Abs(right.z)*e.z;
            float vertical=Mathf.Abs(up.x)*e.x+Mathf.Abs(up.y)*e.y+Mathf.Abs(up.z)*e.z;
            float aspect=Mathf.Max(.1f,Screen.width*width/Mathf.Max(1f,Screen.height));
            float size=Mathf.Max(8,Mathf.Max(vertical,horizontal/aspect)*1.15f);
            float t=immediate?1:1-Mathf.Exp(-Time.unscaledDeltaTime*5);
            practiceCamera.transform.SetPositionAndRotation(Vector3.Lerp(practiceCamera.transform.position,position,t),Quaternion.Slerp(practiceCamera.transform.rotation,rotation,t));
            practiceCamera.orthographicSize=Mathf.Lerp(practiceCamera.orthographicSize,size,t);
        }
        public void End()
        {
            if(!IsPresenting)return;IsPresenting=false;
            gameplayCamera.enabled=oldGameEnabled;practiceCamera.enabled=oldPracticeEnabled;practiceCamera.rect=oldRect;practiceCamera.orthographicSize=oldSize;
            practiceCamera.transform.SetPositionAndRotation(oldPosition,oldRotation);
        }
        void OnDisable(){End();}
    }
}
