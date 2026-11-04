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
            var simulation=book.guidedSimulation;
            if(simulation==null)throw new InvalidOperationException("Ruin 1 Page 4 simulation is not assigned.");
            if(challenge.GuidedLessonCompleted || simulation.IsCompleted)throw new InvalidOperationException("Use a fresh Play session.");

            challenge.InteractFromShared();
            if(!book.IsBookOpen || challenge.IsOpen || !book.titleText.text.Contains("Complete the guided simulation to unlock this challenge"))
                throw new InvalidOperationException("Locked bridge challenge did not open Ruin 1 Page 4.");

            simulation.JumpToStep(simulation.Playback.Trace.Steps.Count-1);
            if(!simulation.IsCompleted || !book.GuidedLessonCompleted || !challenge.GuidedLessonCompleted)
                throw new InvalidOperationException("Final Ruin 1 guided checkpoint did not unlock the challenge.");

            book.CloseBook();
            challenge.InteractFromShared();
            if(!challenge.IsOpen || book.IsBookOpen)
                throw new InvalidOperationException("Completed Ruin 1 lesson did not open only the bridge practice panel.");

            challenge.Close();
            if(!challenge.GuidedLessonCompleted)
                throw new InvalidOperationException("Ruin 1 lesson completion did not persist after closing the panel.");
            Debug.Log("PASS Ruin 1 guided prerequisite: Page 4 gates bridge practice and completion persists for this Play session.");
        }
    }
}
