using System.Collections;
using UnityEngine;

namespace Fyp.Development.Recursion
{
    // Reuses the existing gameplay camera while the challenge owns movement and cursor state.
    // A very late execution order prevents legacy camera scripts from overwriting the cinematic pose.
    [DefaultExecutionOrder(32000)]
    public sealed class Ruin02CinematicCamera : MonoBehaviour
    {
        public Camera gameplayCamera;
        public Transform flightStartView, flightEndView;
        Vector3 savedPosition;
        Quaternion savedRotation;
        Vector3 desiredPosition;
        Quaternion desiredRotation;
        bool captured;

        public void Capture()
        {
            savedPosition = gameplayCamera.transform.position;
            savedRotation = gameplayCamera.transform.rotation;
            desiredPosition = savedPosition;
            desiredRotation = savedRotation;
            captured = true;
        }

        void LateUpdate()
        {
            if(captured)ApplyDesiredPose();
        }

        public IEnumerator Enter(Vector3 focus, float duration)
        {
            if(!captured)yield break;
            Vector3 from = gameplayCamera.transform.position;
            Quaternion fromRotation = gameplayCamera.transform.rotation;
            Quaternion toRotation = Look(flightStartView.position,focus,fromRotation);
            for(float elapsed=0;elapsed<duration;elapsed+=Time.unscaledDeltaTime)
            {
                float t = Ease(elapsed/duration);
                desiredPosition=Vector3.Lerp(from,flightStartView.position,t);
                desiredRotation=Quaternion.Slerp(fromRotation,toRotation,t);
                ApplyDesiredPose();
                yield return null;
            }
            desiredPosition=flightStartView.position;desiredRotation=toRotation;ApplyDesiredPose();
        }

        public void Track(float progress, Vector3 focus)
        {
            Track(progress, focus, null);
        }

        public void Track(float progress, Vector3 focus, Transform endView)
        {
            float t=Ease(progress);
            Transform view=endView!=null?endView:flightEndView;
            Vector3 position=Vector3.Lerp(flightStartView.position,view.position,t);
            desiredPosition=position;
            desiredRotation=Look(position,focus,desiredRotation);
            ApplyDesiredPose();
        }

        public IEnumerator Focus(Vector3 point, float duration)
        {
            Quaternion from=desiredRotation;
            Quaternion to=Look(desiredPosition,point,from);
            for(float elapsed=0;elapsed<duration;elapsed+=Time.unscaledDeltaTime)
            {
                desiredRotation=Quaternion.Slerp(from,to,Ease(elapsed/duration));ApplyDesiredPose();
                yield return null;
            }
            desiredRotation=to;ApplyDesiredPose();
        }

        public IEnumerator Return(float duration)
        {
            if(!captured)yield break;
            Vector3 from=desiredPosition;
            Quaternion fromRotation=desiredRotation;
            for(float elapsed=0;elapsed<duration;elapsed+=Time.unscaledDeltaTime)
            {
                float t=Ease(elapsed/duration);
                desiredPosition=Vector3.Lerp(from,savedPosition,t);
                desiredRotation=Quaternion.Slerp(fromRotation,savedRotation,t);
                ApplyDesiredPose();
                yield return null;
            }
            RestoreImmediately();
        }

        public void RestoreImmediately()
        {
            if(!captured)return;
            gameplayCamera.transform.SetPositionAndRotation(savedPosition,savedRotation);
            desiredPosition=savedPosition;desiredRotation=savedRotation;
            captured=false;
        }

        void ApplyDesiredPose()
        {
            if(gameplayCamera!=null)
                gameplayCamera.transform.SetPositionAndRotation(desiredPosition,desiredRotation);
        }

        static float Ease(float t){t=Mathf.Clamp01(t);return t*t*(3f-2f*t);}
        static Quaternion Look(Vector3 from,Vector3 focus,Quaternion fallback)
        {
            Vector3 direction=focus-from;
            return direction.sqrMagnitude>.001f?Quaternion.LookRotation(direction.normalized,Vector3.up):fallback;
        }
    }
}
