using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace AengelStudio.PlacementSystem
{
    public class PlacementMap : System.IDisposable
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        public NativeArray<bool> Blocked;
        public NativeArray<float> Inflation;

        public PlacementMap(int width, int height)
        {
            Width = width;
            Height = height;

            Blocked = new NativeArray<bool>(width * height, Allocator.Persistent);
            Inflation = new NativeArray<float>(width * height, Allocator.Persistent);

            for (int i = 0; i < Inflation.Length; i++)
                Inflation[i] = float.PositiveInfinity;
        }

        public int Index(int x, int y) => y * Width + x;

        public void SetBlocked(int x, int y, bool state)
        {
            if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) return;
            Blocked[Index(x, y)] = state;
            if (state) Inflation[Index(x, y)] = 0f;
        }

        public bool IsSpaceAvailable(int x, int y, float radius)
        {
            if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) return false;
            return Inflation[Index(x, y)] >= radius;
        }

        // ===============================================================
        // NEW: Finds closest available position for a given radius
        // ===============================================================
        public int2? GetClosestSpace(int x, int y, float radius, int maxSearch = 200)
        {
            if (IsSpaceAvailable(x, y, radius))
                return new int2(x, y);

            int bestX = -1, bestY = -1;
            float bestDist = float.MaxValue;

            for (int r = 1; r <= maxSearch; r++)
            {
                bool found = false;

                for (int dy = -r; dy <= r; dy++)
                {
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (dx * dx + dy * dy > r * r)
                            continue;

                        int nx = x + dx;
                        int ny = y + dy;

                        if ((uint)nx >= (uint)Width || (uint)ny >= (uint)Height)
                            continue;

                        if (Inflation[Index(nx, ny)] >= radius)
                        {
                            float dist = math.length(new float2(dx, dy));
                            if (dist < bestDist)
                            {
                                bestDist = dist;
                                bestX = nx;
                                bestY = ny;
                                found = true;
                            }
                        }
                    }
                }

                if (found)
                    return new int2(bestX, bestY);
            }

            return null; // nothing found
        }

        public void Dispose()
        {
            if (Blocked.IsCreated) Blocked.Dispose();
            if (Inflation.IsCreated) Inflation.Dispose();
        }
    }
}

