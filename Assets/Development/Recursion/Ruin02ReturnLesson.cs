using System;
using System.Collections.Generic;

namespace Fyp.Development.Recursion
{
    // Pure teaching snapshots. No reference to the Ruin 2 world or challenge state.
    public static class Ruin02ReturnLesson
    {
        public static RecursionTrace Create()
        {
            var steps = new List<RecursionStep>();
            var stack = new List<int>();
            int restored = 0;
            Action<RecursionPhase, int?, int, string> add = (phase,n,line,why) =>
                steps.Add(new RecursionStep(phase,n,line,stack,restored,why));

            for (int n=4; n>=0; n--)
            {
                stack.Add(n);
                add(RecursionPhase.Enter,n,0,$"Call sealMagic({n}). This call has its own n; older calls wait on the stack.");
                if (n==0)
                {
                    add(RecursionPhase.BaseCase,0,1,"Base case: n is 0. Return now; do not absorb any magic.");
                    break;
                }
                add(RecursionPhase.Check,n,1,$"n is {n}, so the base case is false.");
                add(RecursionPhase.Call,n,5,$"Call sealMagic({n-1}) first. absorbMagic({n}) must wait until it returns.");
            }

            add(RecursionPhase.Return,0,2,"sealMagic(0) returns. Its frame stays visible until the next step pops it.");
            stack.RemoveAt(stack.Count-1);
            for (int n=1; n<=4; n++)
            {
                add(RecursionPhase.Resume,n,6,$"Back in sealMagic({n}) after sealMagic({n-1}) finished.");
                restored++;
                string staff = new[]{"Air","Earth","Fire","Ice"}[n-1];
                add(RecursionPhase.Action,n,6,$"Now absorbMagic({n}) restores the {staff} staff. This is return flow.");
                add(RecursionPhase.Return,n,7,$"sealMagic({n}) is finished. Its frame pops on the next step.");
                stack.RemoveAt(stack.Count-1);
            }
            add(RecursionPhase.Complete,null,-1,"All calls returned. The stack is empty; four staffs are restored in order 1, 2, 3, 4.");
            return new RecursionTrace("sealMagic",new[]{
                "void sealMagic(int n) {",
                "    if (n == 0) {",
                "        return;",
                "    }",
                "",
                "    sealMagic(n - 1);",
                "    absorbMagic(n);",
                "}"
            },steps,"absorbMagic");
        }
    }
}
