using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Fyp.Development.Recursion.Editor
{
    [InitializeOnLoad]
    public static class SceneMaintenanceC2Checks
    {
        const string Key="SceneMaintenanceC2.Suite";
        static SceneMaintenanceC2Checks(){EditorApplication.update+=Tick;}
        [MenuItem("Tools/FYP/Scene Maintenance C2/Run Gameplay Verification %#F4")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||SessionState.GetInt(Key,0)!=0)
                throw new InvalidOperationException("Begin in Edit Mode with no suite running.");
            SceneMaintenanceC2Tools.Verify();
            SessionState.SetString(Key+".Results","");Begin(1);
        }
        [MenuItem("Tools/FYP/Scene Maintenance C2/Retry Portal Verification Only %#F2")]
        public static void RetryPortal()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||SessionState.GetInt(Key,0)!=0)
                throw new InvalidOperationException("Begin in Edit Mode.");
            SceneMaintenanceC2Tools.Verify();EditorApplication.isPaused=false;Begin(3);
        }
        static string Output(int phase)=>phase==1?"Temp/SceneMaintenanceC1/PlayChecks.txt":
            phase==2?"Temp/SceneMaintenanceC2Navigation/PlayChecks.txt":"Temp/FinalPortalP1/PlayChecks.txt";
        static void Begin(int phase)
        {
            SessionState.SetInt(Key,phase);
            var output=Output(phase);
            SessionState.SetString(Key+".Stamp",File.Exists(output)?File.GetLastWriteTimeUtc(output).Ticks.ToString():"0");
            SessionState.SetString(Key+".Start",DateTime.UtcNow.Ticks.ToString());
            if(phase==1)SceneMaintenanceC1Checks.Run();
            else if(phase==2)SceneMaintenanceC2NavigationChecks.Run();
            else FinalPortalP1PlayChecks.Run();
        }
        static void Tick()
        {
            int phase=SessionState.GetInt(Key,0);
            if(phase==0)return;
            if(EditorApplication.isPlaying)
            {
                // QA must keep advancing if focus moves away from Game View between suites.
                if(!SessionState.GetBool(Key+".BackgroundSaved",false))
                {
                    SessionState.SetBool(Key+".BackgroundBefore",Application.runInBackground);
                    SessionState.SetBool(Key+".BackgroundSaved",true);
                }
                Application.runInBackground=true;
                return;
            }
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)return;
            try
            {
                var output=Output(phase);
                if(!File.Exists(output)||File.GetLastWriteTimeUtc(output).Ticks<=long.Parse(SessionState.GetString(Key+".Stamp","0")))
                {
                    if(DateTime.UtcNow.Ticks-long.Parse(SessionState.GetString(Key+".Start","0"))>TimeSpan.FromMinutes(4).Ticks)
                        throw new TimeoutException("No fresh result from stage "+phase);
                    return;
                }
                var result=File.ReadAllText(output);
                if(!result.StartsWith("PASS"))throw new InvalidOperationException(result);
                SceneMaintenanceC2Tools.Verify();
                if(SessionState.GetBool(Key+".BackgroundSaved",false))
                {
                    Application.runInBackground=SessionState.GetBool(Key+".BackgroundBefore",false);
                    SessionState.SetBool(Key+".BackgroundSaved",false);
                }
                var results=SessionState.GetString(Key+".Results","")+"Stage "+phase+": "+result+"\n";
                SessionState.SetString(Key+".Results",results);
                File.WriteAllText(SceneMaintenanceC2Tools.Folder+"/PlayVerification.txt",results);
                Debug.Log("C2 gameplay verification stage "+phase+" passed.");
                if(phase<3){Begin(phase+1);return;}
                SessionState.SetInt(Key,0);
                File.AppendAllText(SceneMaintenanceC2Tools.Folder+"/PlayVerification.txt","PASS C2: all three suites passed; archive remains exactly five inactive roots.\n");
                Debug.Log("PASS C2: Ruin 1 Jump/Fragment, Ruin 2 Staff/Farm/Fragment/Hub and Final Portal P1 gate verified. Play errors=0.");
            }
            catch(Exception e){SessionState.SetInt(Key,0);File.WriteAllText(SceneMaintenanceC2Tools.Folder+"/PlayVerification.txt","FAIL C2 "+e);Debug.LogException(e);}
        }
    }
}
