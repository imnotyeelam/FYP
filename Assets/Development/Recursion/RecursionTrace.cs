using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Fyp.Development.Recursion
{
    public enum RecursionPhase { Enter, Check, Action, Call, BaseCase, Return, Resume, Complete }

    // Pure snapshots: playback never executes user code or mutates the world.
    public sealed class RecursionStep
    {
        public RecursionPhase Phase { get; }
        public int? Parameter { get; }
        public int ActiveLine { get; }
        public ReadOnlyCollection<int> Frames { get; }
        public int ActionCount { get; }
        public string Explanation { get; }

        public RecursionStep(RecursionPhase phase, int? parameter, int line,
            IEnumerable<int> frames, int actionCount, string explanation)
        {
            Phase = phase;
            Parameter = parameter;
            ActiveLine = line;
            Frames = new List<int>(frames).AsReadOnly();
            ActionCount = actionCount;
            Explanation = explanation;
        }
    }

    // Other lessons can supply their own code and snapshots to the same player/UI.
    public sealed class RecursionTrace
    {
        public string FunctionName { get; }
        public string ActionName { get; }
        public ReadOnlyCollection<string> Code { get; }
        public ReadOnlyCollection<RecursionStep> Steps { get; }

        public RecursionTrace(string name, IEnumerable<string> code, IEnumerable<RecursionStep> steps,
            string actionName = "World action")
        {
            FunctionName = name;
            ActionName = actionName;
            Code = new List<string>(code).AsReadOnly();
            Steps = new List<RecursionStep>(steps).AsReadOnly();
            if (Steps.Count == 0) throw new ArgumentException("A trace needs at least one step.");
        }
    }

    public sealed class RecursionPlayback
    {
        public RecursionTrace Trace { get; }
        public int Index { get; private set; }
        public RecursionStep Current => Trace.Steps[Index];
        public bool CanPrevious => Index > 0;
        public bool CanNext => Index < Trace.Steps.Count - 1;
        public RecursionPlayback(RecursionTrace trace) { Trace = trace; }
        public void Next() { if (CanNext) Index++; }
        public void Previous() { if (CanPrevious) Index--; }
        public void Replay() { Index = 0; }
        public void JumpTo(int index) { Index = Math.Max(0, Math.Min(Trace.Steps.Count - 1, index)); }
    }

    public static class BridgeRecursionLesson
    {
        public static RecursionTrace Create(int initial = 3)
        {
            if (initial < 0 || initial > 12) throw new ArgumentOutOfRangeException(nameof(initial));
            var steps = new List<RecursionStep>();
            var stack = new List<int>();
            int stones = 0;
            Action<RecursionPhase, int?, int, string> add = (phase, n, line, explanation) =>
                steps.Add(new RecursionStep(phase, n, line, stack, stones, explanation));

            for (int n = initial; n >= 0; n--)
            {
                stack.Add(n);
                add(RecursionPhase.Enter, n, 0,
                    $"Enter buildBridge({n}). Push a new frame with its own n = {n}." +
                    (stack.Count > 1 ? " Earlier callers remain on the stack, waiting." : ""));
                if (n == 0)
                {
                    add(RecursionPhase.BaseCase, n, 1,
                        "n == 0 is true: the base case stops further calls. No stone is placed and no new frame is created.");
                    break;
                }
                add(RecursionPhase.Check, n, 1, $"n == 0 is false for n = {n}. Continue to placeStone(n).");
                stones++;
                add(RecursionPhase.Action, n, 2,
                    $"Execute placeStone({n}) once. This happens BEFORE the recursive call. The stack does not change.");
                add(RecursionPhase.Call, n, 3,
                    $"Call buildBridge({n - 1}). This frame keeps n = {n}; the NEXT step enters a new frame with n = {n - 1}.");
            }

            for (int n = 0; n <= initial; n++)
            {
                add(RecursionPhase.Return, n, n == 0 ? 1 : 4,
                    n == 0 ? "Return from buildBridge(0). Its frame is shown until the NEXT step pops it." :
                    $"buildBridge({n}) reaches the end of this void function and returns. Its frame is popped on the NEXT step.");
                stack.RemoveAt(stack.Count - 1);
                if (stack.Count > 0)
                    add(RecursionPhase.Resume, stack[stack.Count - 1], 4,
                        $"Pop buildBridge({n}). Resume buildBridge({n + 1}) after its recursive call; no more statements remain. Do not place another stone.");
            }
            add(RecursionPhase.Complete, null, -1,
                $"Pop buildBridge({initial}). The initial call has returned: the stack is empty. Exactly {stones} placeStone calls occurred. No numeric value is returned by this void function.");
            return new RecursionTrace("buildBridge", new[] {
                "void buildBridge(int n) {", "    if (n == 0) return;",
                "    placeStone(n);", "    buildBridge(n - 1);", "}"
            }, steps, "placeStone");
        }
    }
}
