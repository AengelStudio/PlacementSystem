using UnityEngine;
using Unity.Mathematics;

namespace AengelStudio.PlacementSystem
{
    public class PlacementMapDebugger : MonoBehaviour
    {
        public int width = 256, height = 256;
        public float testRadius = 5f;
        public int blockRadius = 5;
        public float inflationRadius = 64f, inflationBudgetMs = 2f;
        public RenderTexture targetTexture;
        public RectTransform targetTextureRect;

        PlacementAPI api;
        PlacementMap map;
        Texture2D tex;
        Material blit;
        int2? lastClick, adjustedClick;

        void Start()
        {
            api = new PlacementAPI(width, height, inflationRadius, inflationBudgetMs);
            map = api.Map;

            tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            blit = new Material(Shader.Find("Unlit/Texture"));
        }

        void Update()
        {
            var p = GetMouseCell();

            if (Input.GetMouseButtonDown(1))
                api.AddObstacle(p.x, p.y, blockRadius, this);

            if (Input.GetMouseButtonDown(0))
            {
                bool ok = api.CanPlaceObject(p.x, p.y, testRadius);
                lastClick = p;
                adjustedClick = ok ? null : api.FindClosestAvailablePosition(p.x, p.y, testRadius);
            }

            Draw();
        }

        void OnInflationDone() => Debug.Log("Inflation completed.");

        int2 GetMouseCell()
        {
            if (targetTextureRect == null) return new int2(-1, -1);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(targetTextureRect, Input.mousePosition, null, out var lp);
            float2 uv = new float2(
                (lp.x + targetTextureRect.rect.width * 0.5f) / targetTextureRect.rect.width,
                (lp.y + targetTextureRect.rect.height * 0.5f) / targetTextureRect.rect.height
            );

            return new int2(Mathf.FloorToInt(uv.x * width), Mathf.FloorToInt(uv.y * height));
        }

        void Draw()
        {
            for (int i = 0; i < map.Blocked.Length; i++)
                tex.SetPixel(i % width, i / width, map.Blocked[i] ? new Color(0.4f, 0.4f, 0.4f) : Color.white);

            if (lastClick.HasValue) DrawCircle(lastClick.Value, (int)testRadius, Color.red);
            if (adjustedClick.HasValue) DrawCircle(adjustedClick.Value, (int)testRadius, Color.blue);

            tex.Apply();
            if (targetTexture) Graphics.Blit(tex, targetTexture, blit);
        }

        void DrawCircle(int2 c, int r, Color col)
        {
            int r2 = r * r;
            for (int y = c.y - r; y <= c.y + r; y++)
                for (int x = c.x - r; x <= c.x + r; x++)
                    if ((uint)x < width && (uint)y < height && (x - c.x) * (x - c.x) + (y - c.y) * (y - c.y) <= r2)
                        tex.SetPixel(x, y, col);
        }

        void OnDestroy()
        {
            api.Dispose();
            Destroy(tex);
            Destroy(blit);
        }
    }
}

