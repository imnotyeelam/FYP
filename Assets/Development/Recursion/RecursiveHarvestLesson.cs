using System;
using System.Collections.Generic;

namespace Fyp.Development.Recursion
{
    public enum HarvestPhase { Call, Base, Return }

    public sealed class HarvestStep
    {
        public int Row { get; }
        public int N { get; }
        public int Depth { get; }
        public HarvestPhase Phase { get; }
        public bool HasCrop { get; }

        public HarvestStep(int row, int n, int depth, HarvestPhase phase, bool crop = false)
        {
            Row = row;
            N = n;
            Depth = depth;
            Phase = phase;
            HasCrop = crop;
        }
    }

    public static class RecursiveHarvestLesson
    {
        public static IEnumerable<HarvestStep> Create(bool[,] occupancy)
        {
            if (occupancy == null || occupancy.GetLength(0) != 3 || occupancy.GetLength(1) != 10)
                throw new ArgumentException("Harvesting requires three rows of ten fixed plots.");
            var snapshot = (bool[,])occupancy.Clone();
            for (int row = 1; row <= 3; row++)
                foreach (var step in Harvest(row, 10, 1, snapshot)) yield return step;
        }

        static IEnumerable<HarvestStep> Harvest(int row, int n, int depth, bool[,] occupancy)
        {
            yield return new HarvestStep(row, n, depth, HarvestPhase.Call);
            if (n == 0)
            {
                yield return new HarvestStep(row, 0, depth, HarvestPhase.Base);
                yield break;
            }
            foreach (var step in Harvest(row, n - 1, depth + 1, occupancy)) yield return step;
            yield return new HarvestStep(row, n, depth, HarvestPhase.Return, occupancy[row - 1, n - 1]);
        }
    }
}
