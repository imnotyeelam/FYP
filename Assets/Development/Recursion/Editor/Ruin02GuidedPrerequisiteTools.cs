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
            var simulation=book.guidedSimulation;
            if(challenge.GuidedLessonCompleted || simulation.IsCompleted)throw new InvalidOperationException("Use a fresh Play session.");
            challenge.InteractFromShared();
            if(!book.IsBookOpen || challenge.IsOpen || !book.titleText.text.Contains("Complete the guided simulation to unlock this challenge"))
                throw new InvalidOperationException("Locked challenge did not open the guided simulation with its prerequisite instruction.");
            book.CloseBook();book.OpenGuidedSimulationForChallenge();simulation.JumpToStep(simulation.Playback.Trace.Steps.Count-1);
            if(!simulation.IsCompleted || !book.GuidedLessonCompleted)throw new InvalidOperationException("Final guided checkpoint did not persist completion.");
            book.CloseBook();challenge.InteractFromShared();
            if(!challenge.IsOpen || book.IsBookOpen)throw new InvalidOperationException("Completed lesson did not unlock only the practice panel.");
            challenge.Close();
            if(!challenge.GuidedLessonCompleted)throw new InvalidOperationException("Lesson completion did not survive reopening state.");
            Debug.Log("PASS Ruin 2 guided prerequisite: locked landmark opens Page 4, final checkpoint unlocks practice, and completion persists in this Play session.");
        }
    }
}
