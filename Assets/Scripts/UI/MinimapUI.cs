using UnityEngine;
using UnityEngine.UI;

namespace ReactorTechnician
{
    [RequireComponent(typeof(RawImage))]
    public sealed class MinimapUI : MonoBehaviour
    {
        [SerializeField] private Transform trackedPlayer;
        [SerializeField] private Transform generatedLayoutRoot;
        [SerializeField] private int textureSize = 192;
        [SerializeField] private float refreshInterval = 0.2f;
        [SerializeField] private Color backgroundColor = new Color(0.02f, 0.04f, 0.05f, 0.86f);
        [SerializeField] private Color floorColor = new Color(0.16f, 0.22f, 0.27f, 1f);
        [SerializeField] private Color storageColor = new Color(0.1f, 0.45f, 0.85f, 1f);
        [SerializeField] private Color reactorColor = new Color(0.2f, 0.95f, 0.55f, 1f);
        [SerializeField] private Color dangerColor = new Color(0.95f, 0.22f, 0.16f, 1f);
        [SerializeField] private Color terminalColor = new Color(0.75f, 0.45f, 1f, 1f);
        [SerializeField] private Color valveColor = new Color(1f, 0.82f, 0.16f, 1f);
        [SerializeField] private Color moduleColor = new Color(0.1f, 0.8f, 1f, 1f);
        [SerializeField] private Color socketColor = new Color(0.85f, 1f, 0.55f, 1f);
        [SerializeField] private Color playerColor = Color.white;
        [SerializeField] private Color wallColor = new Color(0.01f, 0.012f, 0.015f, 1f);

        private RawImage image;
        private Texture2D texture;
        private float timer;
        private Bounds mapBounds;

        private void Awake()
        {
            image = GetComponent<RawImage>();
            texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            image.texture = texture;

            if (trackedPlayer == null)
            {
                PlayerMovementController player = FindAnyObjectByType<PlayerMovementController>();
                if (player != null)
                {
                    trackedPlayer = player.transform;
                }
            }
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f)
            {
                return;
            }

            timer = refreshInterval;
            RefreshMap();
        }

        public void RefreshMap()
        {
            ResolveLayoutRoot();
            Clear();
            CalculateBounds();
            DrawFloors();
            DrawWalls();
            DrawMarkers();
            texture.Apply(false);
        }

        private void ResolveLayoutRoot()
        {
            if (generatedLayoutRoot != null)
            {
                return;
            }

            GameObject root = GameObject.Find("WFC Generated Reactor Layout");
            if (root != null)
            {
                generatedLayoutRoot = root.transform;
            }
        }

        private void Clear()
        {
            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    texture.SetPixel(x, y, backgroundColor);
                }
            }
        }

        private void CalculateBounds()
        {
            mapBounds = new Bounds(Vector3.zero, new Vector3(42f, 1f, 42f));
            if (generatedLayoutRoot == null)
            {
                return;
            }

            bool initialized = false;
            for (int i = 0; i < generatedLayoutRoot.childCount; i++)
            {
                Transform child = generatedLayoutRoot.GetChild(i);
                if (!child.name.StartsWith("WFC Floor"))
                {
                    continue;
                }

                if (!initialized)
                {
                    mapBounds = new Bounds(child.position, Vector3.one);
                    initialized = true;
                }
                else
                {
                    mapBounds.Encapsulate(child.position);
                }
            }

            mapBounds.Expand(8f);
        }

        private void DrawFloors()
        {
            if (generatedLayoutRoot == null)
            {
                return;
            }

            for (int i = 0; i < generatedLayoutRoot.childCount; i++)
            {
                Transform child = generatedLayoutRoot.GetChild(i);
                if (!child.name.StartsWith("WFC Floor"))
                {
                    continue;
                }

                Color color = floorColor;
                if (child.name.Contains("ModuleStorage")) color = storageColor;
                else if (child.name.Contains("ReactorHall")) color = reactorColor;
                else if (child.name.Contains("OverheatRoom")) color = dangerColor;
                else if (child.name.Contains("TerminalRoom")) color = terminalColor;
                else if (child.name.Contains("ValveRoom")) color = valveColor;

                DrawWorldRect(child.position, 5.8f, color);
            }
        }

        private void DrawMarkers()
        {
            DrawComponentMarkers(FindObjectsByType<CoolingModuleInteractable>(), moduleColor, 3);
            DrawComponentMarkers(FindObjectsByType<CoolingModuleSocket>(), socketColor, 3);
            DrawComponentMarkers(FindObjectsByType<ValveInteractable>(), valveColor, 3);
            DrawComponentMarkers(FindObjectsByType<AccessTerminalInteractable>(), terminalColor, 3);

            if (trackedPlayer != null)
            {
                DrawWorldCircle(trackedPlayer.position, 5, playerColor);
            }
        }

        private void DrawWalls()
        {
            if (generatedLayoutRoot == null)
            {
                return;
            }

            for (int i = 0; i < generatedLayoutRoot.childCount; i++)
            {
                Transform child = generatedLayoutRoot.GetChild(i);
                if (!child.name.StartsWith("WFC Wall"))
                {
                    continue;
                }

                Vector3 half = child.right * (child.localScale.x * 0.5f);
                DrawWorldSegment(child.position - half, child.position + half, 2, wallColor);
            }
        }

        private void DrawComponentMarkers<T>(T[] components, Color color, int radius) where T : Component
        {
            if (components == null)
            {
                return;
            }

            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] != null && components[i].gameObject.activeInHierarchy)
                {
                    DrawWorldCircle(components[i].transform.position, radius, color);
                }
            }
        }

        private void DrawWorldRect(Vector3 world, float size, Color color)
        {
            Vector2Int center = WorldToPixel(world);
            int half = Mathf.Max(1, Mathf.RoundToInt(size / Mathf.Max(mapBounds.size.x, mapBounds.size.z) * textureSize));
            for (int y = -half; y <= half; y++)
            {
                for (int x = -half; x <= half; x++)
                {
                    SetPixel(center.x + x, center.y + y, color);
                }
            }
        }

        private void DrawWorldCircle(Vector3 world, int radius, Color color)
        {
            Vector2Int center = WorldToPixel(world);
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y <= radius * radius)
                    {
                        SetPixel(center.x + x, center.y + y, color);
                    }
                }
            }
        }

        private void DrawWorldSegment(Vector3 start, Vector3 end, int radius, Color color)
        {
            Vector2Int a = WorldToPixel(start);
            Vector2Int b = WorldToPixel(end);
            int steps = Mathf.Max(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y), 1);
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(a.x, b.x, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(a.y, b.y, t));
                for (int oy = -radius; oy <= radius; oy++)
                {
                    for (int ox = -radius; ox <= radius; ox++)
                    {
                        SetPixel(x + ox, y + oy, color);
                    }
                }
            }
        }

        private Vector2Int WorldToPixel(Vector3 world)
        {
            float u = Mathf.InverseLerp(mapBounds.min.x, mapBounds.max.x, world.x);
            float v = Mathf.InverseLerp(mapBounds.min.z, mapBounds.max.z, world.z);
            return new Vector2Int(
                Mathf.Clamp(Mathf.RoundToInt(u * (textureSize - 1)), 0, textureSize - 1),
                Mathf.Clamp(Mathf.RoundToInt(v * (textureSize - 1)), 0, textureSize - 1));
        }

        private void SetPixel(int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= textureSize || y >= textureSize)
            {
                return;
            }

            texture.SetPixel(x, y, color);
        }
    }
}
