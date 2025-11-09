using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using Unity.Mathematics;
using Debug = UnityEngine.Debug;

namespace AengelStudio.PlacementSystem
{
    public class PlacementAPI : IDisposable
    {
        public readonly int Width;
        public readonly int Height;
        public readonly float InflationRadius;
        public readonly float InflationBudgetMs;

        private readonly PlacementMap map;
        private readonly InflationSystem inflation;

        private bool isInflating;
        private Action onInflationComplete;

        public bool IsInflating => isInflating;
        public bool HasPendingUpdates => inflation.HasPendingUpdates;
        public PlacementMap Map => map;

        public PlacementAPI(int width, int height, float inflationRadius, float inflationBudgetMs)
        {
            Width = width;
            Height = height;
            InflationRadius = inflationRadius;
            InflationBudgetMs = inflationBudgetMs;

            map = new PlacementMap(width, height);
            inflation = new InflationSystem(map);
        }

        /// <summary>
        /// Adds an obstacle and starts inflation asynchronously using a coroutine host.
        /// </summary>
        public void AddObstacle(int x, int y, int radius, MonoBehaviour coroutineHost, Action onComplete = null)
        {
            if (coroutineHost == null)
                throw new ArgumentNullException(nameof(coroutineHost), "Coroutine host is required for async inflation.");

            inflation.AddBlockedZone(x, y, radius);
            onInflationComplete = onComplete;

            if (!isInflating)
            {
                isInflating = true;
                coroutineHost.StartCoroutine(RunInflationCoroutine(coroutineHost));
            }
        }

        /// <summary>
        /// Returns true if the given placement is valid.
        /// </summary>
        public bool CanPlaceObject(int x, int y, float radius)
        {
            if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
                return false;

            if (radius > InflationRadius)
            {
                Debug.LogError($"[PlacementAPI] Placement radius {radius:F2} exceeds inflation radius {InflationRadius:F2} — accuracy not guaranteed.");
            }

            return map.IsSpaceAvailable(x, y, radius);
        }

        /// <summary>
        /// Returns the nearest available placement location.
        /// </summary>
        public int2? FindClosestAvailablePosition(int x, int y, float radius)
        {
            if (radius > InflationRadius)
            {
                Debug.LogError($"[PlacementAPI] Radius {radius:F2} exceeds inflation radius {InflationRadius:F2} — results may be inaccurate.");
            }

            return map.GetClosestSpace(x, y, radius);
        }

        /// <summary>
        /// Completes inflation synchronously.
        /// </summary>
        public void CompleteInflationNow()
        {
            while (inflation.HasPendingUpdates)
                inflation.StepInflationWithBudget(10f, InflationRadius);

            isInflating = false;
            onInflationComplete?.Invoke();
            onInflationComplete = null;
        }

        private IEnumerator RunInflationCoroutine(MonoBehaviour host)
        {
            var stopwatch = new Stopwatch();
            while (inflation.HasPendingUpdates)
            {
                stopwatch.Restart();
                inflation.StepInflationWithBudget(InflationBudgetMs, InflationRadius);
                stopwatch.Stop();

                if (!inflation.HasPendingUpdates)
                {
                    isInflating = false;
                    onInflationComplete?.Invoke();
                    onInflationComplete = null;
                    yield break;
                }

                yield return null;
            }

            isInflating = false;
            onInflationComplete?.Invoke();
            onInflationComplete = null;
        }

        public void Dispose()
        {
            inflation.Dispose();
            map.Dispose();
        }
    }
}

