using System;
using System.Collections.Generic;

namespace Fyp.Development.Recursion
{
    /// <summary>
    /// Pure teaching snapshots for the Magical Farm Simulation Guide (Phase D): gather(4) over four
    /// plots with Plot 2 empty. Similar to, but intentionally different from, the real harvest
    /// challenge so it does not reveal the challenge answers. No Unity world, farm or reward references.
    /// </summary>
    public sealed class RecursiveGatherGuideLesson
    {
        public const int PlotCount = 4;
        public static readonly bool[] Plants = { true, false, true, true }; // plots 1..4; Plot 2 is empty

        public RecursionTrace Trace { get; }
        /// <summary>Per checkpoint: bit (k-1) set when plot k has been checked.</summary>
        public IReadOnlyList<int> Checked { get; }
        /// <summary>Per checkpoint: bit (k-1) set when plot k's plant has been picked.</summary>
        public IReadOnlyList<int> Picked { get; }
        /// <summary>Per checkpoint: plot currently being examined on the return flow (0 = none).</summary>
        public IReadOnlyList<int> Focus { get; }

        // Code line indices (0-based) used for the active-line highlight.
        const int LineSignature = 0, LineBaseTest = 1, LineReturn = 2, LineCall = 4, LineCheck = 6, LinePick = 7, LineEnd = 8, LineEntry = 10;

        RecursiveGatherGuideLesson(RecursionTrace trace, List<int> check, List<int> pick, List<int> focus)
        { Trace = trace; Checked = check.AsReadOnly(); Picked = pick.AsReadOnly(); Focus = focus.AsReadOnly(); }

        public static RecursiveGatherGuideLesson Create()
        {
            var steps = new List<RecursionStep>();
            var checkedMask = new List<int>(); var pickedMask = new List<int>(); var focus = new List<int>();
            var stack = new List<int>();
            int picked = 0, checkedBits = 0, pickedBits = 0;
            Action<RecursionPhase, int?, int, int, string> add = (phase, k, line, plot, why) =>
            {
                steps.Add(new RecursionStep(phase, k, line, stack, picked, why));
                checkedMask.Add(checkedBits); pickedMask.Add(pickedBits); focus.Add(plot);
            };

            add(RecursionPhase.Call, 4, LineEntry, 0, "The guide starts with gather(4). Nothing has been checked yet and the stack is empty.");
            for (int k = 4; k >= 0; k--)
            {
                stack.Add(k);
                add(RecursionPhase.Enter, k, LineSignature, 0, $"Enter gather({k}). This call has its own k = {k}; earlier calls wait on the stack.");
                if (k == 0)
                {
                    add(RecursionPhase.Check, 0, LineBaseTest, 0, "k is 0, so the base-case test is true.");
                    add(RecursionPhase.BaseCase, 0, LineReturn, 0, "Base case: return immediately. No plot is checked here.");
                    break;
                }
                add(RecursionPhase.Check, k, LineBaseTest, 0, $"k is {k}, not 0, so the base case is false.");
                add(RecursionPhase.Call, k, LineCall, 0, $"Call gather({k - 1}) first. Plot {k} must wait until that call returns.");
            }

            stack.RemoveAt(stack.Count - 1);
            for (int k = 1; k <= PlotCount; k++)
            {
                add(RecursionPhase.Resume, k, LineCall, 0, $"gather({k - 1}) has returned. Back in gather({k}), the line after the call runs now.");
                checkedBits |= 1 << (k - 1);
                if (Plants[k - 1])
                {
                    add(RecursionPhase.Check, k, LineCheck, k, $"Check plot {k}: containsPlant({k}) is true.");
                    picked++; pickedBits |= 1 << (k - 1);
                    add(RecursionPhase.Action, k, LinePick, k, $"pickPlant({k}) collects the plant on plot {k}. This happens on the way back.");
                }
                else
                {
                    add(RecursionPhase.Check, k, LineCheck, k, $"Check plot {k}: containsPlant({k}) is false. The empty plot is skipped, but it still counts as checked.");
                }
                add(RecursionPhase.Return, k, LineEnd, k, $"gather({k}) is finished. Its frame pops on the next step.");
                stack.RemoveAt(stack.Count - 1);
            }
            add(RecursionPhase.Complete, null, -1, 0, "All calls returned. 4 plots checked in order 1, 2, 3, 4; 3 plants picked; Plot 2 was skipped. The stack is empty.");

            var trace = new RecursionTrace("gather", new[]
            {
                "void gather(int k) {",
                "    if (k == 0)",
                "        return;",
                "",
                "    gather(k - 1);",
                "",
                "    if (containsPlant(k))",
                "        pickPlant(k);",
                "}",
                "",
                "gather(4);"
            }, steps, "pickPlant");
            return new RecursiveGatherGuideLesson(trace, checkedMask, pickedMask, focus);
        }
    }
}
