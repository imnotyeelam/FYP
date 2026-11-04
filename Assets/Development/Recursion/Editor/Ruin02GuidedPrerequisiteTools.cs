using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Fyp.Development.Recursion.Editor
{
    public static class Ruin02GuidedPrerequisiteTools
    {
        [MenuItem("Tools/FYP/Ruin 2/Guided Prerequisite/Run Play Assertions")]
        public static void RunPlayAssertions()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode first.");
            var challenge=UnityEngine.Object.FindObjectsByType<Ruin02PracticeController>(FindObjectsSortMode.None).Single();
            var book=UnityEngine.Object.FindObjectsByType<Ruin02SpellbookUI>(FindObjectsSortMode.None).Single();
            var guided=UnityEngine.Object.FindObjectsByType<Ruin02GuidedSimulationUI>(FindObjectsSortMode.None).Single();
            var simulation=guided.simulation;
            if(challenge.GuidedLessonCompleted || simulation.IsCompleted)throw new InvalidOperationException("Use a fresh Play session.");
            challenge.InteractFromShared();
            if(!guided.IsOpen || book.IsBookOpen || challenge.IsOpen)throw new InvalidOperationException("Locked challenge did not open only the standalone simulation.");
            guided.CloseEarly();
            if(challenge.GuidedLessonCompleted)throw new InvalidOperationException("Closing guided simulation early unlocked practice.");
            challenge.InteractFromShared();simulation.JumpToStep(simulation.Playback.Trace.Steps.Count-1);
            if(simulation.IsCompleted || !guided.finishButton.gameObject.activeInHierarchy)throw new InvalidOperationException("Final checkpoint did not require Finish.");
            guided.Finish();
            if(!simulation.IsCompleted || !challenge.GuidedLessonCompleted || guided.IsOpen)throw new InvalidOperationException("Finish did not persist completion and close simulation.");
            challenge.InteractFromShared();
            if(!challenge.IsOpen || book.IsBookOpen || guided.IsOpen)throw new InvalidOperationException("Completed lesson did not unlock only the practice panel.");
            challenge.Close();
            if(!challenge.GuidedLessonCompleted)throw new InvalidOperationException("Lesson completion did not survive reopening state.");
            Debug.Log("PASS Ruin 2 guided prerequisite: landmark opens standalone simulation, Finish gates unlock, and completion persists in this Play session.");
        }
    }
}
