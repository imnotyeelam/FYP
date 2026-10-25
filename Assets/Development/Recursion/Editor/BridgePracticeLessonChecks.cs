using System;
using System.Collections.Generic;
using System.Linq;

namespace Fyp.Development.Recursion.Editor
{
    // Pure unit checks, runnable in the Editor and in an isolated C# console build.
    public static class BridgePracticeLessonChecks
    {
        private static int checks;
        private static void Check(bool value,string message)
        { checks++;if(!value)throw new InvalidOperationException("Recursion unit check: "+message); }
        private static string Signature(RecursionStep s) => s.Phase+"|"+s.Parameter+"|"+s.ActiveLine+"|"+
            string.Join(",",s.Frames)+"|"+s.ActionCount+"|"+s.Explanation;
        public static int Run()
        {
            checks=0;
            var lesson=new BridgePracticeLesson();var trace=lesson.Trace;
            var signatures=trace.Steps.Select(Signature).ToArray();
            var phases=new[]{"Enter","Check","Action","Call","Enter","Check","Action","Call","Enter","Check","Action","Call",
                "Enter","BaseCase","Return","Resume","Return","Resume","Return","Resume","Return","Complete"};
            int[] counts={0,0,1,1,1,1,2,2,2,2,3,3,3,3,3,3,3,3,3,3,3,3};
            int[] depths={1,1,1,1,2,2,2,2,3,3,3,3,4,4,4,3,3,2,2,1,1,0};
            var learning=new RecursionPlayback(trace);var independent=new RecursionPlayback(trace);
            for(int i=0;i<22;i++)
            {
                Check(learning.Index==i,"learning reaches every checkpoint");
                var s=learning.Current;Check(s.Phase.ToString()==phases[i],"phase at "+i);
                Check(s.Frames.Count==depths[i],"frame lifetime at "+i);
                Check(s.ActionCount==counts[i],"placements at "+i);
                Check(s.Frames.Count==0?s.Parameter==null:s.Parameter==s.Frames.Last(),"active n at "+i);
                Check(s.ActiveLine>=-1 && s.ActiveLine<trace.Code.Count,"code line bounds");
                var projection=new BridgeSnapshotProjection(s,3);
                Check(Enumerable.Range(0,3).Count(projection.IsPlaced)==counts[i],"2D placements from snapshot");
                Check(projection.Completed==(i==21),"complete only final snapshot");
                Check(independent.Index==0,"learning playback isolation");
                learning.Next();
            }
            learning.Next();Check(learning.Index==21,"next end bound");
            for(int i=20;i>=0;i--){learning.Previous();Check(Signature(learning.Current)==signatures[i],"reversible learning "+i);}
            learning.Previous();Check(learning.Index==0,"previous start bound");
            foreach(int i in new[]{21,13,2,10,17,0}){learning.JumpTo(i);Check(Signature(learning.Current)==signatures[i],"jump exact snapshot");}
            learning.Replay();Check(learning.Index==0,"learning replay");
            Check(trace.Steps.Count(s=>s.Phase==RecursionPhase.Action)==3,"exactly three positive placements");
            Check(trace.Steps.Where(s=>s.Phase==RecursionPhase.Action).Select(s=>s.Parameter).SequenceEqual(new int?[]{3,2,1}),"placement order");
            Check(trace.Steps[13].Frames.SequenceEqual(new[]{3,2,1,0}),"base creates no extra frame");
            Check(trace.Steps[14].Frames.Count==4 && trace.Steps[15].Frames.Count==3,"return frame persists until resume");
            Check(lesson.Decisions.Count==6 && lesson.Decisions.Select(q=>q.Id).Distinct().Count()==6,"six stable unique questions");
            var practice=new BridgePracticeSession(lesson);
            Check(!practice.TryAdvanceApprovedStep(),"no advancement without accepted answer");
            var replayTrace=new BridgePracticeLesson();
            Check(lesson.Decisions.Select(q=>q.Id).SequenceEqual(replayTrace.Decisions.Select(q=>q.Id)),"stable question IDs across sessions");
            var visited=new List<int>();
            foreach(var q in lesson.Decisions)
            {
                Check(practice.Index==q.CheckpointIndex,"gate checkpoint");
                Check(q.Answers.Select(a=>a.Id).Distinct().Count()==q.Answers.Count,"unique answer IDs per question");
                Check(q.Answers.Count(a=>a.Id==q.CorrectAnswerId)==1,"one deterministic correct answer");
                int before=practice.Index;
                Check(practice.Submit("future-or-stale",q.CorrectAnswerId).Status==PracticeSubmissionStatus.StaleQuestion,"stale question blocked");
                Check(practice.Submit(q.Id,"unknown").Status==PracticeSubmissionStatus.UnknownAnswer,"unknown answer blocked");
                foreach(var wrong in q.Answers.Where(a=>a.Id!=q.CorrectAnswerId))
                {
                    var result=practice.Submit(q.Id,wrong.Id);
                    Check(result.Status==PracticeSubmissionStatus.Incorrect && !result.Accepted,"incorrect deterministic");
                    Check(!string.IsNullOrWhiteSpace(result.Feedback),"immediate incorrect explanation");
                    Check(practice.Index==before && !practice.IsAdvancing,"incorrect changes nothing");
                    Check(!practice.TryAdvanceApprovedStep(),"incorrect never authorizes playback");
                }
                var accepted=practice.Submit(q.Id,q.CorrectAnswerId);
                Check(accepted.Accepted && !string.IsNullOrWhiteSpace(accepted.Feedback),"correct immediate feedback");
                Check(practice.Index==before,"submit itself does not skip animations");
                Check(practice.Submit(q.Id,q.CorrectAnswerId).Status==PracticeSubmissionStatus.Advancing,"double submit blocked");
                while(practice.IsAdvancing)
                {
                    int old=practice.Index;Check(practice.TryAdvanceApprovedStep(),"approved step advances");
                    Check(practice.Index==old+1,"never skip an internal checkpoint");visited.Add(practice.Index);
                    Check(Signature(practice.Current)==signatures[practice.Index],"exact source snapshot");
                    if(practice.Index<21)Check(!practice.IsComplete,"all stones alone cannot complete practice");
                    learning.JumpTo(21);learning.Replay();
                    Check(practice.Index==old+1,"Spellbook replay cannot reset gameplay");
                }
                Check(practice.Index==q.TargetIndex,"authorized range ends exactly");
                Check(!practice.TryAdvanceApprovedStep(),"cannot advance past gate");
                if(!practice.IsComplete)Check(practice.Submit(q.Id,q.CorrectAnswerId).Status==PracticeSubmissionStatus.StaleQuestion,"consumed answer cannot be reused");
            }
            Check(visited.SequenceEqual(Enumerable.Range(2,20)),"all approved internal checkpoints consumed exactly once");
            Check(practice.IsComplete && practice.Index==21 && practice.Current.Frames.Count==0,"checkpoint 22 plus empty stack plus six decisions");
            Check(practice.Submit(lesson.Decisions[5].Id,lesson.Decisions[5].CorrectAnswerId).Status==PracticeSubmissionStatus.Complete,"completed cannot resubmit");
            Check(!practice.TryAdvanceApprovedStep(),"completed cannot advance");
            Check(trace.Steps.Select(Signature).SequenceEqual(signatures),"source trace immutable");
            return checks;
        }
    }
}
