using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace Fyp.Development.Recursion
{
    public sealed class Ruin02PracticeQuestion
    {
        public string Id { get; }
        public string Prompt { get; }
        public string Expected { get; }
        public int At { get; }
        public int Through { get; }
        public string Hint { get; }
        public Ruin02PracticeQuestion(string id, string prompt, string expected, int at, int through, string hint)
        { Id=id; Prompt=prompt; Expected=expected; At=at; Through=through; Hint=hint; }
    }

    public enum Ruin02AnswerStatus { Correct, Incorrect, Busy, Stale, Complete }
    public readonly struct Ruin02AnswerResult
    {
        public Ruin02AnswerStatus Status { get; }
        public string Message { get; }
        public Ruin02AnswerResult(Ruin02AnswerStatus status,string message) { Status=status; Message=message; }
    }

    // The six gates authorize ranges in the tested 28-snapshot trace. They never execute C++.
    public sealed class Ruin02PracticeLesson
    {
        public RecursionTrace Trace { get; } = Ruin02ReturnLesson.Create();
        public ReadOnlyCollection<Ruin02PracticeQuestion> Questions { get; }
        public Ruin02PracticeLesson()
        {
            Questions=new List<Ruin02PracticeQuestion> {
                new Ruin02PracticeQuestion("ruin02.call.4","What recursive call executes next?","sealMagic(3)",1,13,"The recursive argument is n - 1. The caller keeps n = 4 while new frames descend."),
                new Ruin02PracticeQuestion("ruin02.base.0","At n = 0, what statement executes?","return",13,15,"The base case stops new calls before any absorbMagic operation."),
                new Ruin02PracticeQuestion("ruin02.absorb.1","Back in sealMagic(1): what executes now?","absorbMagic(1)",15,18,"After the smaller call returns, the waiting call uses its own n."),
                new Ruin02PracticeQuestion("ruin02.absorb.2","Back in sealMagic(2): what executes now?","absorbMagic(2)",18,21,"The stack unwinds from the most recent unfinished caller."),
                new Ruin02PracticeQuestion("ruin02.absorb.3","Back in sealMagic(3): what executes now?","absorbMagic(3)",21,24,"Continue with the caller that was waiting for sealMagic(2)."),
                new Ruin02PracticeQuestion("ruin02.absorb.4","Back in sealMagic(4): what executes now?","absorbMagic(4)",24,27,"The original caller resumes last, using its own n = 4.")
            }.AsReadOnly();
            if(Trace.Steps.Count!=28)throw new InvalidOperationException("Ruin 2 trace must have 28 checkpoints.");
            var phases=new[]{RecursionPhase.Check,RecursionPhase.BaseCase,RecursionPhase.Resume,RecursionPhase.Resume,RecursionPhase.Resume,RecursionPhase.Resume};
            var ns=new[]{4,0,1,2,3,4};
            for(int i=0;i<Questions.Count;i++)
            {
                var q=Questions[i];var current=Trace.Steps[q.At];
                if(current.Phase!=phases[i] || current.Parameter!=ns[i] || q.Through<=q.At || q.Through>=Trace.Steps.Count)
                    throw new InvalidOperationException("Ruin 2 gate no longer matches the source trace: "+q.Id);
            }
            if(Trace.Steps[27].Phase!=RecursionPhase.Complete || Trace.Steps[27].Frames.Count!=0)
                throw new InvalidOperationException("Ruin 2 completion snapshot must have an empty stack.");
        }

        public static bool Matches(string input,string expected)
        {
            if(string.IsNullOrWhiteSpace(input))return false;
            string normalized=Regex.Replace(input,@"\s+","");
            if(normalized.EndsWith(";",StringComparison.Ordinal))normalized=normalized.Substring(0,normalized.Length-1);
            return string.Equals(normalized,expected,StringComparison.OrdinalIgnoreCase);
        }

        // This is guidance only: Submit still owns the deterministic pass/fail decision.
        public static bool TryGetLocalInputHint(string input,string expected,out string hint)
        {
            hint=null;
            if(string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(expected))return false;
            string value=Regex.Replace(input,@"\s+","");
            if(value.EndsWith(";",StringComparison.Ordinal))value=value.Substring(0,value.Length-1);
            if(Matches(value,expected))return false;

            var expectedCall=Regex.Match(expected,@"^(?<name>[A-Za-z_]\w*)\((?<n>\d+)\)$");
            if(!expectedCall.Success)return false;
            string name=expectedCall.Groups["name"].Value;
            string currentN=expectedCall.Groups["n"].Value;
            var missingParens=Regex.Match(value,@"^(?<name>[A-Za-z_]\w*)(?<n>\d+)$");
            if(missingParens.Success)
            {
                hint="Function calls need parentheses around their argument.";
                return true;
            }
            var call=Regex.Match(value,@"^(?<name>[A-Za-z_]\w*)\((?<arg>[^)]*)\)$");
            if(call.Success)
            {
                if(!string.Equals(call.Groups["name"].Value,name,StringComparison.OrdinalIgnoreCase))
                {
                    hint="Check the complete function name in the highlighted statement.";
                    return true;
                }
                if(!string.Equals(call.Groups["arg"].Value,currentN,StringComparison.Ordinal))
                {
                    hint="Check the current n value in the highlighted stack frame.";
                    return true;
                }
            }
            if(value.StartsWith(name,StringComparison.OrdinalIgnoreCase))
            {
                hint="Check the call syntax: use the function name followed by one parenthesized argument.";
                return true;
            }
            return false;
        }
    }

    public sealed class Ruin02PracticeSession
    {
        public Ruin02PracticeLesson Lesson { get; }
        readonly RecursionPlayback playback;
        readonly HashSet<int> emitted=new HashSet<int>();
        int questionIndex,targetIndex;
        public int Index=>playback.Index;
        public RecursionStep Current=>playback.Current;
        public Ruin02PracticeQuestion Question=>questionIndex<Lesson.Questions.Count?Lesson.Questions[questionIndex]:null;
        public int QuestionNumber=>Math.Min(questionIndex+1,6);
        public bool Advancing { get; private set; }
        public bool Complete=>questionIndex==6 && !Advancing && Index==27 && Current.Phase==RecursionPhase.Complete && Current.Frames.Count==0 && emitted.Count==4;
        public event Action<int,RecursionStep> SnapshotPresented;
        public event Action<int> AbsorbMagic;
        public event Action ChallengeCompleted;

        public Ruin02PracticeSession(Ruin02PracticeLesson lesson=null)
        { Lesson=lesson??new Ruin02PracticeLesson();playback=new RecursionPlayback(Lesson.Trace);playback.JumpTo(1); }

        public Ruin02AnswerResult Submit(string questionId,string input)
        {
            if(Complete)return new Ruin02AnswerResult(Ruin02AnswerStatus.Complete,"The ritual is complete.");
            if(Advancing)return new Ruin02AnswerResult(Ruin02AnswerStatus.Busy,"Wait for this execution sequence to finish.");
            var q=Question;
            if(q==null || q.Id!=questionId || Index!=q.At)return new Ruin02AnswerResult(Ruin02AnswerStatus.Stale,"This question is no longer active.");
            if(!Ruin02PracticeLesson.Matches(input,q.Expected))return new Ruin02AnswerResult(Ruin02AnswerStatus.Incorrect,q.Hint);
            targetIndex=q.Through;Advancing=true;
            return new Ruin02AnswerResult(Ruin02AnswerStatus.Correct,"Correct. Watch the call stack and return order.");
        }

        public bool TryAdvanceApprovedStep()
        {
            if(!Advancing)return false;
            playback.Next();
            var step=Current;
            SnapshotPresented?.Invoke(Index,step);
            if(step.Phase==RecursionPhase.Action)
            {
                int n=step.Parameter??-1;
                if(n<1 || n>4 || !emitted.Add(n))throw new InvalidOperationException("Duplicate or invalid absorbMagic action.");
                AbsorbMagic?.Invoke(n);
            }
            if(Index==targetIndex)
            {
                Advancing=false;questionIndex++;
                if(Complete)ChallengeCompleted?.Invoke();
            }
            return true;
        }
    }
}
