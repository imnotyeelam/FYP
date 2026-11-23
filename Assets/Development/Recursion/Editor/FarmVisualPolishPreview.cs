using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Fyp.Development.Recursion.Editor
{
    [InitializeOnLoad]
    public static class FarmVisualPolishPreview
    {
        const string Key="FarmVisual.Preview";
        static double began;static int stage,frame,errors;static Keyboard keyboard;
        static Ruin02HarvestConsole console;
        static FarmVisualPolishPreview(){EditorApplication.update+=Tick;}
        [MenuItem("Tools/FYP/Farm Visual Polish/Capture Game View %#&0")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Begin in Edit Mode.");
            began=0;stage=errors=0;console=null;keyboard=null;SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
        }
        static void Log(string m,string t,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;}
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
            try
            {
                if(began==0){began=EditorApplication.timeSinceStartup;Application.logMessageReceived+=Log;return;}
                if(EditorApplication.timeSinceStartup-began>25)throw new TimeoutException("Stage "+stage);
                if(console==null)
                {
                    if(EditorApplication.timeSinceStartup-began<1)return;
                    console=FarmVisualPolishTools.Console;
                    var dev=UnityEngine.Object.FindFirstObjectByType<RuinDeveloperPanel>();dev.Open();dev.TeleportToMagicalFarm();
                    keyboard=InputSystem.AddDevice<Keyboard>("Farm visual QA");Press(false);Next(1);return;
                }
                if(Time.frameCount<frame)return;
                switch(stage)
                {
                    case 1:Press(true);Next(2);break;
                    case 2:
                        if(!console.IsOpen)throw new InvalidOperationException("Real E did not open console.");
                        Press(false);Directory.CreateDirectory(FarmVisualPolishTools.Folder);
                        ScreenCapture.CaptureScreenshot(FarmVisualPolishTools.Folder+"/"+(console.panel.transform.Find("SkyBackdrop")!=null?"After":"Before")+"_1920.png");Next(3);break;
                    case 3:
                        console.RequestClose();Next(4);break;
                    case 4:
                        if(console.IsOpen||!console.movement.enabled||!console.cameraFollow.enabled||Cursor.lockState!=CursorLockMode.Locked||errors!=0)
                            throw new InvalidOperationException("UI close/restoration or Console error.");
                        Finish("PASS visual preview: existing Developer teleport and shared E opened farm console; camera/controls/cursor restored on close. Play errors=0.");break;
                }
            }
            catch(Exception e){Finish("FAIL visual preview "+e);}
        }
        static void Press(bool down){keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,down?new KeyboardState(UnityEngine.InputSystem.Key.E):new KeyboardState());}
        static void Next(int n){stage=n;frame=Time.frameCount+12;}
        static void Finish(string result)
        {
            SessionState.SetBool(Key,false);Application.logMessageReceived-=Log;
            if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);
            File.WriteAllText(FarmVisualPolishTools.Folder+"/PreviewResult.txt",result);
            if(result.StartsWith("PASS"))Debug.Log(result);else Debug.LogError(result);EditorApplication.isPlaying=false;
        }
    }
}
