using System.Collections.Generic;
using UnityEngine;

namespace ReactorTechnician
{
    public sealed class WaveFunctionLevelGenerator : MonoBehaviour
    {
        private enum TileKind
        {
            Empty,
            CorridorNS,
            CorridorEW,
            CornerNE,
            CornerNW,
            CornerSE,
            CornerSW,
            Junction,
            Start,
            ReactorHall,
            ModuleStorage,
            ValveRoom,
            OverheatRoom,
            TerminalRoom,
            LockedDoor,
            DeadEndDecor
        }

        private sealed class TileDefinition
        {
            public readonly TileKind Kind;
            public readonly bool North;
            public readonly bool East;
            public readonly bool South;
            public readonly bool West;
            public readonly int Weight;

            public TileDefinition(TileKind kind, bool north, bool east, bool south, bool west, int weight)
            {
                Kind = kind;
                North = north;
                East = east;
                South = south;
                West = west;
                Weight = weight;
            }

            public bool HasConnection(int direction)
            {
                switch (direction)
                {
                    case 0: return North;
                    case 1: return East;
                    case 2: return South;
                    default: return West;
                }
            }
        }

        [SerializeField] private int gridSize = 7;
        [SerializeField] private float tileSize = 7f;
        [SerializeField] private int seed;
        [SerializeField] private bool randomizeSeed = true;
        [SerializeField] private int maxAttempts = 40;
        [SerializeField] private int difficultyTier = 1;
        [SerializeField] private bool generateOnStart = true;
        [SerializeField] private Transform playerStart;
        [SerializeField] private CoolingModuleInteractable[] modulePickups;
        [SerializeField] private CoolingModuleSocket[] moduleSockets;
        [SerializeField] private ValveInteractable[] routeValves;
        [SerializeField] private AccessTerminalInteractable[] accessTerminals;
        [SerializeField] private Material floorMaterial;
        [SerializeField] private Material wallMaterial;
        [SerializeField] private Material storageMaterial;
        [SerializeField] private Material reactorMaterial;
        [SerializeField] private Material dangerMaterial;
        [SerializeField] private Material terminalMaterial;
        [SerializeField] private GameObject wallPrefab;
        [SerializeField] private GameObject archPrefab;
        [SerializeField] private GameObject blockPrefab;
        [SerializeField] private GameObject platformPrefab;
        [SerializeField] private GameObject stepsPrefab;

        private readonly Dictionary<TileKind, TileDefinition> definitions = new Dictionary<TileKind, TileDefinition>();
        private readonly Dictionary<TileKind, Vector2Int> keyPositions = new Dictionary<TileKind, Vector2Int>();
        private TileKind[,] resolvedGrid;
        private Transform generatedRoot;
        private int resolvedSeed;
        private int generationCounter;

        public int DifficultyTier => difficultyTier;
        public int ResolvedSeed => resolvedSeed;
        public bool HasValidLayout { get; private set; }
        public bool UsedFallbackLayout { get; private set; }

        public bool ValidateCurrentLayout()
        {
            return resolvedGrid != null && ValidateRoute();
        }

        public bool ValidateCurrentPhysicalPassability()
        {
            return resolvedGrid != null && generatedRoot != null && ValidatePhysicalPassability();
        }

        private void Awake()
        {
            difficultyTier = Mathf.Clamp(PlayerPrefs.GetInt(LevelProgressManager.DifficultyTierPlayerPrefsKey, difficultyTier), 1, 4);
            BuildDefinitions();
            ConfigureKeyPositions();
        }

        private void Start()
        {
            if (generateOnStart)
            {
                Generate();
            }
        }

        public void SetDifficultyTier(int tier)
        {
            difficultyTier = Mathf.Clamp(tier, 1, 4);
        }

        public void Generate()
        {
            BuildDefinitions();
            ConfigureKeyPositions();
            DisableLegacyBlockers();
            Cleanup();

            generationCounter++;
            resolvedSeed = randomizeSeed ? System.Environment.TickCount + generationCounter * 7919 : seed + generationCounter;
            Random.InitState(resolvedSeed);

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                if (TryCollapse())
                {
                    PruneUnreachableTiles();
                    if (!ValidateRoute())
                    {
                        resolvedSeed++;
                        Random.InitState(resolvedSeed);
                        continue;
                    }

                    HasValidLayout = true;
                    UsedFallbackLayout = false;
                    BuildScene();
                    if (!ValidatePhysicalPassability())
                    {
                        Cleanup();
                        resolvedSeed++;
                        Random.InitState(resolvedSeed);
                        continue;
                    }

                    PlaceGameplayObjects();
                    RebuildPipeNetwork();
                    return;
                }

                resolvedSeed++;
                Random.InitState(resolvedSeed);
            }

            BuildConstrainedWfcLayout();
            HasValidLayout = ValidateRoute();
            UsedFallbackLayout = false;
            if (!HasValidLayout)
            {
                BuildFallbackLayout();
                HasValidLayout = true;
                UsedFallbackLayout = true;
            }

            BuildScene();
            if (!ValidatePhysicalPassability())
            {
                Cleanup();
                BuildGuaranteedOpenLayout();
                HasValidLayout = true;
                UsedFallbackLayout = true;
                BuildScene();
            }

            PlaceGameplayObjects();
            RebuildPipeNetwork();
        }

        public Vector3 GetGeneratedPoint(string key)
        {
            TileKind kind;
            switch (key)
            {
                case "Start":
                    kind = TileKind.Start;
                    break;
                case "ModuleStorage":
                    kind = TileKind.ModuleStorage;
                    break;
                case "ReactorHall":
                    kind = TileKind.ReactorHall;
                    break;
                case "ValveRoom":
                    kind = TileKind.ValveRoom;
                    break;
                case "OverheatRoom":
                    kind = TileKind.OverheatRoom;
                    break;
                case "TerminalRoom":
                    kind = TileKind.TerminalRoom;
                    break;
                default:
                    return transform.position;
            }

            return GetWorldPosition(kind);
        }

        private Vector3 GetWorldPosition(TileKind kind)
        {
            Vector2Int cell;
            if (!keyPositions.TryGetValue(kind, out cell))
            {
                return transform.position;
            }

            return CellToWorld(cell);
        }

        private void BuildDefinitions()
        {
            definitions.Clear();
            Add(TileKind.Empty, false, false, false, false, 3);
            Add(TileKind.CorridorNS, true, false, true, false, 7);
            Add(TileKind.CorridorEW, false, true, false, true, 7);
            Add(TileKind.CornerNE, true, true, false, false, 5);
            Add(TileKind.CornerNW, true, false, false, true, 5);
            Add(TileKind.CornerSE, false, true, true, false, 5);
            Add(TileKind.CornerSW, false, false, true, true, 5);
            Add(TileKind.Junction, true, true, true, true, 2 + difficultyTier);
            Add(TileKind.Start, true, true, false, true, 1);
            Add(TileKind.ReactorHall, true, true, true, true, 1);
            Add(TileKind.ModuleStorage, true, true, true, true, 1);
            Add(TileKind.ValveRoom, true, true, true, true, 1);
            Add(TileKind.OverheatRoom, true, true, true, true, 1);
            Add(TileKind.TerminalRoom, true, true, true, true, 1);
            Add(TileKind.LockedDoor, true, true, true, true, Mathf.Max(1, difficultyTier - 1));
            Add(TileKind.DeadEndDecor, true, false, false, false, 2 + difficultyTier);
        }

        private void Add(TileKind kind, bool north, bool east, bool south, bool west, int weight)
        {
            definitions[kind] = new TileDefinition(kind, north, east, south, west, Mathf.Max(1, weight));
        }

        private void ConfigureKeyPositions()
        {
            keyPositions.Clear();
            int center = gridSize / 2;
            keyPositions[TileKind.Start] = new Vector2Int(center, 0);
            keyPositions[TileKind.ModuleStorage] = new Vector2Int(1, 2);
            keyPositions[TileKind.ReactorHall] = new Vector2Int(center, center);
            keyPositions[TileKind.ValveRoom] = new Vector2Int(gridSize - 2, center);
            keyPositions[TileKind.OverheatRoom] = new Vector2Int(gridSize - 2, gridSize - 2);
            keyPositions[TileKind.TerminalRoom] = new Vector2Int(center, gridSize - 2);
        }

        private bool TryCollapse()
        {
            List<TileKind>[,] possible = new List<TileKind>[gridSize, gridSize];
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    possible[x, y] = GetAllowedTilesForCell(new Vector2Int(x, y));
                }
            }

            for (int guard = 0; guard < gridSize * gridSize * 4; guard++)
            {
                Vector2Int cell = FindLowestEntropyCell(possible);
                if (cell.x < 0)
                {
                    resolvedGrid = ToGrid(possible);
                    return ValidateRoute();
                }

                TileKind chosen = PickWeighted(possible[cell.x, cell.y]);
                possible[cell.x, cell.y].Clear();
                possible[cell.x, cell.y].Add(chosen);

                if (!Propagate(possible, cell))
                {
                    return false;
                }
            }

            return false;
        }

        private List<TileKind> GetAllowedTilesForCell(Vector2Int cell)
        {
            foreach (KeyValuePair<TileKind, Vector2Int> key in keyPositions)
            {
                if (key.Value == cell)
                {
                    return new List<TileKind> { key.Key };
                }
            }

            List<TileKind> tiles = new List<TileKind>
            {
                TileKind.Empty,
                TileKind.CorridorNS,
                TileKind.CorridorEW,
                TileKind.CornerNE,
                TileKind.CornerNW,
                TileKind.CornerSE,
                TileKind.CornerSW,
                TileKind.Junction,
                TileKind.DeadEndDecor
            };

            if (difficultyTier >= 2)
            {
                tiles.Add(TileKind.LockedDoor);
            }

            return tiles;
        }

        private Vector2Int FindLowestEntropyCell(List<TileKind>[,] possible)
        {
            Vector2Int best = new Vector2Int(-1, -1);
            int bestCount = int.MaxValue;

            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    int count = possible[x, y].Count;
                    if (count > 1 && count < bestCount)
                    {
                        best = new Vector2Int(x, y);
                        bestCount = count;
                    }
                }
            }

            return best;
        }

        private bool Propagate(List<TileKind>[,] possible, Vector2Int start)
        {
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                Vector2Int cell = queue.Dequeue();
                for (int direction = 0; direction < 4; direction++)
                {
                    Vector2Int neighbor = cell + DirectionOffset(direction);
                    if (!IsInside(neighbor))
                    {
                        continue;
                    }

                    int opposite = (direction + 2) % 4;
                    int before = possible[neighbor.x, neighbor.y].Count;
                    possible[neighbor.x, neighbor.y].RemoveAll(tile => !IsCompatible(possible[cell.x, cell.y], tile, direction, opposite));
                    if (possible[neighbor.x, neighbor.y].Count == 0)
                    {
                        return false;
                    }

                    if (possible[neighbor.x, neighbor.y].Count != before)
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return true;
        }

        private bool IsCompatible(List<TileKind> sourceOptions, TileKind neighborTile, int direction, int opposite)
        {
            TileDefinition neighbor = definitions[neighborTile];
            for (int i = 0; i < sourceOptions.Count; i++)
            {
                TileDefinition source = definitions[sourceOptions[i]];
                if (source.HasConnection(direction) == neighbor.HasConnection(opposite))
                {
                    return true;
                }
            }

            return false;
        }

        private TileKind PickWeighted(List<TileKind> tiles)
        {
            int total = 0;
            for (int i = 0; i < tiles.Count; i++)
            {
                total += definitions[tiles[i]].Weight;
            }

            int roll = Random.Range(0, total);
            for (int i = 0; i < tiles.Count; i++)
            {
                roll -= definitions[tiles[i]].Weight;
                if (roll < 0)
                {
                    return tiles[i];
                }
            }

            return tiles[0];
        }

        private TileKind[,] ToGrid(List<TileKind>[,] possible)
        {
            TileKind[,] grid = new TileKind[gridSize, gridSize];
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    grid[x, y] = possible[x, y][0];
                }
            }

            return grid;
        }

        private bool ValidateRoute()
        {
            return AllNonEmptyTilesReachableFromStart()
                && HasPath(TileKind.Start, TileKind.ModuleStorage)
                && HasPath(TileKind.ModuleStorage, TileKind.ReactorHall)
                && HasPath(TileKind.ReactorHall, TileKind.ValveRoom)
                && HasPath(TileKind.ValveRoom, TileKind.TerminalRoom)
                && HasPath(TileKind.ValveRoom, TileKind.OverheatRoom);
        }

        private bool HasPath(TileKind from, TileKind to)
        {
            Vector2Int start = keyPositions[from];
            Vector2Int target = keyPositions[to];
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                Vector2Int cell = queue.Dequeue();
                if (cell == target)
                {
                    return true;
                }

                TileDefinition current = definitions[resolvedGrid[cell.x, cell.y]];
                for (int direction = 0; direction < 4; direction++)
                {
                    if (!current.HasConnection(direction))
                    {
                        continue;
                    }

                    Vector2Int neighbor = cell + DirectionOffset(direction);
                    if (!IsInside(neighbor) || visited.Contains(neighbor))
                    {
                        continue;
                    }

                    TileDefinition neighborDefinition = definitions[resolvedGrid[neighbor.x, neighbor.y]];
                    if (neighborDefinition.HasConnection((direction + 2) % 4))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return false;
        }

        private void BuildFallbackLayout()
        {
            resolvedGrid = new TileKind[gridSize, gridSize];
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    resolvedGrid[x, y] = TileKind.Empty;
                }
            }

            foreach (KeyValuePair<TileKind, Vector2Int> key in keyPositions)
            {
                resolvedGrid[key.Value.x, key.Value.y] = key.Key;
            }

            CarvePath(TileKind.Start, TileKind.ModuleStorage);
            CarvePath(TileKind.ModuleStorage, TileKind.ReactorHall);
            CarvePath(TileKind.ReactorHall, TileKind.ValveRoom);
            CarvePath(TileKind.ValveRoom, TileKind.TerminalRoom);
            CarvePath(TileKind.ValveRoom, TileKind.OverheatRoom);
            AddRandomSideRooms();
            PruneUnreachableTiles();
        }

        private void BuildConstrainedWfcLayout()
        {
            resolvedGrid = new TileKind[gridSize, gridSize];
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    resolvedGrid[x, y] = TileKind.Empty;
                }
            }

            foreach (KeyValuePair<TileKind, Vector2Int> key in keyPositions)
            {
                resolvedGrid[key.Value.x, key.Value.y] = key.Key;
            }

            CarvePath(TileKind.Start, TileKind.ModuleStorage);
            CarvePath(TileKind.ModuleStorage, TileKind.ReactorHall);
            CarvePath(TileKind.ReactorHall, TileKind.ValveRoom);
            CarvePath(TileKind.ValveRoom, TileKind.TerminalRoom);
            CarvePath(TileKind.ValveRoom, TileKind.OverheatRoom);
            AddConnectedWfcBranches();
            NormalizePathTilesToOpenJunctions();
            PruneUnreachableTiles();
        }

        private void BuildGuaranteedOpenLayout()
        {
            resolvedGrid = new TileKind[gridSize, gridSize];
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    resolvedGrid[x, y] = TileKind.Empty;
                }
            }

            foreach (KeyValuePair<TileKind, Vector2Int> key in keyPositions)
            {
                resolvedGrid[key.Value.x, key.Value.y] = key.Key;
            }

            CarvePath(TileKind.Start, TileKind.ModuleStorage);
            CarvePath(TileKind.ModuleStorage, TileKind.ReactorHall);
            CarvePath(TileKind.ReactorHall, TileKind.ValveRoom);
            CarvePath(TileKind.ValveRoom, TileKind.TerminalRoom);
            CarvePath(TileKind.ValveRoom, TileKind.OverheatRoom);
            NormalizePathTilesToOpenJunctions();
        }

        private void CarvePath(TileKind from, TileKind to)
        {
            Vector2Int current = keyPositions[from];
            Vector2Int target = keyPositions[to];

            while (current != target)
            {
                if (resolvedGrid[current.x, current.y] == TileKind.Empty)
                {
                    resolvedGrid[current.x, current.y] = TileKind.Junction;
                }

                if (current.x != target.x)
                {
                    current.x += current.x < target.x ? 1 : -1;
                }
                else if (current.y != target.y)
                {
                    current.y += current.y < target.y ? 1 : -1;
                }
            }
        }

        private void AddRandomSideRooms()
        {
            float chance = Mathf.Clamp01(0.18f + difficultyTier * 0.08f);
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    float cellRoll = Hash01(resolvedSeed, x, y, 0);
                    if (resolvedGrid[x, y] != TileKind.Empty || cellRoll > chance)
                    {
                        continue;
                    }

                    Vector2Int cell = new Vector2Int(x, y);
                    if (!HasAdjacentNonEmpty(cell))
                    {
                        continue;
                    }

                    float roll = Hash01(resolvedSeed, x, y, 1);
                    if (difficultyTier >= 3 && roll > 0.78f)
                    {
                        resolvedGrid[x, y] = TileKind.LockedDoor;
                    }
                    else if (roll > 0.56f)
                    {
                        resolvedGrid[x, y] = TileKind.DeadEndDecor;
                    }
                    else
                    {
                        resolvedGrid[x, y] = Hash01(resolvedSeed, x, y, 2) > 0.5f ? TileKind.CorridorNS : TileKind.CorridorEW;
                    }
                }
            }
        }

        private void AddConnectedWfcBranches()
        {
            int branchBudget = Mathf.Clamp(4 + difficultyTier * 3, 5, gridSize * 2);
            for (int i = 0; i < branchBudget; i++)
            {
                List<Vector2Int> frontier = new List<Vector2Int>();
                for (int x = 0; x < gridSize; x++)
                {
                    for (int y = 0; y < gridSize; y++)
                    {
                        if (resolvedGrid[x, y] != TileKind.Empty)
                        {
                            AddEmptyNeighbors(new Vector2Int(x, y), frontier);
                        }
                    }
                }

                if (frontier.Count == 0)
                {
                    return;
                }

                Vector2Int cell = frontier[Random.Range(0, frontier.Count)];
                resolvedGrid[cell.x, cell.y] = PickConnectedRoomKind(cell);
            }
        }

        private void AddEmptyNeighbors(Vector2Int cell, List<Vector2Int> frontier)
        {
            for (int direction = 0; direction < 4; direction++)
            {
                Vector2Int neighbor = cell + DirectionOffset(direction);
                if (!IsInside(neighbor) || resolvedGrid[neighbor.x, neighbor.y] != TileKind.Empty || frontier.Contains(neighbor))
                {
                    continue;
                }

                frontier.Add(neighbor);
            }
        }

        private TileKind PickConnectedRoomKind(Vector2Int cell)
        {
            float roll = Hash01(resolvedSeed, cell.x, cell.y, generationCounter);
            if (difficultyTier >= 3 && roll > 0.86f)
            {
                return TileKind.LockedDoor;
            }

            if (roll > 0.72f)
            {
                return TileKind.DeadEndDecor;
            }

            return TileKind.Junction;
        }

        private void NormalizePathTilesToOpenJunctions()
        {
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    TileKind kind = resolvedGrid[x, y];
                    if (kind == TileKind.Empty || keyPositions.ContainsValue(new Vector2Int(x, y)))
                    {
                        continue;
                    }

                    resolvedGrid[x, y] = TileKind.Junction;
                }
            }
        }

        private static float Hash01(int baseSeed, int x, int y, int salt)
        {
            System.Random random = new System.Random(baseSeed + x * 92821 + y * 68917 + salt * 31337);
            return (float)random.NextDouble();
        }

        private bool HasAdjacentNonEmpty(Vector2Int cell)
        {
            for (int direction = 0; direction < 4; direction++)
            {
                Vector2Int neighbor = cell + DirectionOffset(direction);
                if (IsInside(neighbor) && resolvedGrid[neighbor.x, neighbor.y] != TileKind.Empty)
                {
                    return true;
                }
            }

            return false;
        }

        private void PruneUnreachableTiles()
        {
            Vector2Int start;
            if (!keyPositions.TryGetValue(TileKind.Start, out start) || resolvedGrid == null)
            {
                return;
            }

            HashSet<Vector2Int> reachable = new HashSet<Vector2Int>();
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            reachable.Add(start);
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                Vector2Int cell = queue.Dequeue();
                TileDefinition current = definitions[resolvedGrid[cell.x, cell.y]];
                for (int direction = 0; direction < 4; direction++)
                {
                    if (!current.HasConnection(direction))
                    {
                        continue;
                    }

                    Vector2Int neighbor = cell + DirectionOffset(direction);
                    if (!IsInside(neighbor) || reachable.Contains(neighbor))
                    {
                        continue;
                    }

                    TileDefinition neighborDefinition = definitions[resolvedGrid[neighbor.x, neighbor.y]];
                    if (neighborDefinition.HasConnection((direction + 2) % 4))
                    {
                        reachable.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    if (resolvedGrid[x, y] != TileKind.Empty && !reachable.Contains(cell))
                    {
                        resolvedGrid[x, y] = TileKind.Empty;
                    }
                }
            }
        }

        private bool AllNonEmptyTilesReachableFromStart()
        {
            Vector2Int start;
            if (!keyPositions.TryGetValue(TileKind.Start, out start) || resolvedGrid == null)
            {
                return false;
            }

            HashSet<Vector2Int> reachable = CollectReachableCells(start);
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    if (resolvedGrid[x, y] != TileKind.Empty && !reachable.Contains(new Vector2Int(x, y)))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private HashSet<Vector2Int> CollectReachableCells(Vector2Int start)
        {
            HashSet<Vector2Int> reachable = new HashSet<Vector2Int>();
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            reachable.Add(start);
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                Vector2Int cell = queue.Dequeue();
                TileDefinition current = definitions[resolvedGrid[cell.x, cell.y]];
                for (int direction = 0; direction < 4; direction++)
                {
                    if (!current.HasConnection(direction))
                    {
                        continue;
                    }

                    Vector2Int neighbor = cell + DirectionOffset(direction);
                    if (!IsInside(neighbor) || reachable.Contains(neighbor))
                    {
                        continue;
                    }

                    TileDefinition neighborDefinition = definitions[resolvedGrid[neighbor.x, neighbor.y]];
                    if (neighborDefinition.HasConnection((direction + 2) % 4))
                    {
                        reachable.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return reachable;
        }

        private void BuildScene()
        {
            GameObject rootObject = new GameObject("WFC Generated Reactor Layout");
            rootObject.transform.SetParent(transform);
            rootObject.transform.localPosition = Vector3.zero;
            generatedRoot = rootObject.transform;

            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    TileKind kind = resolvedGrid[x, y];
                    if (kind == TileKind.Empty)
                    {
                        continue;
                    }

                    Vector2Int cell = new Vector2Int(x, y);
                    CreateFloor(cell, kind);
                    CreateClosedSideWalls(cell, kind);
                    CreateRoomProp(cell, kind);
                }
            }
        }

        private void CreateFloor(Vector2Int cell, TileKind kind)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = $"WFC Floor - {kind} {cell.x},{cell.y}";
            floor.transform.SetParent(generatedRoot);
            floor.transform.position = CellToWorld(cell) + new Vector3(0f, -0.05f, 0f);
            floor.transform.localScale = new Vector3(tileSize, 0.1f, tileSize);
            SetMaterial(floor, GetTileMaterial(kind));
        }

        private void CreateClosedSideWalls(Vector2Int cell, TileKind kind)
        {
            TileDefinition definition = definitions[kind];
            for (int direction = 0; direction < 4; direction++)
            {
                Vector2Int neighbor = cell + DirectionOffset(direction);
                bool open = definition.HasConnection(direction) && IsInside(neighbor)
                    && definitions[resolvedGrid[neighbor.x, neighbor.y]].HasConnection((direction + 2) % 4);
                if (!open)
                {
                    CreateWall(cell, direction);
                }
                else if (direction < 2)
                {
                    CreateDoorArch(cell, direction);
                }
            }
        }

        private void CreateWall(Vector2Int cell, int direction)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = $"WFC Wall {cell.x},{cell.y} D{direction}";
            wall.transform.SetParent(generatedRoot);
            Vector3 center = CellToWorld(cell);
            bool northSouth = direction == 0 || direction == 2;
            float side = tileSize * 0.5f;
            Vector3 offset = DirectionVector(direction) * side;
            wall.transform.position = center + offset + Vector3.up * 1.2f;
            wall.transform.rotation = Quaternion.Euler(0f, northSouth ? 0f : 90f, 0f);
            wall.transform.localScale = new Vector3(tileSize, 2.4f, 0.22f);
            SetMaterial(wall, wallMaterial);

            if (wallPrefab != null)
            {
                GameObject visual = Instantiate(wallPrefab);
                visual.name = $"WFC Wall Visual {cell.x},{cell.y} D{direction}";
                visual.transform.SetParent(generatedRoot);
                visual.transform.position = wall.transform.position;
                visual.transform.rotation = wall.transform.rotation;
                visual.transform.localScale = Vector3.one;
                DisableColliders(visual);
            }
        }

        private void CreateDoorArch(Vector2Int cell, int direction)
        {
            GameObject arch = new GameObject($"WFC Solid Open Frame {cell.x},{cell.y} D{direction}");
            arch.transform.SetParent(generatedRoot);
            arch.transform.position = CellToWorld(cell) + DirectionVector(direction) * tileSize * 0.5f;
            arch.transform.rotation = Quaternion.Euler(0f, direction == 0 ? 0f : 90f, 0f);

            AddArchFramePart(arch.transform, "Left Post", new Vector3(-1.85f, 1.1f, 0f), new Vector3(0.42f, 2.2f, 0.42f));
            AddArchFramePart(arch.transform, "Right Post", new Vector3(1.85f, 1.1f, 0f), new Vector3(0.42f, 2.2f, 0.42f));
            AddArchFramePart(arch.transform, "Top Beam", new Vector3(0f, 2.25f, 0f), new Vector3(4.1f, 0.42f, 0.42f));
        }

        private void CreateRoomProp(Vector2Int cell, TileKind kind)
        {
            GameObject prefab = null;
            Material material = null;
            Vector3 scale = Vector3.one;

            if (kind == TileKind.ModuleStorage)
            {
                prefab = blockPrefab;
                material = storageMaterial;
                scale = new Vector3(1.4f, 0.7f, 1.4f);
            }
            else if (kind == TileKind.ValveRoom || kind == TileKind.TerminalRoom)
            {
                prefab = platformPrefab;
                material = terminalMaterial;
                scale = new Vector3(1.8f, 0.25f, 1.8f);
            }
            else if (kind == TileKind.OverheatRoom)
            {
                prefab = stepsPrefab;
                material = dangerMaterial;
                scale = new Vector3(1.6f, 0.5f, 1.6f);
            }
            else if (kind == TileKind.DeadEndDecor)
            {
                prefab = blockPrefab;
                material = wallMaterial;
                scale = new Vector3(1f, 0.8f, 1f);
            }

            if (prefab == null && material == null)
            {
                return;
            }

            GameObject prop = prefab == null ? GameObject.CreatePrimitive(PrimitiveType.Cube) : Instantiate(prefab);
            prop.name = $"WFC Prop - {kind}";
            prop.transform.SetParent(generatedRoot);
            prop.transform.position = CellToWorld(cell) + Vector3.up * 0.35f;
            prop.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            if (prefab == null)
            {
                prop.transform.localScale = scale;
            }
            SetMaterial(prop, material);
            DisableColliders(prop);
        }

        private void PlaceGameplayObjects()
        {
            Vector3 start = GetWorldPosition(TileKind.Start);
            if (playerStart != null)
            {
                playerStart.position = start + new Vector3(0f, 0.15f, -1.2f);
            }

            Vector3 storage = GetWorldPosition(TileKind.ModuleStorage);
            if (modulePickups == null || modulePickups.Length == 0)
            {
                modulePickups = FindObjectsByType<CoolingModuleInteractable>();
            }

            for (int i = 0; i < modulePickups.Length; i++)
            {
                if (modulePickups[i] != null)
                {
                    modulePickups[i].transform.position = storage + new Vector3(-1.4f + i * 1.2f, 0.55f, -1.4f);
                    modulePickups[i].transform.rotation = Quaternion.identity;
                }
            }

            Vector3 reactor = GetWorldPosition(TileKind.ReactorHall);
            if (moduleSockets == null || moduleSockets.Length == 0)
            {
                moduleSockets = FindObjectsByType<CoolingModuleSocket>();
            }

            for (int i = 0; i < moduleSockets.Length; i++)
            {
                if (moduleSockets[i] != null)
                {
                    moduleSockets[i].transform.position = reactor + new Vector3(-1.5f + i * 3f, 0.35f, 2.1f);
                }
            }

            Vector3 valveRoom = GetWorldPosition(TileKind.ValveRoom);
            if (routeValves == null || routeValves.Length == 0)
            {
                routeValves = FindObjectsByType<ValveInteractable>();
            }

            for (int i = 0; i < routeValves.Length; i++)
            {
                if (routeValves[i] != null)
                {
                    routeValves[i].transform.position = valveRoom + new Vector3(-2f + i * 1.8f, 0.75f, 1.2f);
                    EnsureInteractionTrigger(routeValves[i].gameObject, 1.45f);
                }
            }

            Vector3 terminalRoom = GetWorldPosition(TileKind.TerminalRoom);
            if (accessTerminals == null || accessTerminals.Length == 0)
            {
                accessTerminals = FindObjectsByType<AccessTerminalInteractable>();
            }

            for (int i = 0; i < accessTerminals.Length; i++)
            {
                if (accessTerminals[i] != null)
                {
                    accessTerminals[i].transform.position = terminalRoom + new Vector3(-1.2f + i * 2.4f, 0.75f, -1.2f);
                }
            }
        }

        private Material GetTileMaterial(TileKind kind)
        {
            if (kind == TileKind.ModuleStorage)
            {
                return storageMaterial == null ? floorMaterial : storageMaterial;
            }

            if (kind == TileKind.ReactorHall)
            {
                return reactorMaterial == null ? floorMaterial : reactorMaterial;
            }

            if (kind == TileKind.OverheatRoom)
            {
                return dangerMaterial == null ? floorMaterial : dangerMaterial;
            }

            if (kind == TileKind.TerminalRoom || kind == TileKind.ValveRoom)
            {
                return terminalMaterial == null ? floorMaterial : terminalMaterial;
            }

            return floorMaterial;
        }

        private void Cleanup()
        {
            Transform oldRoot = transform.Find("WFC Generated Reactor Layout");
            if (oldRoot != null)
            {
                DestroyImmediate(oldRoot.gameObject);
            }
        }

        private void DisableLegacyBlockers()
        {
            DisableNonInteractiveCollidersUnder("Reactor Station - Blockout");
            DisableNonInteractiveCollidersUnder("Dynamic Level Variation");
            DisableNonInteractiveCollidersUnder("Stage 10 Store Style Decorations");
        }

        private static void DisableNonInteractiveCollidersUnder(string rootName)
        {
            GameObject root = GameObject.Find(rootName);
            if (root == null)
            {
                return;
            }

            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].GetComponentInParent<InteractableObject>() == null)
                {
                    colliders[i].enabled = false;
                }
            }
        }

        private bool ValidatePhysicalPassability()
        {
            if (generatedRoot == null || resolvedGrid == null)
            {
                return false;
            }

            Vector3 start = GetWorldPosition(TileKind.Start) + Vector3.up * 1.0f;
            if (HasBlockingColliderAt(start, new Vector3(0.45f, 0.85f, 0.45f)))
            {
                return false;
            }

            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    if (resolvedGrid[x, y] == TileKind.Empty)
                    {
                        continue;
                    }

                    Vector2Int cell = new Vector2Int(x, y);
                    TileDefinition definition = definitions[resolvedGrid[x, y]];
                    for (int direction = 0; direction < 4; direction++)
                    {
                        Vector2Int neighbor = cell + DirectionOffset(direction);
                        if (!definition.HasConnection(direction) || !IsInside(neighbor) || resolvedGrid[neighbor.x, neighbor.y] == TileKind.Empty)
                        {
                            continue;
                        }

                        TileDefinition neighborDefinition = definitions[resolvedGrid[neighbor.x, neighbor.y]];
                        if (!neighborDefinition.HasConnection((direction + 2) % 4))
                        {
                            continue;
                        }

                        Vector3 doorway = CellToWorld(cell) + DirectionVector(direction) * tileSize * 0.5f + Vector3.up * 1.0f;
                        Vector3 extents = direction == 0 || direction == 2
                            ? new Vector3(1.35f, 0.85f, 0.28f)
                            : new Vector3(0.28f, 0.85f, 1.35f);
                        if (HasBlockingColliderAt(doorway, extents))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private bool HasBlockingColliderAt(Vector3 center, Vector3 extents)
        {
            Collider[] hits = Physics.OverlapBox(center, extents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i];
                if (hit == null || !hit.enabled || hit.isTrigger)
                {
                    continue;
                }

                if (hit.GetComponentInParent<PlayerMovementController>() != null
                    || hit.GetComponentInParent<InteractableObject>() != null
                    || hit.GetComponentInParent<CoolingZoneState>() != null)
                {
                    continue;
                }

                if (hit.transform.name.StartsWith("WFC Floor"))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static void DisableColliders(GameObject target)
        {
            Collider[] colliders = target.GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }
        }

        private static void EnsureInteractionTrigger(GameObject target, float radius)
        {
            SphereCollider sphere = target.GetComponent<SphereCollider>();
            if (sphere == null)
            {
                sphere = target.AddComponent<SphereCollider>();
            }

            sphere.isTrigger = true;
            sphere.radius = radius;
            sphere.center = Vector3.zero;
        }

        private void AddArchFramePart(Transform parent, string objectName, Vector3 localPosition, Vector3 size)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = $"Arch Solid {objectName}";
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = size;
            SetMaterial(part, wallMaterial);
        }

        private void RebuildPipeNetwork()
        {
            CoolantPipeNetworkBuilder builder = FindAnyObjectByType<CoolantPipeNetworkBuilder>();
            if (builder != null)
            {
                builder.RebuildNetwork();
            }
        }

        private Vector3 CellToWorld(Vector2Int cell)
        {
            float half = (gridSize - 1) * 0.5f;
            return transform.position + new Vector3((cell.x - half) * tileSize, 0f, (cell.y - half) * tileSize);
        }

        private bool IsInside(Vector2Int cell)
        {
            return cell.x >= 0 && cell.y >= 0 && cell.x < gridSize && cell.y < gridSize;
        }

        private static Vector2Int DirectionOffset(int direction)
        {
            switch (direction)
            {
                case 0: return new Vector2Int(0, 1);
                case 1: return new Vector2Int(1, 0);
                case 2: return new Vector2Int(0, -1);
                default: return new Vector2Int(-1, 0);
            }
        }

        private static Vector3 DirectionVector(int direction)
        {
            switch (direction)
            {
                case 0: return Vector3.forward;
                case 1: return Vector3.right;
                case 2: return Vector3.back;
                default: return Vector3.left;
            }
        }

        private static void SetMaterial(GameObject target, Material material)
        {
            if (target == null || material == null)
            {
                return;
            }

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sharedMaterial = material;
            }
        }
    }
}
