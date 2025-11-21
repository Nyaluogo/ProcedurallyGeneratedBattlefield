using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static GraphTheory.GraphMaster;

namespace GraphTheory
{
    /// <summary>
    /// 1. Generate N random room nodes
    ///- Rooms have random sizes and initial random positions
    ///
    ///2. Build Minimum Spanning Tree using Prim's Algorithm
    ///- Creates guaranteed connectivity between all rooms
    ///- Forms the "backbone" of the dungeon
    ///
    ///3. Add 20% of remaining edges back as cycles
    ///- Creates multiple paths and loops
    ///   - Makes dungeon more interesting to explore

    ///4. Apply force-directed layout
    ///- Rooms repel each other to avoid overlaps
    ///- Connected rooms attract to maintain reasonable corridor lengths
    ///- After X iterations, rooms are nicely spaced
    ///
    ///5. Convert to physical representation
    ///- Create actual room geometry at final positions
    ///- Build corridors between connected rooms
    ///- Populate with npcs and treasure based on room type
    /// </summary>
    public class Dungeon_ProceduralGenerator : MonoBehaviour
    {
        public static Dungeon_ProceduralGenerator Instance { get; private set; }
        Dungeon_ProceduralGenerator()
        {
            Instance = this;
        }

        void Awake()
        {
            Instance = this;
        }

        public float timestep = 0.1f;

        public enum MST_TYPE
        {
            PRIM,
            KRUSKA
        }
        public MST_TYPE mst_type = MST_TYPE.PRIM;

        [System.Serializable]
        public class DungeonConfig
        {
            public int dungeonWidth = 50;
            public int dungeonHeight = 50;
            public int dungeonDepth = 50;
            public int roomCount = 10;
            public int minRoomSize = 5;
            public int maxRoomSize = 15;
            public float maxWallThickness = 2f;

            [Range(0f, 1f)]
            public float extraEdgePercentage = 0.2f; // 20% extra edges for cycles
            public int layoutIterations = 50;
            public float repulsionForce = 100f;
            public float attractionForce = 0.5f;
            public float dampingFactor = 0.8f;

            public Color roomWallColor = Color.blue;
            public Color roomFloorColor = Color.gray;
            public Color corridorColor = Color.yellow;
            public Color mstEdgeColor = Color.green;
            public Color cycleEdgeColor = Color.red;
        }
        public DungeonConfig settings;

        [System.Serializable]
        public class Room
        {
            public int id;
            public Vector3Int position;
            public Vector3Int size;
            public Vector3 centerPosition;
            public Vector3 velocity;
            public Color color;
            public Transform root;
            public bool isRendered = false;

            public Room(int id, int x, int y, int z, int width, int height, int depth)
            {
                this.id = id;
                this.position = new Vector3Int(x, y, z);
                this.size = new Vector3Int(width, height, depth);

                // Initialize centerPosition BEFORE creating the GameObject
                this.centerPosition = new Vector3(x + width / 2f, y + height / 2f, z + depth / 2f);
                this.velocity = Vector3.zero;

                GameObject room_root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                room_root.name = $"Room_{id}";
                room_root.transform.localScale = new Vector3(width, height, depth);
                room_root.transform.position = centerPosition;
                root = room_root.transform;

                NodeBehavior nodeBehavior = room_root.AddComponent<NodeBehavior>();
                nodeBehavior.nodeId = id;
                nodeBehavior.dataObj = room_root;
                nodeBehavior.nodeName = room_root.name;

                // Add renderer for visualization
                Renderer renderer = room_root.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = Color.gray;
                }
            }

            public void UpdatePosition()
            {
                centerPosition += velocity;
                position.x = Mathf.RoundToInt(centerPosition.x - size.x / 2f);
                position.y = Mathf.RoundToInt(centerPosition.y - size.y / 2f);
                position.z = Mathf.RoundToInt(centerPosition.z - size.z / 2f);
            }

            public void UpdateVisualPosition()
            {
                if (root != null)
                {
                    root.position = centerPosition;

                    var vertex = root.GetComponent<NodeBehavior>();
                    if (vertex != null)
                    {
                        vertex.position = root.position;
                    }
                }
            }

            public bool Overlaps(Room other)
            {
                return !(position.x + size.x < other.position.x || position.x > other.position.x + other.size.x ||
                         position.y + size.y < other.position.y || position.y > other.position.y + other.size.y ||
                         position.z + size.z < other.position.z || position.z > other.position.z + other.size.z);
            }
        }


        [System.Serializable]
        public class Edge
        {
            public int roomA;
            public int roomB;
            public float weight;
            public bool isMSTEdge;
            public LineRenderer lineRenderer;

            public Edge(int a, int b, float weight)
            {
                this.roomA = a;
                this.roomB = b;
                this.weight = weight;
                this.isMSTEdge = false;
            }
        }

        [System.Serializable]
        public class Corridor
        {
            public Vector3 start;
            public Vector3 end;
            public int width = 2;
            public List<Transform> segments = new List<Transform>();

            public Corridor(Vector3 start, Vector3 end)
            {
                this.start = start;
                this.end = end;
            }
        }

        [System.Serializable]
        public class ConstructionMaterials
        {
            public GameObject floorTile;
            public GameObject wallTile;
            public GameObject pillar;

            public void InstantiateMaterials()
            {
                GameObject new_floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                GameObject new_wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                GameObject new_pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

                new_floor.transform.localScale = Vector3.one * 0.1f;
                new_wall.transform.localScale = new Vector3(0.1f, 1f, 0.1f);
                new_pillar.transform.localScale = new Vector3(0.1f, 1f, 0.1f);


                floorTile = new_floor;
                wallTile = new_wall;
                pillar = new_pillar;
            }
        }

        // Dungeon data structures
        private List<Room> rooms = new List<Room>();
        private List<Edge> allEdges = new List<Edge>();
        private List<Edge> mstEdges = new List<Edge>();
        private List<Edge> cycleEdges = new List<Edge>();
        private List<Corridor> corridors = new List<Corridor>();


        GraphMaster graphMaster;
        public TreeProperties dungeonTree;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            SetInitialReferences();
            GenerateMap();
        }

        // Update is called once per frame
        void Update()
        {
            SetUpdateReferences();
        }

        void SetInitialReferences()
        {
            if (graphMaster == null)
            {
                graphMaster = GraphMaster.Instance;
            }
        }

        void SetUpdateReferences()
        {
            timestep += Time.deltaTime;

            if (timestep >= 0f)
            {
                timestep = 0f;
            }
        }


        void GenerateRooms()
        {
            Debug.Log($"<===============Generating random rooms data==================>");
            rooms.Clear();

            for (int i = 0; i < settings.roomCount; i++)
            {
                int width = UnityEngine.Random.Range(settings.minRoomSize, settings.maxRoomSize);
                int height = UnityEngine.Random.Range(settings.minRoomSize, settings.maxRoomSize);
                int depth = UnityEngine.Random.Range(settings.minRoomSize, settings.maxRoomSize);
                int x = UnityEngine.Random.Range(0, settings.dungeonWidth - width);
                int y = UnityEngine.Random.Range(0, settings.dungeonHeight - height);
                int z = UnityEngine.Random.Range(0, settings.dungeonDepth - depth);

                Room room = new Room(i, x, y, z, width, height, depth);
                rooms.Add(room);

                Debug.Log($"Room {i}: Position=({x},{y},{z}), Size=({width}x{height}x{depth})");
            }
        }

        void BuildCompleteGraph()
        {
            Debug.Log($"<===============Building comoplete graph==================>");
            allEdges.Clear();

            for (int i = 0; i < rooms.Count; i++)
            {
                for (int j = i + 1; j < rooms.Count; j++)
                {
                    float distance = Vector3.Distance(rooms[i].centerPosition, rooms[j].centerPosition);
                    Edge edge = new Edge(i, j, distance);
                    allEdges.Add(edge);
                }
            }

            Debug.Log($"Created {allEdges.Count} potential edges");
        }

        private void BuildMST_Prim()
        {
            Debug.Log("=== STEP 2: Building MST with Prim's Algorithm ===");
            mstEdges.Clear();

            if (rooms.Count == 0) return;

            bool[] inMST = new bool[rooms.Count];
            float[] minWeight = new float[rooms.Count];
            int[] parent = new int[rooms.Count];

            // Initialize
            for (int i = 0; i < rooms.Count; i++)
            {
                minWeight[i] = float.MaxValue;
                parent[i] = -1;
                inMST[i] = false;
            }

            // Start with room 0
            minWeight[0] = 0;

            Debug.Log("Starting Prim's algorithm from room 0");

            for (int count = 0; count < rooms.Count; count++)
            {
                // Find minimum weight vertex not in MST
                int u = -1;
                float min = float.MaxValue;

                for (int v = 0; v < rooms.Count; v++)
                {
                    if (!inMST[v] && minWeight[v] < min)
                    {
                        min = minWeight[v];
                        u = v;
                    }
                }

                if (u == -1) break;

                inMST[u] = true;

                if (parent[u] != -1)
                {
                    Edge edge = new Edge(parent[u], u, minWeight[u]);
                    edge.isMSTEdge = true;
                    mstEdges.Add(edge);
                    Debug.Log($"Added MST edge: Room {parent[u]} -> Room {u}, weight={minWeight[u]:F2}");
                }

                // Update weights for adjacent vertices
                for (int v = 0; v < rooms.Count; v++)
                {
                    if (!inMST[v])
                    {
                        float weight = Vector3.Distance(rooms[u].centerPosition, rooms[v].centerPosition);
                        if (weight < minWeight[v])
                        {
                            minWeight[v] = weight;
                            parent[v] = u;
                        }
                    }
                }
            }

            Debug.Log($"MST completed with {mstEdges.Count} edges");
        }

        // STEP 3: Build MST using Kruskal's Algorithm
        private void BuildMST_Kruskal()
        {
            Debug.Log("=== STEP 2: Building MST with Kruskal's Algorithm ===");
            mstEdges.Clear();

            if (rooms.Count == 0) return;

            // Sort edges by weight
            allEdges.Sort((a, b) => a.weight.CompareTo(b.weight));

            // Union-Find data structure
            int[] parent = new int[rooms.Count];
            for (int i = 0; i < rooms.Count; i++)
                parent[i] = i;

            Debug.Log("Starting Kruskal's algorithm");

            foreach (Edge edge in allEdges)
            {
                int rootA = Find(parent, edge.roomA);
                int rootB = Find(parent, edge.roomB);

                if (rootA != rootB)
                {
                    mstEdges.Add(edge);
                    edge.isMSTEdge = true;
                    Union(parent, rootA, rootB);

                    Debug.Log($"Added MST edge: Room {edge.roomA} -> Room {edge.roomB}, weight={edge.weight:F2}");

                    if (mstEdges.Count >= rooms.Count - 1)
                        break;
                }
            }

            Debug.Log($"MST completed with {mstEdges.Count} edges");
        }

        // Union-Find helpers
        private int Find(int[] parent, int i)
        {
            if (parent[i] != i)
                parent[i] = Find(parent, parent[i]);
            return parent[i];
        }

        private void Union(int[] parent, int x, int y)
        {
            parent[x] = y;
        }


        private void AddCycleEdges()
        {
            Debug.Log("=== STEP 3: Adding Extra Edges for Cycles ===");
            cycleEdges.Clear();

            List<Edge> nonMSTEdges = new List<Edge>();
            foreach (Edge edge in allEdges)
            {
                if (!edge.isMSTEdge)
                    nonMSTEdges.Add(edge);
            }

            int extraEdgeCount = Mathf.CeilToInt(nonMSTEdges.Count * settings.extraEdgePercentage);

            // Shuffle and take first N edges
            for (int i = 0; i < extraEdgeCount && i < nonMSTEdges.Count; i++)
            {
                int randomIndex = UnityEngine.Random.Range(i, nonMSTEdges.Count);
                Edge temp = nonMSTEdges[i];
                nonMSTEdges[i] = nonMSTEdges[randomIndex];
                nonMSTEdges[randomIndex] = temp;

                cycleEdges.Add(nonMSTEdges[i]);
                Debug.Log($"Added cycle edge: Room {nonMSTEdges[i].roomA} -> Room {nonMSTEdges[i].roomB}");
            }

            Debug.Log($"Added {cycleEdges.Count} extra edges for cycles");
        }

        // STEP 5: Apply Force-Directed Layout
        private void ApplyForceDirectedLayout()
        {
            Debug.Log("=== STEP 4: Applying Force-Directed Layout ===");

            for (int iteration = 0; iteration < settings.layoutIterations; iteration++)
            {
                // Reset velocities
                foreach (Room room in rooms)
                {
                    room.velocity = Vector3.zero;
                }

                // Repulsion between ALL rooms
                for (int i = 0; i < rooms.Count; i++)
                {
                    for (int j = i + 1; j < rooms.Count; j++)
                    {
                        Vector3 direction = rooms[i].centerPosition - rooms[j].centerPosition;
                        float distance = direction.magnitude;

                        if (distance < 0.1f) distance = 0.1f; // Avoid division by zero

                        // Apply repulsion force
                        Vector3 force = (direction.normalized * settings.repulsionForce) / (distance * distance);

                        rooms[i].velocity += force;
                        rooms[j].velocity -= force;

                        // Extra repulsion for overlapping rooms
                        if (rooms[i].Overlaps(rooms[j]))
                        {
                            Vector3 separationForce = direction.normalized * settings.repulsionForce; // Simplified extra push
                            rooms[i].velocity += separationForce;
                            rooms[j].velocity -= separationForce;
                        }
                    }
                }

                // Attraction along ALL edges (MST + Cycles)
                List<Edge> allConnectedEdges = new List<Edge>(mstEdges);
                allConnectedEdges.AddRange(cycleEdges);

                foreach (Edge edge in allConnectedEdges)
                {
                    Vector3 direction = rooms[edge.roomB].centerPosition - rooms[edge.roomA].centerPosition;
                    float distance = direction.magnitude;

                    Vector3 force = direction.normalized * distance * settings.attractionForce;

                    rooms[edge.roomA].velocity += force;
                    rooms[edge.roomB].velocity -= force;
                }

                // Apply damping and update positions
                foreach (Room room in rooms)
                {
                    room.velocity *= settings.dampingFactor;
                    room.UpdatePosition();

                    // Keep rooms within dungeon bounds
                    room.centerPosition.x = Mathf.Clamp(room.centerPosition.x, room.size.x / 2f, settings.dungeonWidth - room.size.x / 2f);
                    room.centerPosition.y = Mathf.Clamp(room.centerPosition.y, room.size.y / 2f, settings.dungeonHeight - room.size.y / 2f);
                    room.centerPosition.z = Mathf.Clamp(room.centerPosition.z, room.size.z / 2f, settings.dungeonDepth - room.size.z / 2f);
                }

                if (iteration % 10 == 0)
                {
                    Debug.Log($"Layout iteration {iteration}/{settings.layoutIterations}");
                }
            }

            // After all iterations, update the visual positions one last time
            foreach (Room room in rooms)
            {
                room.UpdateVisualPosition();
            }

            Debug.Log("Force-directed layout completed");
        }

        // STEP 6: Connect Rooms with Corridors
        public void ConnectRooms()
        {
            Debug.Log("=== STEP 5: Creating Corridors ===");
            corridors.Clear();

            // Create corridors for MST edges
            foreach (Edge edge in mstEdges)
            {
                CreateCorridor(rooms[edge.roomA], rooms[edge.roomB], settings.mstEdgeColor);
            }

            // Create corridors for cycle edges
            foreach (Edge edge in cycleEdges)
            {
                CreateCorridor(rooms[edge.roomA], rooms[edge.roomB], settings.cycleEdgeColor);
            }

            Debug.Log($"Created {corridors.Count} corridors");
        }

        private void CreateCorridor(Room roomA, Room roomB, Color color)
        {
            Vector3 start = roomA.centerPosition;
            Vector3 end = roomB.centerPosition;

            Corridor corridor = new Corridor(start, end);
            corridors.Add(corridor);

            Vector3 current = start;
            float corridorWidth = corridor.width;

            // Create up to 3 segments for an L-shaped path in 3D
            // Segment 1: Move along X
            if (Mathf.Abs(start.x - end.x) > 0.1f)
            {
                Vector3 next = new Vector3(end.x, current.y, current.z);
                CreateCorridorSegment(current, next, corridorWidth, color, corridor);
                current = next;
            }

            // Segment 2: Move along Y
            if (Mathf.Abs(start.y - end.y) > 0.1f)
            {
                Vector3 next = new Vector3(current.x, end.y, current.z);
                CreateCorridorSegment(current, next, corridorWidth, color, corridor);
                current = next;
            }

            // Segment 3: Move along Z to the end
            CreateCorridorSegment(current, end, corridorWidth, color, corridor);
        }

        private void CreateCorridorSegment(Vector3 start, Vector3 end, float width, Color color, Corridor corridor)
        {
            if (Vector3.Distance(start, end) < 0.1f) return;

            GameObject segmentObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segmentObj.name = "CorridorSegment";
            Renderer renderer = segmentObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }

            Vector3 center = (start + end) / 2f;
            float length = Vector3.Distance(start, end);

            segmentObj.transform.position = center;

            Vector3 direction = (end - start).normalized;
            if (Mathf.Abs(direction.x) > 0.99f) // Aligned with X-axis
            {
                segmentObj.transform.localScale = new Vector3(length, width, width);
            }
            else if (Mathf.Abs(direction.y) > 0.99f) // Aligned with Y-axis
            {
                segmentObj.transform.localScale = new Vector3(width, length, width);
            }
            else // Aligned with Z-axis
            {
                segmentObj.transform.localScale = new Vector3(width, width, length);
            }

            corridor.segments.Add(segmentObj.transform);
        }

        public void ConstructRoom(ConstructionMaterials constructionMaterials)
        {
            constructionMaterials.InstantiateMaterials();

            if (constructionMaterials.wallTile != null)
            {
                //TODO: build the walls
            }

            if (constructionMaterials.floorTile != null)
            {
                //TODO: build and position floor
            }

            if (constructionMaterials.pillar != null)
            {
                //TODO build and position pillars between walls
            }

            //TODO: 
        }

        public void ConstructCorridoor(ConstructionMaterials constructionMaterials)
        {

        }


        public void EnforceSpatialLayout()
        {

        }

        public void GenerateMap()
        {
            //construct the rooms
            Debug.Log("========================================");
            Debug.Log("STARTING DUNGEON GENERATION");
            Debug.Log($"Using {mst_type} algorithm for MST");
            Debug.Log("========================================");

            // Clear previous dungeon
            ClearDungeon();

            // Step 1: Generate random rooms
            GenerateRooms();

            // Step 2: Build complete graph
            BuildCompleteGraph();

            // Step 3: Build MST
            if (mst_type == MST_TYPE.PRIM)
                BuildMST_Prim();
            else
                BuildMST_Kruskal();

            // Step 4: Add cycle edges
            AddCycleEdges();

            // Step 5: Apply force-directed layout
            ApplyForceDirectedLayout();

            // Step 6: Connect rooms with corridors
            ConnectRooms();

            Debug.Log("========================================");
            Debug.Log("DUNGEON GENERATION COMPLETE!");
            Debug.Log($"Total Rooms: {rooms.Count}");
            Debug.Log($"MST Edges: {mstEdges.Count}");
            Debug.Log($"Cycle Edges: {cycleEdges.Count}");
            Debug.Log($"Total Corridors: {corridors.Count}");
            Debug.Log("========================================");
        }

        private void ClearDungeon()
        {
            foreach (Room room in rooms)
            {
                if (room.root != null)
                    Destroy(room.root.gameObject);
            }

            foreach (Corridor corridor in corridors)
            {
                foreach (Transform segment in corridor.segments)
                {
                    if (segment != null)
                        Destroy(segment.gameObject);
                }
            }

            rooms.Clear();
            allEdges.Clear();
            mstEdges.Clear();
            cycleEdges.Clear();
            corridors.Clear();
        }

        public void RegenerateDungeon()
        {
            GenerateMap();
        }

        // Visualization helpers
        private void OnDrawGizmos()
        {
            if (rooms == null) return;

            // Draw MST edges in green
            Gizmos.color = Color.green;
            foreach (Edge edge in mstEdges)
            {
                if (edge.roomA < rooms.Count && edge.roomB < rooms.Count)
                {
                    Vector3 posA = rooms[edge.roomA].centerPosition;
                    Vector3 posB = rooms[edge.roomB].centerPosition;
                    Gizmos.DrawLine(posA, posB);
                }
            }

            // Draw cycle edges in red
            Gizmos.color = Color.red;
            foreach (Edge edge in cycleEdges)
            {
                if (edge.roomA < rooms.Count && edge.roomB < rooms.Count)
                {
                    Vector3 posA = rooms[edge.roomA].centerPosition;
                    Vector3 posB = rooms[edge.roomB].centerPosition;
                    Gizmos.DrawLine(posA, posB);
                }
            }
        }
    }
}