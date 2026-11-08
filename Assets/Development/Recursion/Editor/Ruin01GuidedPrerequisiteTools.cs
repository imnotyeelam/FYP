using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Fyp.Development.Recursion.Editor
{
    public static class Ruin01GuidedPrerequisiteTools
    {
        [MenuItem("Tools/FYP/Ruin 1/Guided Prerequisite/Run Play Assertions")]
        public static void RunPlayAssertions()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode first.");
            var challenge=UnityEngine.Object.FindObjectsByType<BridgePracticeController>(FindObjectsSortMode.None).Single();
            var book=UnityEngine.Object.FindObjectsByType<global::AncientSpellbookUI>(FindObjectsSortMode.None).Single();
            var guided=UnityEngine.Object.FindObjectsByType<Ruin01GuidedSimulationUI>(FindObjectsSortMode.None).Single();
            var simulation=guided.simulation;
            if(simulation==null)throw new InvalidOperationException("Ruin 1 standalone guided simulation is not assigned.");
            if(challenge.GuidedLessonCompleted || simulation.IsCompleted)throw new InvalidOperationException("Use a fresh Play session.");

            challenge.InteractFromShared();
            if(!guided.IsOpen || book.IsBookOpen || challenge.IsOpen)
                throw new InvalidOperationException("Locked bridge challenge did not open only the Ruin 1 standalone simulation.");

            guided.CloseEarly();
            if(challenge.GuidedLessonCompleted)throw new InvalidOperationException("Closing Ruin 1 guided simulation early unlocked practice.");
            challenge.InteractFromShared();

            simulation.JumpToStep(simulation.Playback.Trace.Steps.Count-1);
            if(simulation.IsCompleted || !guided.finishButton.gameObject.activeInHierarchy)
                throw new InvalidOperationException("Final Ruin 1 guided checkpoint did not require Finish.");
            guided.Finish();
            if(!simulation.IsCompleted || !book.GuidedLessonCompleted || !challenge.GuidedLessonCompleted || guided.IsOpen)
                throw new InvalidOperationException("Finish did not unlock Ruin 1 bridge practice.");

            challenge.InteractFromShared();
            if(!challenge.IsOpen || book.IsBookOpen || guided.IsOpen)
                throw new InvalidOperationException("Completed Ruin 1 lesson did not open only the bridge practice panel.");

            challenge.Close();
            if(!challenge.GuidedLessonCompleted)
                throw new InvalidOperationException("Ruin 1 lesson completion did not persist after closing the panel.");
            Debug.Log("PASS Ruin 1 guided prerequisite: standalone simulation requires Finish, gates bridge practice, and completion persists for this Play session.");
        }
    }
}
