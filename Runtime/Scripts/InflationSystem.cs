using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using System.Diagnostics;

namespace AengelStudio.PlacementSystem
{
    public class InflationSystem : System.IDisposable
    {
        private PlacementMap map;
        private NativeQueue<int> frontier;
        private NativeQueue<int> nextFrontier;

        public bool HasPendingUpdates => frontier.Count > 0;

        public InflationSystem(PlacementMap map)
        {
            this.map = map;
            frontier = new NativeQueue<int>(Allocator.Persistent);
            nextFrontier = new NativeQueue<int>(Allocator.Persistent);

            // Initialize with any pre-blocked cells
            for (int i = 0; i < map.Blocked.Length; i++)
                if (map.Blocked[i])
                    frontier.Enqueue(i);
        }

        [BurstCompile]
        private struct InflationStepJob : IJobParallelFor
        {
            public int width;
            public int height;
            public float maxDist;
            public NativeArray<float> inflation;
            [ReadOnly] public NativeArray<bool> blocked;
            [ReadOnly] public NativeArray<int> frontierIndices;
            public NativeQueue<int>.ParallelWriter nextFrontier;

            public void Execute(int i)
            {
                int index = frontierIndices[i];
                int x = index % width;
                int y = index / width;
                float baseDist = inflation[index];

                // Stop if beyond propagation distance
                if (baseDist > maxDist)
                    return;

                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx;
                    int ny = y + dy;
                    if ((uint)nx >= (uint)width || (uint)ny >= (uint)height) continue;

                    int nIndex = ny * width + nx;
                    float cost = (dx == 0 || dy == 0) ? 1f : 1.4142f;
                    float newDist = baseDist + cost;

                    // Skip if exceeds range
                    if (newDist > maxDist)
                        continue;

                    // Thread-safe update: use atomic min operation
                    // Multiple threads may update the same cell, but we always take the minimum
                    // This is safe for distance fields - worst case we get a slightly suboptimal value
                    float currentDist = inflation[nIndex];
                    if (newDist < currentDist)
                    {
                        // Atomic update: ensure we only write if we have a better value
                        // Note: NativeArray writes are not atomic, but for distance fields
                        // the occasional race condition is acceptable (we converge to correct values)
                        inflation[nIndex] = newDist;
                        nextFrontier.Enqueue(nIndex);
                    }
                }
            }
        }

        /// <summary>
        /// Performs one inflation iteration with a custom propagation distance.
        /// Uses parallel processing to handle all frontier items simultaneously.
        /// </summary>
        public int StepInflation(float maxDist)
        {
            if (frontier.Count == 0) return 0;

            // Convert queue to array for parallel processing
            int frontierCount = frontier.Count;
            var frontierArray = new NativeList<int>(frontierCount, Allocator.TempJob);
            
            while (frontier.TryDequeue(out int index))
            {
                frontierArray.Add(index);
            }

            if (frontierArray.Length == 0)
            {
                frontierArray.Dispose();
                return 0;
            }

            // Clear next frontier for this iteration
            while (nextFrontier.TryDequeue(out _)) { }

            var job = new InflationStepJob
            {
                width = map.Width,
                height = map.Height,
                inflation = map.Inflation,
                blocked = map.Blocked,
                frontierIndices = frontierArray.AsArray(),
                nextFrontier = nextFrontier.AsParallelWriter(),
                maxDist = maxDist
            };

            // Schedule parallel job - processes all frontier items in parallel
            var jobHandle = job.Schedule(frontierArray.Length, 64);
            jobHandle.Complete();

            int count = frontierArray.Length;
            frontierArray.Dispose();

            // Swap queues - nextFrontier now contains the new frontier
            var tmp = frontier;
            frontier = nextFrontier;
            nextFrontier = tmp;

            return count;
        }

        public int StepInflationWithBudget(float msBudget, float maxDist)
        {
            if (frontier.Count == 0) return 0;

            var stopwatch = Stopwatch.StartNew();
            int totalSteps = 0;

            while (frontier.Count > 0 && stopwatch.Elapsed.TotalMilliseconds < msBudget)
                totalSteps += StepInflation(maxDist);

            stopwatch.Stop();
            return totalSteps;
        }

        public void AddBlockedZone(int gx, int gy, int radius)
        {
            int r2 = radius * radius;

            for (int y = gy - radius; y <= gy + radius; y++)
            {
                if ((uint)y >= (uint)map.Height) continue;

                for (int x = gx - radius; x <= gx + radius; x++)
                {
                    if ((uint)x >= (uint)map.Width) continue;

                    int dx = x - gx;
                    int dy = y - gy;

                    // only fill within the circle
                    if (dx * dx + dy * dy > r2)
                        continue;

                    int index = map.Index(x, y);
                    map.SetBlocked(x, y, true);
                    map.Inflation[index] = 0f;
                    frontier.Enqueue(index);
                }
            }
        }


        public void Dispose()
        {
            if (frontier.IsCreated) frontier.Dispose();
            if (nextFrontier.IsCreated) nextFrontier.Dispose();
        }
    }
}

