using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Fyp.Development.Recursion.Editor
{
    [InitializeOnLoad]
    public static class SceneMaintenanceC1Probe
    {
        const string Key="C1.PlayProbe";
        static double began;static int stage,frame;static StringBuilder log;
        static Ruin01MagicJumpController jump;static RuinDeveloperPanel dev;
        static SceneMaintenanceC1Probe(){EditorApplication.update+=Tick;}
        [MenuItem("Tools/FYP/Scene Maintenance C1/Diagnose Runtime Eligibility %#F6")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Begin in Edit Mode.");
            began=0;stage=0;jump=null;log=new StringBuilder();SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
            try
            {
                if(began==0){began=EditorApplication.timeSinceStartup;log ??= new StringBuilder();return;}
                if(EditorApplication.timeSinceStartup-began>15)throw new TimeoutException("Stage "+stage);
                if(jump==null)
                {
                    if(EditorApplication.timeSinceStartup-began<1)return;
                    jump=UnityEngine.Object.FindFirstObjectByType<Ruin01MagicJumpController>(FindObjectsInactive.Include);
                    dev=UnityEngine.Object.FindFirstObjectByType<RuinDeveloperPanel>();
                    State("Fresh session, initial player position");
                    dev.Open();dev.TeleportToRuin01JumpStart();stage=1;frame=Time.frameCount+10;return;
                }
                if(Time.frameCount<frame)return;
                if(stage==1)
                {
                    State("Developer teleport, normal prerequisites");
                    dev.Open();dev.UnlockRuin01JumpForTesting();dev.Close();stage=2;frame=Time.frameCount+10;return;
                }
                State("Developer test override");
                Finish();
            }
            catch(Exception e){log.AppendLine(e.ToString());Finish();}
        }
        static void State(string label)
        {
            var hints=InteractionHintManager.Instance;var text=hints.hintText;
            log.AppendLine(label);
            log.AppendLine($"player={jump.player.position:F3} anchor={jump.interactionAnchor.position:F3} distance={Vector3.Distance(jump.player.position,jump.interactionAnchor.position):F4} eligible={jump.CanSharedInteract} active={jump.isActiveAndEnabled} surface={jump.jumpStartSurface.enabled} bridge={jump.bridgePractice.IsComplete} guided={jump.bridgePractice.GuidedLessonCompleted} testOverride={jump.DeveloperPrerequisitesUnlocked} cursor={Cursor.lockState}/{CursorManager.allowCursorLock}");
            log.AppendLine($"hintActive={text.gameObject.activeInHierarchy} text={text.text} color={text.color} alpha={text.alpha} rendererAlpha={text.canvasRenderer.GetInheritedAlpha()} cull={text.canvasRenderer.cull} rect={text.rectTransform.rect} position={text.transform.position} CanvasActive={text.canvas.isActiveAndEnabled}");
            log.AppendLine("ACTIVE MODALS "+string.Join(",",hints.modalPanels.Where(x=>x!=null&&x.activeInHierarchy).Select(x=>SceneMaintenanceC1Tools.Path(x.transform))));
            log.AppendLine("DEV canOpen="+dev.CanOpen+" open="+dev.IsOpen);
        }
        static void Finish()
        {
            File.WriteAllText(SceneMaintenanceC1Tools.Folder+"/RuntimeDiagnosis.txt",log.ToString());
            SessionState.SetBool(Key,false);EditorApplication.isPlaying=false;Debug.Log("C1 runtime diagnosis written.");
        }
    }
}
