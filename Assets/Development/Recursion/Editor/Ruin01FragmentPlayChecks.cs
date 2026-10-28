using System;
using System.IO;
using System.Linq;
using System.Text;
using Fyp.Development.Progress;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Fyp.Development.Recursion.Editor
{
    [InitializeOnLoad]
    public static class Ruin01FragmentPlayChecks
    {
        const string Key = "FYP.D.FragmentChecks";
        const string Output = "Temp/Ruin01PhaseD";
        static Ruin01FragmentReward reward;
        static BridgePracticeController practice;
        static FragmentProgressTracker progress;
        static RuinEntrance entry, exit;
        static Keyboard keyboard;
        static StringBuilder log;
        static double began, stageAt;
        static int stage, decisions, assertions, errors;

        static Ruin01FragmentPlayChecks()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key, false))
                {
                    SessionState.SetBool(Key, false);
                    Application.logMessageReceived -= OnLog;
                }
                if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Key + ".After", false)) return;
                SessionState.SetBool(Key + ".After", false);
                EditorApplication.delayCall += () => EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), Output + "/AfterPlay.unity", true);
            };
            if (SessionState.GetBool(Key, false)) Application.logMessageReceived += OnLog;
        }
        static void OnLog(string message, string trace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++;
        }
        static void Check(bool okay, string reason)
        {
            assertions++;
            if (!okay) throw new InvalidOperationException("Phase D: " + reason);
        }
        static void Stage(int next) { stage = next; stageAt = EditorApplication.timeSinceStartup; }

        [MenuItem("Tools/FYP/Ruin 1/Phase D/Run Fragment Play Checks %#&F5")]
        public static void Run()
        {
            SpellbookStageTools.RequireScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start Phase D checks in Edit Mode.");
            var root = FloatingIslandPrototypeTools.Exact("Ruin01_Area/Interactables/FragmentOfCalling");
            var gem = root.Find("Diamondo_FragmentOfCalling");
            Check(gem != null && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gem.gameObject) == Ruin01FragmentTools.GemPath,
                "reward is a scene instance of Diamondo");
            Check(root.GetComponent<Ruin01FragmentReward>() != null, "reward controller installed");
            Directory.CreateDirectory(Output);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), Output + "/BeforePlay.unity", true);
            reward = null; practice = null; progress = null; entry = null; exit = null; keyboard = null;
            log = new StringBuilder("PHASE D ACTUAL UNITY PLAY MODE\n");
            stage = 0; decisions = 0; assertions = 0; errors = 0; began = 0;
            Application.logMessageReceived -= OnLog; Application.logMessageReceived += OnLog;
            SessionState.SetBool(Key, true); EditorApplication.isPlaying = true;
        }
        static void Finish(bool pass, string reason = null)
        {
            if (log == null) log = new StringBuilder("PHASE D ACTUAL UNITY PLAY MODE\n");
            Application.logMessageReceived -= OnLog;
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            log.AppendLine((pass ? "PASS" : "FAIL") + " assertions=" + assertions + " Console errors=" + errors);
            if (reason != null) log.AppendLine(reason);
            Directory.CreateDirectory(Output); File.WriteAllText(Output + "/PlayChecks.txt", log.ToString());
            if (pass) Debug.Log(log.ToString()); else Debug.LogError(log.ToString());
            SessionState.SetBool(Key, false); SessionState.SetBool(Key + ".After", true);
            EditorApplication.isPlaying = false;
        }
        static void Tick()
        {
            if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying) return;
            if (log == null) log = new StringBuilder("PHASE D ACTUAL UNITY PLAY MODE\n");
            try
            {
                if (began == 0) { began = EditorApplication.timeSinceStartup; return; }
                if (EditorApplication.timeSinceStartup - began > 150) throw new TimeoutException("Phase D timed out at stage " + stage);
                if (stage == 0)
                {
                    if (EditorApplication.timeSinceStartup - began < 1) return;
                    reward = FloatingIslandPrototypeTools.Exact("Ruin01_Area/Interactables/FragmentOfCalling").GetComponent<Ruin01FragmentReward>();
                    progress = FloatingIslandPrototypeTools.Exact("Ruin01_Area/Gameplay/FragmentProgressTracker").GetComponent<FragmentProgressTracker>();
                    practice = BridgePracticeTools.Controller();
                    Check(reward.practice == practice && reward.progress == progress, "explicit practice and tracker references");
                    Check(InteractionHintManager.Instance.interactionTargets.Count(x => x == reward) == 1, "one shared interaction registration");
                    Check(!reward.ChallengeSolved && !reward.FragmentCollected && !reward.gemVisual.activeSelf && !reward.CanSharedInteract, "fragment unavailable before challenge");
                    Check(progress.CollectedCount == 0 && !progress.AllThreeCollected, "new play session has no fragments");
                    Check(reward.SharedHint == "Press [E] to Collect Fragment", "precise shared prompt");
                    var manager = InteractionHintManager.Instance;
                    entry = FloatingIslandPrototypeTools.Exact("MainHub/Ruin1").GetComponent<RuinEntrance>();
                    exit = FloatingIslandPrototypeTools.Exact("Ruin01_Area/Interactables/ReturnInteractionPoint").GetComponent<RuinEntrance>();
                    Check(manager.coordinateInteractions && manager.interactionTargets.Contains(entry) && manager.interactionTargets.Contains(exit),
                        "Main Hub entry and Ruin 1 return registered with one shared manager");
                    Check(exit.enabled && exit.useSharedInteraction && exit.destination == FloatingIslandPrototypeTools.Exact("MainHub/Ruin1/Destination"),
                        "return target and Main Hub destination retained");
                    var returnVisual=FloatingIslandPrototypeTools.Exact("Ruin01_Area/Interactables/ReturnInteractionPoint/ReturnMagicRock");
                    Check(returnVisual.GetComponentsInChildren<Renderer>(true).Any(r=>r.enabled) &&
                        returnVisual.GetComponentsInChildren<Collider>(true).All(c=>!c.enabled) &&
                        FloatingIslandPrototypeTools.Exact("Ruin01_Area/Interactables/ReturnInteractionPoint/ReturnBeaconLight")
                            .GetComponent<Light>().enabled,
                        "return landmark visible without blocking collisions");
                    keyboard = InputSystem.AddDevice<Keyboard>("FYP Phase D QA Keyboard"); keyboard.MakeCurrent();
                    var player = FloatingIslandPrototypeTools.Exact("Player").GetComponent<CharacterController>();
                    player.enabled = false; player.transform.position = entry.transform.position + new Vector3(-1.2f,.5f,0); player.enabled = true;
                    Physics.SyncTransforms(); InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    Stage(-2); return;
                }
                if (stage == -2)
                {
                    if (EditorApplication.timeSinceStartup - stageAt < .35) return;
                    Check(InteractionHintManager.Instance.hintText.gameObject.activeInHierarchy &&
                        InteractionHintManager.Instance.hintText.text == entry.SharedHint, "Main Hub entrance hint visible");
                    keyboard.MakeCurrent(); InputSystem.QueueStateEvent(keyboard, new KeyboardState(UnityEngine.InputSystem.Key.E));
                    Stage(-1); return;
                }
                if (stage == -1)
                {
                    if (EditorApplication.timeSinceStartup - stageAt < .35) return;
                    var cc = FloatingIslandPrototypeTools.Exact("Player").GetComponent<CharacterController>();
                    Check(Vector2.Distance(new Vector2(cc.transform.position.x,cc.transform.position.z),
                        new Vector2(entry.destination.position.x,entry.destination.position.z)) < .35f && cc.transform.position.y > 38,
                        "E teleports from Main Hub to Ruin 1 spawn");
                    Check(cc.enabled && CursorManager.allowCursorLock && Cursor.lockState == CursorLockMode.Locked,
                        "entry restores active controller and locked cursor");
                    keyboard.MakeCurrent(); InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    var book = FloatingIslandPrototypeTools.Exact("Magic Book").GetComponent<KnowledgeInteractable>().spellbookUI;
                    book.OpenBook();
                    for (int i = 0; i < 3; i++) book.nextButton.onClick.Invoke();
                    book.guidedSimulation.JumpToStep(21);
                    Check(!reward.ChallengeSolved && !reward.gemVisual.activeSelf && progress.CollectedCount == 0,
                        "Spellbook checkpoint 22 does not unlock or collect fragment");
                    book.guidedSimulation.Replay(); book.CloseBook();
                    practice.Open(); Check(practice.IsOpen, "practice opens after book closes");
                    log.AppendLine("Main Hub entry E, locked reward, shared registration and independent Spellbook verified.");
                    Stage(1); return;
                }
                if (stage == 1)
                {
                    if (practice.IsComplete) { Stage(2); return; }
                    Check(!reward.ChallengeSolved && !reward.gemVisual.activeSelf, "no early reward during execution");
                    if (practice.IsBusy || practice.CurrentDecision == null || practice.CheckpointIndex != practice.CurrentDecision.CheckpointIndex) return;
                    var q = practice.CurrentDecision;
                    var answer = practice.SubmitAnswer(q.Id, q.CorrectAnswerId);
                    Check(answer.Accepted, "correct decision " + q.Id + " accepted");
                    decisions++;
                    return;
                }
                if (stage == 2)
                {
                    Check(decisions == 6 && practice.CheckpointIndex == 21 && practice.Current.Frames.Count == 0, "six decisions finish all 22 checkpoints with empty stack");
                    Check(practice.RestorationCount == 3 && practice.VisitedCheckpoints.SequenceEqual(Enumerable.Range(0, 22)), "three placements and all snapshots");
                    Check(reward.ChallengeSolved && !reward.FragmentCollected && reward.RevealCount == 1 && reward.CollectionCount == 0,
                        "completion reveals once without collecting");
                    Check(reward.gemVisual.activeSelf && !reward.CanSharedInteract, "gem visible but modal prevents collection");
                    Check(new[] { practice.rig.placeStone3, practice.rig.placeStone2, practice.rig.placeStone1 }
                        .All(m => m.IsRepaired && m.walkableColliders.All(c => c.enabled)), "all repaired bridge collisions remain on");
                    practice.Close(); Check(!practice.IsOpen, "practice closes normally");
                    Check(CursorManager.allowCursorLock &&
                        FloatingIslandPrototypeTools.Exact("Player").GetComponent<PlayerMovement>().enabled &&
                        FloatingIslandPrototypeTools.Exact("Main Camera").GetComponent<CameraFollow>().enabled,
                        "cursor, movement and camera restored after practice");
                    var returnPoint = FloatingIslandPrototypeTools.Exact("Ruin01_Area/Interactables/ReturnInteractionPoint");
                    Check(returnPoint.GetComponent<RuinEntrance>().enabled &&
                        Vector3.Distance(returnPoint.position, new Vector3(-440f, 46.08545f, 166f)) < .001f,
                        "aligned return teleport remains enabled on the destination island");
                    WalkFromFinalModuleToReward();
                    Check(reward.CanSharedInteract, "real Player CharacterController reached collection range");
                    keyboard.MakeCurrent();
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    Stage(3); return;
                }
                if (stage == 3)
                {
                    if (EditorApplication.timeSinceStartup - stageAt < .4) return;
                    Check(InteractionHintManager.Instance.hintText.gameObject.activeInHierarchy &&
                        InteractionHintManager.Instance.hintText.text == reward.SharedHint, "nearest shared hint selects fragment");
                    keyboard.MakeCurrent(); InputSystem.QueueStateEvent(keyboard, new KeyboardState(UnityEngine.InputSystem.Key.E));
                    Stage(4); return;
                }
                if (stage == 4)
                {
                    if (EditorApplication.timeSinceStartup - stageAt < .4) return;
                    Check(reward.FragmentCollected && progress.HasFragment(FragmentProgressTracker.Ruin01) && progress.CollectedCount == 1,
                        "one E press registers Ruin01 exactly once");
                    Check(reward.CollectionCount == 1 && !reward.gemVisual.activeSelf && !reward.CanSharedInteract, "gem hides and cannot be recollected");
                    Check(reward.confirmationText.gameObject.activeInHierarchy, "reward confirmation displayed");
                    Check(!progress.AllThreeCollected, "one fragment does not satisfy three-fragment condition");
                    reward.InteractFromShared(); Check(reward.CollectionCount == 1 && progress.CollectedCount == 1, "repeat interaction cannot duplicate reward");
                    keyboard.MakeCurrent(); InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    var cc = FloatingIslandPrototypeTools.Exact("Player").GetComponent<CharacterController>();
                    var original = cc.transform.position;
                    cc.enabled = false; cc.transform.position = practice.interactionPoint.position + Vector3.down * .27f; cc.enabled = true;
                    Physics.SyncTransforms(); practice.Open();
                    Check(practice.IsOpen && practice.IsComplete && reward.ChallengeSolved && progress.CollectedCount == 1,
                        "reopen completed challenge preserves progress");
                    practice.Close();
                    cc.enabled = false; cc.transform.position = original; cc.enabled = true; Physics.SyncTransforms();
                    Check(!reward.gemVisual.activeSelf && reward.CollectionCount == 1, "returning to reward area remains collected");
                    TestTrackerUniqueness();
                    WalkRewardToReturn();
                    Check(Vector3.Distance(cc.transform.position, exit.transform.position) < exit.interactionDistance,
                        "CharacterController reaches the return interaction from fragment");
                    Stage(5); return;
                }
                if (stage == 5)
                {
                    if (EditorApplication.timeSinceStartup - stageAt < .4) return;
                    Check(InteractionHintManager.Instance.hintText.gameObject.activeInHierarchy &&
                        InteractionHintManager.Instance.hintText.text == exit.SharedHint,
                        "shared return hint visible even after reward confirmation");
                    keyboard.MakeCurrent(); InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.E));
                    Stage(6); return;
                }
                if (stage == 6)
                {
                    if (EditorApplication.timeSinceStartup - stageAt < .4) return;
                    var cc = FloatingIslandPrototypeTools.Exact("Player").GetComponent<CharacterController>();
                    Check(Vector2.Distance(new Vector2(cc.transform.position.x,cc.transform.position.z),
                        new Vector2(exit.destination.position.x,exit.destination.position.z))<.35f,
                        "return E teleports to Main Hub destination");
                    Check(cc.enabled && cc.GetComponent<PlayerMovement>().enabled &&
                        FloatingIslandPrototypeTools.Exact("Main Camera").GetComponent<CameraFollow>().enabled &&
                        CursorManager.allowCursorLock && Cursor.lockState==CursorLockMode.Locked,
                        "movement, camera and cursor operational in Main Hub");
                    Check(progress.HasFragment(FragmentProgressTracker.Ruin01) && reward.CollectionCount==1 && !reward.gemVisual.activeSelf,
                        "Ruin01 fragment persists after returning");
                    keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                    cc.enabled=false;cc.transform.position=entry.transform.position+new Vector3(-1.2f,.5f,0);cc.enabled=true;Physics.SyncTransforms();
                    Stage(7);return;
                }
                if (stage == 7)
                {
                    if(EditorApplication.timeSinceStartup-stageAt<.4)return;
                    Check(InteractionHintManager.Instance.hintText.gameObject.activeInHierarchy &&
                        InteractionHintManager.Instance.hintText.text==entry.SharedHint,
                        "interaction hint works in Main Hub after return");
                    Check(errors == 0, "no Console errors");
                    log.AppendLine("Six decisions, 22 checkpoints, crossing to gem, one shared E collection, walk to exit, return E, Main Hub controls and persistent fragment passed.");
                    Finish(true); return;
                }
            }
            catch (Exception ex) { Finish(false, ex.ToString()); }
        }
        static void TestTrackerUniqueness()
        {
            var temporary = new GameObject("Temporary Phase D progress test");
            try
            {
                var tracker = temporary.AddComponent<FragmentProgressTracker>(); int allEvents = 0, fragmentEvents = 0;
                tracker.AllFragmentsCollected += () => allEvents++;
                tracker.FragmentCollected += _ => fragmentEvents++;
                Check(!tracker.TryCollect("Unknown") && !tracker.TryCollect(null), "reject unknown fragment IDs");
                foreach (var id in new[] { FragmentProgressTracker.Ruin01, FragmentProgressTracker.Ruin02, FragmentProgressTracker.Ruin03 })
                {
                    Check(tracker.TryCollect(id), "unique fragment added: " + id);
                    Check(!tracker.TryCollect(id), "duplicate fragment rejected: " + id);
                }
                Check(tracker.AllThreeCollected && tracker.CollectedCount == 3 && allEvents == 1 && fragmentEvents == 3,
                    "three distinct IDs emit completion once");
            }
            finally { UnityEngine.Object.Destroy(temporary); }
        }
        static void WalkRewardToReturn()
        {
            var cc=FloatingIslandPrototypeTools.Exact("Player").GetComponent<CharacterController>();
            var movement=cc.GetComponent<PlayerMovement>();bool originalEnabled=movement.enabled;
            movement.enabled=false;
            try
            {
                float vertical=-2;var goal=exit.transform.position;
                bool reached=false;
                for(int frame=0;frame<1800;frame++)
                {
                    var p=cc.transform.position;var delta=new Vector2(goal.x-p.x,goal.z-p.z);
                    if(delta.magnitude<.45f){reached=true;break;}
                    if(p.y<40)break;
                    if(cc.isGrounded && vertical<0)vertical=-2;
                    vertical+=movement.gravity/60f;
                    var move=delta.normalized*movement.moveSpeed/60f;
                    cc.Move(new Vector3(move.x,vertical/60f,move.y));
                }
                Check(reached && cc.transform.position.y>44,"Player CharacterController walks from fragment to return landmark");
                log.AppendLine("CharacterController walked from collected fragment to existing return interaction.");
            }
            finally{movement.enabled=originalEnabled;}
        }
        static void WalkFromFinalModuleToReward()
        {
            var cc = FloatingIslandPrototypeTools.Exact("Player").GetComponent<CharacterController>();
            var movement = cc.GetComponent<PlayerMovement>(); bool wasEnabled = movement.enabled;
            movement.enabled = false;
            var target = reward.interactionPoint.position;
            var start = practice.rig.placeStone1.repairedPose.position;
            var ground = Physics.RaycastAll(new Vector3(start.x, 110, start.z + 2), Vector3.down, 130, ~0, QueryTriggerInteraction.Ignore)
                .Where(h => h.collider.transform.IsChildOf(practice.rig.placeStone1.transform)).OrderByDescending(h => h.point.y).First();
            cc.enabled = false; cc.transform.position = ground.point + Vector3.up * .2f; cc.enabled = true; Physics.SyncTransforms();
            for (int i = 0; i < 30; i++) cc.Move(Vector3.down * .05f);
            float vertical = -2;
            var path = new[] { new Vector2(start.x, target.z - 1), new Vector2(target.x, target.z - 1) };
            foreach (var point in path)
            {
                bool reached = false;
                for (int frame = 0; frame < 3000; frame++)
                {
                    var p = cc.transform.position; var delta = new Vector2(point.x - p.x, point.y - p.z);
                    if (delta.magnitude < .45f) { reached = true; break; }
                    if (p.y < 37) break;
                    if (cc.isGrounded && vertical < 0) vertical = -2;
                    vertical += movement.gravity / 60f;
                    var move = delta.normalized * movement.moveSpeed / 60f;
                    cc.Move(new Vector3(move.x, vertical / 60f, move.y));
                }
                Check(reached && cc.transform.position.y > 38, "CharacterController traverses repaired far-side route to fragment");
            }
            movement.enabled = wasEnabled;
            log.AppendLine("Actual Player CharacterController traversed from Bridge (3) to fragment on destination island.");
        }
    }
}
