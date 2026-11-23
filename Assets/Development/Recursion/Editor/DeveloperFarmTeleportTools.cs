using System;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fyp.Development.Recursion.Editor
{
    public static class DeveloperFarmTeleportTools
    {
        public const string Folder="LocalBackups/DeveloperFarmTeleport_20261119";
        [MenuItem("Tools/FYP/Developer Farm Teleport/Audit and Back Up %#F1")]
        public static void Audit()
        {
            var scene=EditorSceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!="Assets/Scenes/MagicWorld_Development.unity")
                throw new InvalidOperationException("Open development scene in Edit Mode.");
            var dev=UnityEngine.Object.FindFirstObjectByType<RuinDeveloperPanel>();
            var farm=FloatingIslandPrototypeTools.Exact("Ruin02_MagicalFarm/FarmEntrySpawn");
            if(dev==null||dev.panel==null||farm==null)throw new InvalidOperationException("Existing developer panel or farm spawn missing.");
            Directory.CreateDirectory(Folder);
            if(!EditorSceneManager.SaveScene(scene,$"{Folder}/Before_{DateTime.Now:HHmmssfff}.unity",true))throw new IOException("Backup failed.");
            var log=new StringBuilder();
            log.AppendLine($"DEVELOPER {SceneMaintenanceC1Tools.Path(dev.transform)} panel={SceneMaintenanceC1Tools.Path(dev.panel.transform)} active={dev.panel.activeSelf}");
            foreach(var b in dev.panel.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                log.AppendLine($"BUTTON {SceneMaintenanceC1Tools.Path(b.transform)} active={b.gameObject.activeSelf} text={string.Join(" / ",b.GetComponentsInChildren<TMP_Text>(true).Select(t=>t.text))} persistentCallbacks={b.onClick.GetPersistentEventCount()}");
            log.AppendLine($"FarmEntrySpawn {SceneMaintenanceC1Tools.Path(farm)} {farm.position:F3}; console distance={Vector3.Distance(farm.position,farm.parent.Find("HarvestConsole").position):F3}");
            File.WriteAllText(Folder+"/UIAudit.txt",log.ToString());
            Debug.Log("Developer Farm teleport: existing UI inspected and live scene backed up.");
        }
        [MenuItem("Tools/FYP/Developer Farm Teleport/Replace Reset Control %#F9")]
        public static void Install()
        {
            var scene=EditorSceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!="Assets/Scenes/MagicWorld_Development.unity")
                throw new InvalidOperationException("Open development scene in Edit Mode.");
            if(!Directory.Exists(Folder)||Directory.GetFiles(Folder,"Before_*.unity").Length==0)
                throw new InvalidOperationException("Back up the live scene first.");
            var dev=UnityEngine.Object.FindFirstObjectByType<RuinDeveloperPanel>();
            var farm=FloatingIslandPrototypeTools.Exact("Ruin02_MagicalFarm/FarmEntrySpawn");
            if(dev.farmButton==null||dev.panel.GetComponentsInChildren<UnityEngine.UI.Button>(true).Length!=6||
                dev.farmButton.onClick.GetPersistentEventCount()!=0)
                throw new InvalidOperationException("Existing reset control/migrated button reference changed; inspect first.");
            Undo.RecordObject(dev,"Reference existing farm entry spawn");dev.farmEntrySpawn=farm;
            Undo.RecordObject(dev.farmButton.gameObject,"Replace reset button with Farm teleport");dev.farmButton.name="TeleportFarmButton";
            foreach(var label in dev.farmButton.GetComponentsInChildren<TMP_Text>(true))
            {
                Undo.RecordObject(label,"Label developer Farm teleport");label.text="Teleport to Magical Farm";
                EditorUtility.SetDirty(label);
            }
            EditorUtility.SetDirty(dev);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Debug.Log("Developer Mode saved: reset UI replaced with Farm teleport using existing FarmEntrySpawn. Jump reset/failure APIs and all other buttons preserved.");
        }
    }
}
