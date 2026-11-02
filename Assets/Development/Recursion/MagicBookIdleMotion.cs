using UnityEngine;

namespace Fyp.Development.Recursion
{
    // Scene-instance-only idle layer: it leaves the vendor Animator, clips and controller untouched.
    [DefaultExecutionOrder(100)]
    public sealed class MagicBookIdleMotion : MonoBehaviour
    {
        [Min(.01f)] public float verticalAmplitude=.012f;
        [Min(.01f)] public float cycleSeconds=2.8f;
        [Min(0f)] public float yawDegrees=2.2f;
        [Min(0f)] public float rollDegrees=1.2f;
        Vector3 basePosition;Quaternion baseRotation;float offset;
        void Awake(){basePosition=transform.localPosition;baseRotation=transform.localRotation;offset=transform.GetInstanceID()*.173f;}
        void LateUpdate()
        {
            float phase=(Time.unscaledTime+offset)/cycleSeconds*Mathf.PI*2f;
            transform.localPosition=basePosition+Vector3.up*(Mathf.Sin(phase)*verticalAmplitude);
            transform.localRotation=baseRotation*Quaternion.Euler(0f,Mathf.Sin(phase)*yawDegrees,Mathf.Sin(phase*1.37f)*rollDegrees);
        }
        void OnDisable(){transform.localPosition=basePosition;transform.localRotation=baseRotation;}
    }
}
