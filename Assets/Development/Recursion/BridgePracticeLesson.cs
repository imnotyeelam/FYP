using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Fyp.Development.Recursion
{
    public sealed class PracticeAnswer
    {
        public string Id { get; }
        public string Text { get; }
        public string Feedback { get; }
        public PracticeAnswer(string id, string text, string feedback)
        { Id = id; Text = text; Feedback = feedback; }
    }

    public sealed class PracticeDecision
    {
        public string Id { get; }
        public string Prompt { get; }
        public int CheckpointIndex { get; }
        public int TargetIndex { get; }
        public string CorrectAnswerId { get; }
        public ReadOnlyCollection<PracticeAnswer> Answers { get; }
        internal PracticeDecision(string id, string prompt, int at, int target, string correct, params PracticeAnswer[] answers)
        { Id=id; Prompt=prompt; CheckpointIndex=at; TargetIndex=target; CorrectAnswerId=correct;
            Answers=new List<PracticeAnswer>(answers).AsReadOnly(); }
    }

    public enum PracticeSubmissionStatus { Correct, Incorrect, StaleQuestion, UnknownAnswer, Advancing, Complete }
    public readonly struct PracticeSubmission
    {
        public PracticeSubmissionStatus Status { get; }
        public string Feedback { get; }
        public bool Accepted => Status == PracticeSubmissionStatus.Correct;
        internal PracticeSubmission(PracticeSubmissionStatus status, string feedback) { Status=status; Feedback=feedback; }
    }

    // Decisions authorize ranges of the original trace, never another execution engine.
    // No Unity, world objects, inventory, AI or reward dependencies.
    public sealed class BridgePracticeLesson
    {
        public RecursionTrace Trace { get; } = BridgeRecursionLesson.Create(3);
        public ReadOnlyCollection<PracticeDecision> Decisions { get; }
        private static PracticeAnswer A(string id, string text, string feedback) => new PracticeAnswer(id,text,feedback);
        public BridgePracticeLesson()
        {
            Decisions = new List<PracticeDecision> {
                new PracticeDecision("bridge.order.n3", "n = 3: the base-case check is false. What executes next?", 1,2,"place.n3",
                    A("place.n3","placeStone(3)","Correct. Place stone 3 BEFORE the recursive call. The stack is unchanged."),
                    A("call.n2","buildBridge(2)","Not yet. placeStone(3) comes before buildBridge(n - 1)."),
                    A("return.n3","Return from buildBridge(3)","n is not zero, so this call must place its stone and recurse first.")),
                new PracticeDecision("bridge.call.n2", "Stone 3 is placed. What is the next recursive call?",2,5,"call.n2",
                    A("call.n2","buildBridge(2); the caller keeps n = 3", "Correct. A new frame owns n = 2; its caller still owns n = 3."),
                    A("call.n3","buildBridge(3)","The argument is n - 1. Repeating 3 would not reduce this problem."),
                    A("mutate.caller","Change the caller's n to 2", "A new call receives 2. The existing caller keeps its own n = 3.")),
                new PracticeDecision("bridge.order.n2", "n = 2: choose the execution order.",5,9,"place.n2.then.call.n1",
                    A("place.n2.then.call.n1","placeStone(2), then buildBridge(1)","Correct. Place stone 2, then enter a new frame with n = 1."),
                    A("call.n1.then.place.n2","buildBridge(1), then placeStone(2)","The statements in this function place the stone BEFORE recursing."),
                    A("return.n2","Return immediately", "The base case is false for n = 2. There is still work to execute.")),
                new PracticeDecision("bridge.order.n1", "n = 1: what happens before the base case?",9,12,"place.n1.then.call.n0",
                    A("place.n1.then.call.n0","placeStone(1), then buildBridge(0)","Correct. This is the third and final placement, followed by the call with n = 0."),
                    A("stop.n1","Stop at n = 1", "The stopping condition is n == 0, not n == 1."),
                    A("place.n0","Place a fourth stone with n = 0", "Only positive n reaches placeStone. The zero call will return immediately.")),
                new PracticeDecision("bridge.base.n0", "buildBridge(0) has entered. What does the condition require?",12,15,"base.return",
                    A("base.return","Return; no placement and no further call", "Correct. Reuse the existing zero frame, return from it, then resume its caller."),
                    A("call.negative","Call buildBridge(-1)", "n == 0 is true. return stops execution before the recursive call."),
                    A("place.zero","Run placeStone(0)", "The base-case return appears before placeStone, so no fourth stone is placed.")),
                new PracticeDecision("bridge.unwind", "buildBridge(1) resumes after its child returned. What remains?",15,21,"return.callers",
                    A("return.callers","Return from 1, then 2, then 3; leave an empty stack", "Correct. No statements remain after the recursive call. The void function returns no numeric result."),
                    A("place.again","Place stones again while returning", "Each placement already happened before its recursive call. Do not repeat it."),
                    A("return.number","Return a numeric result of 3", "This function is void. It unwinds its frames without returning a numeric value."))
            }.AsReadOnly();
            // Fail clearly if the underlying lesson changes; do not silently teach another trace.
            var phases=new[]{RecursionPhase.Check,RecursionPhase.Action,RecursionPhase.Check,
                RecursionPhase.Check,RecursionPhase.Enter,RecursionPhase.Resume};
            var parameters=new[]{3,3,2,1,0,1};
            if(Trace.Steps.Count!=22) throw new InvalidOperationException("Expected the tested 22-checkpoint bridge trace.");
            for(int i=0;i<Decisions.Count;i++)
                if(Trace.Steps[Decisions[i].CheckpointIndex].Phase!=phases[i] ||
                    Trace.Steps[Decisions[i].CheckpointIndex].Parameter!=parameters[i])
                    throw new InvalidOperationException("Practice gate no longer matches the source trace.");
        }
    }

    public sealed class BridgePracticeSession
    {
        public BridgePracticeLesson Lesson { get; }
        private readonly RecursionPlayback playback;
        private int decisionIndex, targetIndex;
        public int Index => playback.Index;
        public RecursionStep Current => playback.Current;
        public bool IsAdvancing { get; private set; }
        public PracticeDecision CurrentDecision => decisionIndex < Lesson.Decisions.Count ? Lesson.Decisions[decisionIndex] : null;
        public bool IsComplete => decisionIndex==Lesson.Decisions.Count && !IsAdvancing &&
            Index==Lesson.Trace.Steps.Count-1 && Current.Phase==RecursionPhase.Complete && Current.Frames.Count==0;

        public BridgePracticeSession(BridgePracticeLesson lesson = null)
        {
            Lesson=lesson ?? new BridgePracticeLesson();
            playback=new RecursionPlayback(Lesson.Trace);
            // Enter(3) -> Check(3), before the first meaningful decision. No action occurs.
            playback.Next();
        }
        public PracticeSubmission Submit(string questionId, string answerId)
        {
            if(IsComplete)return new PracticeSubmission(PracticeSubmissionStatus.Complete,"Practice is already complete.");
            if(IsAdvancing)return new PracticeSubmission(PracticeSubmissionStatus.Advancing,"The accepted decision is still being presented.");
            var q=CurrentDecision;
            if(q==null || q.Id!=questionId || Index!=q.CheckpointIndex)
                return new PracticeSubmission(PracticeSubmissionStatus.StaleQuestion,"This question is no longer active.");
            PracticeAnswer answer=null;
            foreach(var candidate in q.Answers)if(candidate.Id==answerId){answer=candidate;break;}
            if(answer==null)return new PracticeSubmission(PracticeSubmissionStatus.UnknownAnswer,"Choose an answer from the current question.");
            if(answer.Id!=q.CorrectAnswerId)return new PracticeSubmission(PracticeSubmissionStatus.Incorrect,answer.Feedback);
            targetIndex=q.TargetIndex; IsAdvancing=true;
            return new PracticeSubmission(PracticeSubmissionStatus.Correct,answer.Feedback);
        }
        // Future presenter consumes one original snapshot per call. No arbitrary jump API.
        public bool TryAdvanceApprovedStep()
        {
            if(!IsAdvancing)return false;
            playback.Next();
            if(Index==targetIndex){IsAdvancing=false;decisionIndex++;}
            return true;
        }
    }
}
