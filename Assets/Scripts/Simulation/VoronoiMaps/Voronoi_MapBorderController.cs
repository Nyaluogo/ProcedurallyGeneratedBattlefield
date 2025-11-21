using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using static GraphTheory.GraphMaster;
using static GraphTheory.Voronoi_NeighborhoodWars;

namespace GraphTheory
{
    public class Voronoi_MapBorderController : MonoBehaviour
    {
        public static Voronoi_MapBorderController Instance { get; private set; }

        Voronoi_MapBorderController()
        {
            Instance = this;
        }

        [Header("Voronoi Settings")]
        public float timestep = 0.1f;
        public VoronoiDiagramProperties voronoiDiagramProperties;

        [Header("Map Boundaries")]
        public Vector2 mapMinBounds = new Vector2(-10f, -10f);
        public Vector2 mapMaxBounds = new Vector2(10f, 10f);
        public int boundaryResolution = 64; // Increased for better accuracy

        [Header("Beacon Settings")]
        [Tooltip("The z-coordinate (height) at which beacons should be placed")]
        public float beaconHeight = 0.5f;

        [Header("Weighted Voronoi Settings")]
        [Tooltip("Use tower properties to create competitive boundaries")]
        public bool useWeightedVoronoi = true;

        [Tooltip("How much shootingPower affects boundary distance")]
        public float powerWeight = 0.5f;

        [Tooltip("How much towerLevel affects boundary distance")]
        public float levelWeight = 0.3f;

        [Tooltip("Base weight for all towers")]
        public float baseWeight = 1.0f;

        [Tooltip("Precision for binary search (smaller = more accurate)")]
        public float searchPrecision = 0.01f;

        [Tooltip("Maximum search iterations for boundary finding")]
        public int maxSearchIterations = 50;

        [Header("Minimum Range Settings")]
        [Tooltip("If true, ensures boundaries are at least tower.shootingRange away from tower")]
        public bool enforceMinimumRange = true;

        [Tooltip("Multiplier for expanding boundaries beyond minimum range")]
        public float boundaryExpansionFactor = 1.5f;

        [Header("Intersection Prevention")]
        [Tooltip("Detect and fix boundary intersections")]
        public bool preventIntersections = true;

        [Tooltip("Maximum allowed overlap distance")]
        public float maxOverlapTolerance = 0.1f;

        [Header("Visualization")]
        public bool drawDelaunayTriangulation = false;
        public bool drawVoronoiEdges = true;
        public bool drawCellCentroids = true;
        public bool drawMinimumRanges = true;
        public bool drawIntersections = true;
        public Color voronoiEdgeColor = Color.green;
        public Color delaunayEdgeColor = Color.blue;
        public Color minimumRangeColor = Color.yellow;
        public Color intersectionColor = Color.red;


        [System.Serializable]
        public class Region
        {
            // Map geometry data
            public Voronoi_Tower tower;
            public Voronoi_Beacon[] beacons;
            public List<Vector3> boundaryVertices = new List<Vector3>();
            public float actualBoundaryRadius = 0f;
            public float towerWeight = 1f;

            [Header("Territory Data")]
            public List<Voronoi_ElectronSoldier> garrisonSoldiers = new List<Voronoi_ElectronSoldier>();
            public List<Voronoi_ElectronSoldier> invadingSoldiers = new List<Voronoi_ElectronSoldier>();
            public int maxGarrisonSize = 15;
            public float defenseRingRadius = 5f;

            [Header("Battle Metrics")]
            public int totalBattles = 0;
            public int successfulDefenses = 0;
            public int timesConquered = 0;
            public float battleIntensity = 0f;
            public float controlStrength = 1f;

            [Header("Strategic Value")]
            public float strategicValue = 1f;
            public bool isBorderRegion = false;
            public bool isCapitalRegion = false;
            public int adjacentEnemyRegions = 0;

            [Header("Resources")]
            public float resourceGeneration = 1f;
            public float resourceStock = 100f;
            public float maxResourceStock = 500f;

            [Header("Visual Feedback")]
            public Material flagMaterial;
            public Color territoryColor = Color.white;
            public ParticleSystem battleEffectPrefab;
            private ParticleSystem activeBattleEffect;

            private float lastBattleTime = 0f;
            private const float BATTLE_COOLDOWN = 3f;

            [Header("Faction Assignment")]
            public RegionFactionDefinition nativeFaction;
            public RegionFactionDefinition rulingFaction=null;
            public bool isAtWar = false;

            // KEEP ONLY MAP-RELATED METHODS
            public void DrawBoundaryLines()
            {
                if (beacons == null || beacons.Length == 0) return;

                for (int i = 0; i < beacons.Length; i++)
                {
                    if (beacons[i] == null) continue;

                    int nextIndex = (i + 1) % beacons.Length;
                    if (beacons[nextIndex] != null)
                    {
                        Debug.DrawLine(beacons[i].transform.position, beacons[nextIndex].transform.position, Color.green);
                    }
                }
            }

            public void UpdateBeaconPositions()
            {
                if (boundaryVertices == null || boundaryVertices.Count == 0) return;

                if (beacons != null)
                {
                    for (int i = 0; i < Mathf.Min(beacons.Length, boundaryVertices.Count); i++)
                    {
                        if (beacons[i] != null)
                        {
                            beacons[i].transform.position = boundaryVertices[i];
                        }
                    }
                }
            }

            /// <summary>
            /// Update strategic importance based on neighbors
            /// </summary>
            public void UpdateStrategicValue(Region[] allRegions)
            {
                if (allRegions == null) return;

                adjacentEnemyRegions = 0;
                isBorderRegion = false;

                foreach (var other in allRegions)
                {
                    if (other == this || other.rulingFaction == null) continue;

                    if (IsAdjacent(other) && other.rulingFaction != this.rulingFaction)
                    {
                        adjacentEnemyRegions++;
                        isBorderRegion = true;
                    }
                }

                strategicValue = 1f;
                if (isBorderRegion) strategicValue += 0.5f;
                if (isCapitalRegion) strategicValue += 1f;
                strategicValue += adjacentEnemyRegions * 0.2f;
            }

            /// <summary>
            /// Check if another region is adjacent (shares boundary)
            /// </summary>
            public bool IsAdjacent(Region other)
            {
                if (other == null || tower == null || other.tower == null) return false;

                float distance = Vector3.Distance(tower.transform.position, other.tower.transform.position);
                float combinedRadius = actualBoundaryRadius + other.actualBoundaryRadius;

                return distance <= combinedRadius * 1.1f;
            }

            /// <summary>
            /// Check if region can be invaded (not already at war or few defending soldiers)
            /// </summary>
            public bool CanBeInvaded()
            {
                return !isAtWar && (Time.time - lastBattleTime) > BATTLE_COOLDOWN;
            }

            /// <summary>
            /// Calculate defensive strength of this region
            /// </summary>
            public float GetDefensiveStrength()
            {
                float strength = garrisonSoldiers.Count * controlStrength;
                if (tower != null)
                {
                    strength += tower.shootingPower * 0.5f;
                }
                if (isCapitalRegion) strength *= 1.5f;
                return strength;
            }

            /// <summary>
            /// Get color based on faction
            /// </summary>
            public Color GetFactionColor(Voronoi_NeighborhoodWars.SoldierFaction faction)
            {
                switch (faction)
                {
                    case Voronoi_NeighborhoodWars.SoldierFaction.Red: return Color.red;
                    case Voronoi_NeighborhoodWars.SoldierFaction.Blue: return Color.blue;
                    case Voronoi_NeighborhoodWars.SoldierFaction.Green: return Color.green;
                    case Voronoi_NeighborhoodWars.SoldierFaction.Yellow: return Color.yellow;
                    case Voronoi_NeighborhoodWars.SoldierFaction.Orange: return new Color(1f, 0.5f, 0f);
                    case Voronoi_NeighborhoodWars.SoldierFaction.Purple: return new Color(0.5f, 0f, 0.5f);
                    case Voronoi_NeighborhoodWars.SoldierFaction.Pink: return new Color(1f, 0.75f, 0.8f);
                    case Voronoi_NeighborhoodWars.SoldierFaction.Cyan: return Color.cyan;
                    case Voronoi_NeighborhoodWars.SoldierFaction.Magenta: return Color.magenta;
                    case Voronoi_NeighborhoodWars.SoldierFaction.Grey: return Color.grey;
                    default: return Color.white;
                }
            }

            public void UpdateFlagVisual()
            {
                if (flagMaterial != null && rulingFaction != null)
                {
                    flagMaterial.color = GetFactionColor(rulingFaction.faction);
                }

                if(rulingFaction != null)
                {

                }
            }

            /// <summary>
            /// Spawn or clear battle effects (visual feedback only)
            /// </summary>
            public void SetBattleEffect(bool active)
            {
                if (active && battleEffectPrefab != null && tower != null && activeBattleEffect == null)
                {
                    activeBattleEffect = GameObject.Instantiate(battleEffectPrefab, tower.transform.position, Quaternion.identity);
                }
                else if (!active && activeBattleEffect != null)
                {
                    GameObject.Destroy(activeBattleEffect.gameObject, 2f);
                    activeBattleEffect = null;
                }
            }

            /// <summary>
            /// Update last battle time (for cooldown tracking)
            /// </summary>
            public void RecordBattleTime()
            {
                lastBattleTime = Time.time;
            }

            /// <summary>
            /// Draw debug visualization
            /// </summary>
            public void DrawDebugInfo()
            {
                if (tower == null) return;

                Vector3 towerPos = tower.transform.position;

                Gizmos.color = territoryColor;
                Gizmos.DrawWireSphere(towerPos, defenseRingRadius);

                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(towerPos, 0.5f * strategicValue);

                if (isAtWar)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(towerPos, actualBoundaryRadius * 0.5f);
                }
            }
        }

        public Region[] regions;
        public GameObject beaconPrefab;

        private GraphMaster graphMaster;
        private List<VoronoiDiagramProperties.DelaunayTriangle> delaunayTriangles;
        private bool voronoiInitialized = false;
        private List<(Vector3, Vector3)> detectedIntersections = new List<(Vector3, Vector3)>();

        void Awake()
        {
            Instance = this;
            delaunayTriangles = new List<VoronoiDiagramProperties.DelaunayTriangle>();
        }

        void Start()
        {
            SetInitialReferences();
            InitializeVoronoi();
        }

        void Update()
        {
            SetUpdateReferences();

            if (voronoiInitialized)
            {
                if (drawVoronoiEdges)
                {
                    DrawAllBoundaryLines();
                }

                if (drawDelaunayTriangulation)
                {
                    DrawDelaunayTriangulation();
                }
            }
        }

        void SetInitialReferences()
        {
            graphMaster = GraphMaster.Instance;
        }

        void SetUpdateReferences()
        {
            timestep += Time.deltaTime;

            if (timestep >= 0.1f)
            {
                timestep = 0f;
            }
        }

        private float CalculateTowerWeight(Voronoi_Tower tower)
        {
            float weight = baseWeight;
            weight += tower.shootingPower * powerWeight;
            weight += tower.towerLevel * levelWeight;
            return weight;
        }

        public void InitializeVoronoi()
        {
            if (regions == null || regions.Length == 0)
            {
                Debug.LogError("No regions defined in Voronoi_MapBorderController!");
                return;
            }

            Debug.Log($"Initializing Voronoi diagram with {regions.Length} stationary towers...");

            voronoiDiagramProperties.cells = new VoronoiDiagramProperties.VoronoiCell[regions.Length];

            for (int i = 0; i < regions.Length; i++)
            {
                if (regions[i].tower == null)
                {
                    Debug.LogError($"Region {i} has no tower assigned!");
                    continue;
                }

                regions[i].towerWeight = CalculateTowerWeight(regions[i].tower);

                voronoiDiagramProperties.cells[i] = new VoronoiDiagramProperties.VoronoiCell();
                VoronoiDiagramProperties.VoronoiCell cell = voronoiDiagramProperties.cells[i];

                NodeBehavior towerNode = regions[i].tower.GetComponent<NodeBehavior>();
                if (towerNode == null)
                {
                    towerNode = regions[i].tower.gameObject.AddComponent<NodeBehavior>();
                    towerNode.Initialize(i, regions[i].tower.transform.position);
                    towerNode.nodeName = $"Tower_{i}";
                }
                cell.nucleus = towerNode;
                cell.weight = regions[i].towerWeight;
                cell.velocity = Vector3.zero;
                cell.forceMagnitude = 0f;
                cell.centroidPosition = regions[i].tower.transform.position;
                cell.area = Mathf.PI * regions[i].tower.shootingRange * regions[i].tower.shootingRange;

                Debug.Log($"Tower {i}: shootingPower={regions[i].tower.shootingPower}, towerLevel={regions[i].tower.towerLevel}, weight={regions[i].towerWeight:F2}");
            }

            GenerateDelaunayTriangulation();
            GenerateVoronoiFromDelaunay();

            if (useWeightedVoronoi)
            {
                CalculateWeightedVoronoiBoundaries();
            }
            else
            {
                CalculateCellBoundariesWithMinimumRange();
            }

            if (preventIntersections)
            {
                ResolveIntersections();
            }

            InstantiateBeacons();

            voronoiInitialized = true;
            Debug.Log("Voronoi diagram initialized successfully!");
        }

        /// <summary>
        /// Improved boundary calculation using binary search for exact boundary points.
        /// </summary>
        private void CalculateWeightedVoronoiBoundaries()
        {
            for (int i = 0; i < regions.Length; i++)
            {
                if (regions[i].tower == null) continue;

                regions[i].boundaryVertices.Clear();

                Vector3 towerPos = regions[i].tower.transform.position;
                float minRange = regions[i].tower.shootingRange;
                float currentWeight = regions[i].towerWeight;

                int samples = boundaryResolution;
                float angleStep = 360f / samples;

                for (int s = 0; s < samples; s++)
                {
                    float angle = s * angleStep * Mathf.Deg2Rad;
                    Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

                    // Calculate maximum search distance
                    float maxSearchRadius = CalculateMaxSearchRadius(towerPos);

                    // Use binary search for precise boundary location
                    float boundaryRadius = FindExactBoundary(towerPos, direction, minRange, maxSearchRadius, currentWeight, i);

                    // Clamp to map boundaries
                    Vector3 boundaryPoint = towerPos + direction * boundaryRadius;
                    boundaryPoint = ClampToMapBounds(boundaryPoint);
                    boundaryPoint.y = beaconHeight;

                    regions[i].boundaryVertices.Add(boundaryPoint);
                }

                // Smooth and validate
                SmoothBoundary(regions[i].boundaryVertices, 2);

                // Calculate actual boundary radius
                regions[i].actualBoundaryRadius = 0f;
                foreach (var vertex in regions[i].boundaryVertices)
                {
                    float dist = Vector3.Distance(towerPos, vertex);
                    if (dist > regions[i].actualBoundaryRadius)
                    {
                        regions[i].actualBoundaryRadius = dist;
                    }
                }

                if (voronoiDiagramProperties.cells[i] != null)
                {
                    voronoiDiagramProperties.cells[i].UpdateCentroid();
                    voronoiDiagramProperties.cells[i].UpdateArea();
                }

                Debug.Log($"Tower {i}: Weight={currentWeight:F2}, Boundary Radius={regions[i].actualBoundaryRadius:F2}");
            }
        }

        /// <summary>
        /// Binary search for exact boundary point between two territories.
        /// </summary>
        private float FindExactBoundary(Vector3 towerPos, Vector3 direction, float minRadius, float maxRadius, float currentWeight, int currentIndex)
        {
            float low = minRadius;
            float high = maxRadius;
            float bestRadius = minRadius;

            for (int iteration = 0; iteration < maxSearchIterations; iteration++)
            {
                if (high - low < searchPrecision)
                    break;

                float mid = (low + high) / 2f;
                Vector3 testPoint = towerPos + direction * mid;
                testPoint.y = beaconHeight;

                // Check ownership
                int owner = GetTerritoryOwner(testPoint, currentIndex, currentWeight);

                if (owner == currentIndex)
                {
                    bestRadius = mid;
                    low = mid; // Search further out
                }
                else
                {
                    high = mid; // Search closer in
                }
            }

            return Mathf.Max(bestRadius, minRadius);
        }

        /// <summary>
        /// Determines which tower owns a given point based on weighted distance.
        /// </summary>
        private int GetTerritoryOwner(Vector3 point, int currentIndex, float currentWeight)
        {
            float minWeightedDist = GetWeightedDistance(point, regions[currentIndex].tower.transform.position, currentWeight);
            int owner = currentIndex;

            for (int j = 0; j < regions.Length; j++)
            {
                if (j == currentIndex || regions[j].tower == null) continue;

                Vector3 otherTowerPos = regions[j].tower.transform.position;
                float otherWeight = regions[j].towerWeight;
                float otherWeightedDist = GetWeightedDistance(point, otherTowerPos, otherWeight);

                if (otherWeightedDist < minWeightedDist)
                {
                    minWeightedDist = otherWeightedDist;
                    owner = j;
                }
            }

            return owner;
        }

        /// <summary>
        /// Calculates maximum search radius based on map bounds.
        /// </summary>
        private float CalculateMaxSearchRadius(Vector3 towerPos)
        {
            float distToMinX = Mathf.Abs(towerPos.x - mapMinBounds.x);
            float distToMaxX = Mathf.Abs(towerPos.x - mapMaxBounds.x);
            float distToMinZ = Mathf.Abs(towerPos.z - mapMinBounds.y);
            float distToMaxZ = Mathf.Abs(towerPos.z - mapMaxBounds.y);

            return Mathf.Max(distToMinX, distToMaxX, distToMinZ, distToMaxZ) * 1.5f;
        }

        /// <summary>
        /// Clamps a point to map boundaries.
        /// </summary>
        private Vector3 ClampToMapBounds(Vector3 point)
        {
            point.x = Mathf.Clamp(point.x, mapMinBounds.x, mapMaxBounds.x);
            point.z = Mathf.Clamp(point.z, mapMinBounds.y, mapMaxBounds.y);
            return point;
        }

        /// <summary>
        /// Detects and resolves boundary intersections between regions.
        /// </summary>
        private void ResolveIntersections()
        {
            detectedIntersections.Clear();
            int maxIterations = 5;
            int iteration = 0;

            while (iteration < maxIterations)
            {
                bool foundIntersection = false;

                for (int i = 0; i < regions.Length; i++)
                {
                    for (int j = i + 1; j < regions.Length; j++)
                    {
                        if (DetectAndResolveIntersection(i, j))
                        {
                            foundIntersection = true;
                        }
                    }
                }

                if (!foundIntersection)
                    break;

                iteration++;
            }

            Debug.Log($"Intersection resolution completed in {iteration} iterations. Found {detectedIntersections.Count} intersections.");
        }

        /// <summary>
        /// Checks if two regions' boundaries intersect and resolves if found.
        /// </summary>
        private bool DetectAndResolveIntersection(int regionA, int regionB)
        {
            if (regions[regionA].boundaryVertices.Count < 2 || regions[regionB].boundaryVertices.Count < 2)
                return false;

            bool foundIntersection = false;

            for (int i = 0; i < regions[regionA].boundaryVertices.Count; i++)
            {
                int nextI = (i + 1) % regions[regionA].boundaryVertices.Count;
                Vector3 a1 = regions[regionA].boundaryVertices[i];
                Vector3 a2 = regions[regionA].boundaryVertices[nextI];

                for (int j = 0; j < regions[regionB].boundaryVertices.Count; j++)
                {
                    int nextJ = (j + 1) % regions[regionB].boundaryVertices.Count;
                    Vector3 b1 = regions[regionB].boundaryVertices[j];
                    Vector3 b2 = regions[regionB].boundaryVertices[nextJ];

                    if (LineSegmentsIntersect2D(a1, a2, b1, b2, out Vector3 intersection))
                    {
                        detectedIntersections.Add((a1, a2));
                        detectedIntersections.Add((b1, b2));

                        // Move conflicting vertices toward their tower centers
                        Vector3 towerA = regions[regionA].tower.transform.position;
                        Vector3 towerB = regions[regionB].tower.transform.position;

                        regions[regionA].boundaryVertices[i] = Vector3.Lerp(a1, towerA, 0.1f);
                        regions[regionA].boundaryVertices[nextI] = Vector3.Lerp(a2, towerA, 0.1f);
                        regions[regionB].boundaryVertices[j] = Vector3.Lerp(b1, towerB, 0.1f);
                        regions[regionB].boundaryVertices[nextJ] = Vector3.Lerp(b2, towerB, 0.1f);

                        foundIntersection = true;
                    }
                }
            }

            return foundIntersection;
        }

        /// <summary>
        /// Checks if two line segments intersect in 2D (XZ plane).
        /// </summary>
        private bool LineSegmentsIntersect2D(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, out Vector3 intersection)
        {
            intersection = Vector3.zero;

            float d = (p1.x - p2.x) * (p3.z - p4.z) - (p1.z - p2.z) * (p3.x - p4.x);

            if (Mathf.Abs(d) < 0.001f)
                return false; // Parallel lines

            float t = ((p1.x - p3.x) * (p3.z - p4.z) - (p1.z - p3.z) * (p3.x - p4.x)) / d;
            float u = -((p1.x - p2.x) * (p1.z - p3.z) - (p1.z - p2.z) * (p1.x - p3.x)) / d;

            if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
            {
                intersection.x = p1.x + t * (p2.x - p1.x);
                intersection.z = p1.z + t * (p2.z - p1.z);
                intersection.y = beaconHeight;
                return true;
            }

            return false;
        }

        private float GetWeightedDistance(Vector3 point, Vector3 sitePosition, float weight)
        {
            float euclideanDist = Vector2.Distance(
                new Vector2(point.x, point.z),
                new Vector2(sitePosition.x, sitePosition.z)
            );

            return euclideanDist - weight;
        }

        private void SmoothBoundary(List<Vector3> vertices, int iterations)
        {
            if (vertices.Count < 3) return;

            for (int iter = 0; iter < iterations; iter++)
            {
                List<Vector3> smoothed = new List<Vector3>(vertices.Count);

                for (int i = 0; i < vertices.Count; i++)
                {
                    int prevIndex = (i - 1 + vertices.Count) % vertices.Count;
                    int nextIndex = (i + 1) % vertices.Count;

                    Vector3 averaged = (vertices[prevIndex] + vertices[i] + vertices[nextIndex]) / 3f;
                    smoothed.Add(averaged);
                }

                vertices.Clear();
                vertices.AddRange(smoothed);
            }
        }

        // [Keep all existing methods: GenerateDelaunayTriangulation, IsPointInCircumcircle, AddUniqueEdge, 
        // HasEdge, GenerateVoronoiFromDelaunay, SharesDelaunayEdge, CalculateCellBoundariesWithMinimumRange,
        // GetClampedCircumcenter, CreateCircularBoundary, InstantiateBeacons, DrawAllBoundaryLines, 
        // DrawDelaunayTriangulation, RegenerateVoronoi, ClearBeacons]

        private void GenerateDelaunayTriangulation()
        {
            delaunayTriangles.Clear();

            if (voronoiDiagramProperties.cells.Length < 3)
            {
                Debug.LogWarning("Need at least 3 towers for triangulation!");
                return;
            }

            float padding = 100f;
            Vector3 superA = new Vector3(mapMinBounds.x - padding, beaconHeight, mapMinBounds.y - padding);
            Vector3 superB = new Vector3(mapMaxBounds.x + padding, beaconHeight, mapMinBounds.y - padding);
            Vector3 superC = new Vector3((mapMinBounds.x + mapMaxBounds.x) / 2f, beaconHeight, mapMaxBounds.y + padding);

            GameObject superAObj = new GameObject("SuperA");
            GameObject superBObj = new GameObject("SuperB");
            GameObject superCObj = new GameObject("SuperC");

            NodeBehavior nodeA = superAObj.AddComponent<NodeBehavior>();
            NodeBehavior nodeB = superBObj.AddComponent<NodeBehavior>();
            NodeBehavior nodeC = superCObj.AddComponent<NodeBehavior>();

            nodeA.Initialize(-1, superA);
            nodeB.Initialize(-2, superB);
            nodeC.Initialize(-3, superC);

            VoronoiDiagramProperties.DelaunayTriangle superTriangle = new VoronoiDiagramProperties.DelaunayTriangle
            {
                vertexA = nodeA,
                vertexB = nodeB,
                vertexC = nodeC
            };
            superTriangle.CalculateArea();

            delaunayTriangles.Add(superTriangle);

            foreach (var cell in voronoiDiagramProperties.cells)
            {
                if (cell == null || cell.nucleus == null) continue;

                List<VoronoiDiagramProperties.DelaunayTriangle> badTriangles = new List<VoronoiDiagramProperties.DelaunayTriangle>();

                foreach (var triangle in delaunayTriangles)
                {
                    if (IsPointInCircumcircle(cell.nucleus.position, triangle))
                    {
                        badTriangles.Add(triangle);
                    }
                }

                List<(NodeBehavior, NodeBehavior)> polygon = new List<(NodeBehavior, NodeBehavior)>();

                foreach (var triangle in badTriangles)
                {
                    AddUniqueEdge(polygon, triangle.vertexA, triangle.vertexB, badTriangles);
                    AddUniqueEdge(polygon, triangle.vertexB, triangle.vertexC, badTriangles);
                    AddUniqueEdge(polygon, triangle.vertexC, triangle.vertexA, badTriangles);
                }

                foreach (var triangle in badTriangles)
                {
                    delaunayTriangles.Remove(triangle);
                }

                foreach (var edge in polygon)
                {
                    VoronoiDiagramProperties.DelaunayTriangle newTriangle = new VoronoiDiagramProperties.DelaunayTriangle
                    {
                        vertexA = edge.Item1,
                        vertexB = edge.Item2,
                        vertexC = cell.nucleus
                    };
                    newTriangle.CalculateArea();
                    delaunayTriangles.Add(newTriangle);
                }
            }

            delaunayTriangles.RemoveAll(t =>
                t.vertexA == nodeA || t.vertexA == nodeB || t.vertexA == nodeC ||
                t.vertexB == nodeA || t.vertexB == nodeB || t.vertexB == nodeC ||
                t.vertexC == nodeA || t.vertexC == nodeB || t.vertexC == nodeC
            );

            Destroy(superAObj);
            Destroy(superBObj);
            Destroy(superCObj);

            Debug.Log($"Generated {delaunayTriangles.Count} Delaunay triangles");
        }

        private bool IsPointInCircumcircle(Vector3 point, VoronoiDiagramProperties.DelaunayTriangle triangle)
        {
            Vector3 circumcenter = triangle.GetCircumcenter();
            float circumradius = triangle.GetCircumradius();
            float distance = Vector3.Distance(point, circumcenter);

            return distance < circumradius - 0.001f;
        }

        private void AddUniqueEdge(List<(NodeBehavior, NodeBehavior)> polygon, NodeBehavior a, NodeBehavior b,
            List<VoronoiDiagramProperties.DelaunayTriangle> badTriangles)
        {
            int sharedCount = 0;
            foreach (var triangle in badTriangles)
            {
                if (HasEdge(triangle, a, b))
                {
                    sharedCount++;
                }
            }

            if (sharedCount == 1)
            {
                polygon.Add((a, b));
            }
        }

        private bool HasEdge(VoronoiDiagramProperties.DelaunayTriangle triangle, NodeBehavior a, NodeBehavior b)
        {
            return (triangle.vertexA == a && triangle.vertexB == b) ||
                   (triangle.vertexB == a && triangle.vertexC == b) ||
                   (triangle.vertexC == a && triangle.vertexA == b) ||
                   (triangle.vertexA == b && triangle.vertexB == a) ||
                   (triangle.vertexB == b && triangle.vertexC == a) ||
                   (triangle.vertexC == b && triangle.vertexA == a);
        }

        private void GenerateVoronoiFromDelaunay()
        {
            for (int i = 0; i < voronoiDiagramProperties.cells.Length; i++)
            {
                if (voronoiDiagramProperties.cells[i] == null) continue;

                List<VoronoiDiagramProperties.VoronoiCell> neighbors = new List<VoronoiDiagramProperties.VoronoiCell>();

                for (int j = 0; j < voronoiDiagramProperties.cells.Length; j++)
                {
                    if (i == j || voronoiDiagramProperties.cells[j] == null) continue;

                    if (SharesDelaunayEdge(voronoiDiagramProperties.cells[i], voronoiDiagramProperties.cells[j]))
                    {
                        neighbors.Add(voronoiDiagramProperties.cells[j]);
                    }
                }

                voronoiDiagramProperties.cells[i].neighboringCells = neighbors.ToArray();
            }
        }

        private bool SharesDelaunayEdge(VoronoiDiagramProperties.VoronoiCell cellA, VoronoiDiagramProperties.VoronoiCell cellB)
        {
            foreach (var triangle in delaunayTriangles)
            {
                bool hasA = triangle.vertexA == cellA.nucleus || triangle.vertexB == cellA.nucleus || triangle.vertexC == cellA.nucleus;
                bool hasB = triangle.vertexA == cellB.nucleus || triangle.vertexB == cellB.nucleus || triangle.vertexC == cellB.nucleus;

                if (hasA && hasB) return true;
            }
            return false;
        }

        private void CalculateCellBoundariesWithMinimumRange()
        {
            for (int i = 0; i < regions.Length; i++)
            {
                if (regions[i].tower == null) continue;

                regions[i].boundaryVertices.Clear();

                Vector3 towerPos = regions[i].tower.transform.position;
                float minRange = regions[i].tower.shootingRange;

                List<VoronoiDiagramProperties.DelaunayTriangle> cellTriangles = new List<VoronoiDiagramProperties.DelaunayTriangle>();

                foreach (var triangle in delaunayTriangles)
                {
                    if (triangle.vertexA == voronoiDiagramProperties.cells[i].nucleus ||
                        triangle.vertexB == voronoiDiagramProperties.cells[i].nucleus ||
                        triangle.vertexC == voronoiDiagramProperties.cells[i].nucleus)
                    {
                        cellTriangles.Add(triangle);
                    }
                }

                if (cellTriangles.Count < 2)
                {
                    Debug.LogWarning($"Tower {i} has insufficient triangles ({cellTriangles.Count}). Creating circular boundary.");
                    CreateCircularBoundary(regions[i], towerPos, minRange);
                    continue;
                }

                List<Vector3> orderedVertices = new List<Vector3>();
                HashSet<VoronoiDiagramProperties.DelaunayTriangle> visitedTriangles = new HashSet<VoronoiDiagramProperties.DelaunayTriangle>();

                VoronoiDiagramProperties.DelaunayTriangle currentTriangle = cellTriangles[0];
                visitedTriangles.Add(currentTriangle);
                orderedVertices.Add(GetClampedCircumcenter(currentTriangle));

                while (visitedTriangles.Count < cellTriangles.Count)
                {
                    VoronoiDiagramProperties.DelaunayTriangle nextTriangle = null;

                    foreach (var triangle in cellTriangles)
                    {
                        if (!visitedTriangles.Contains(triangle) && currentTriangle.SharesEdge(triangle))
                        {
                            nextTriangle = triangle;
                            break;
                        }
                    }

                    if (nextTriangle == null)
                    {
                        Debug.LogWarning($"Tower {i}: Non-contiguous Voronoi cell detected. Using available vertices.");
                        break;
                    }

                    visitedTriangles.Add(nextTriangle);
                    orderedVertices.Add(GetClampedCircumcenter(nextTriangle));
                    currentTriangle = nextTriangle;
                }

                if (visitedTriangles.Count < cellTriangles.Count)
                {
                    Debug.LogWarning($"Tower {i}: Incomplete traversal. Using angle-based sorting.");
                    orderedVertices.Clear();
                    foreach (var triangle in cellTriangles)
                    {
                        orderedVertices.Add(GetClampedCircumcenter(triangle));
                    }

                    orderedVertices.Sort((a, b) =>
                    {
                        float angleA = Mathf.Atan2(a.z - towerPos.z, a.x - towerPos.x);
                        float angleB = Mathf.Atan2(b.z - towerPos.z, b.x - towerPos.x);
                        return angleA.CompareTo(angleB);
                    });
                }

                foreach (var vertex in orderedVertices)
                {
                    Vector3 adjustedVertex = vertex;

                    Vector3 towerPosFlat = new Vector3(towerPos.x, beaconHeight, towerPos.z);
                    Vector3 vertexFlat = new Vector3(vertex.x, beaconHeight, vertex.z);
                    Vector3 direction = (vertexFlat - towerPosFlat).normalized;

                    float distance = Vector2.Distance(
                        new Vector2(vertex.x, vertex.z),
                        new Vector2(towerPos.x, towerPos.z)
                    );

                    if (enforceMinimumRange && distance < minRange)
                    {
                        adjustedVertex = towerPosFlat + direction * minRange;
                    }
                    else if (distance > minRange * boundaryExpansionFactor)
                    {
                        adjustedVertex = towerPosFlat + direction * (minRange * boundaryExpansionFactor);
                    }

                    adjustedVertex.y = beaconHeight;

                    regions[i].boundaryVertices.Add(adjustedVertex);
                }

                regions[i].actualBoundaryRadius = 0f;
                foreach (var vertex in regions[i].boundaryVertices)
                {
                    float dist = Vector3.Distance(towerPos, vertex);
                    if (dist > regions[i].actualBoundaryRadius)
                    {
                        regions[i].actualBoundaryRadius = dist;
                    }
                }

                if (voronoiDiagramProperties.cells[i] != null)
                {
                    voronoiDiagramProperties.cells[i].UpdateCentroid();
                    voronoiDiagramProperties.cells[i].UpdateArea();
                }

                Debug.Log($"Tower {i}: {regions[i].boundaryVertices.Count} vertices, Min Range = {minRange:F2}, Actual Boundary Radius = {regions[i].actualBoundaryRadius:F2}");
            }
        }

        private Vector3 GetClampedCircumcenter(VoronoiDiagramProperties.DelaunayTriangle triangle)
        {
            Vector3 circumcenter = triangle.GetCircumcenter();
            circumcenter.x = Mathf.Clamp(circumcenter.x, mapMinBounds.x, mapMaxBounds.x);
            circumcenter.z = Mathf.Clamp(circumcenter.z, mapMinBounds.y, mapMaxBounds.y);
            circumcenter.y = beaconHeight;
            return circumcenter;
        }

        private void CreateCircularBoundary(Region region, Vector3 towerPos, float radius)
        {
            int segments = boundaryResolution;
            float angleStep = 360f / segments;

            for (int i = 0; i < segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 vertex = towerPos + new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius
                );

                vertex.x = Mathf.Clamp(vertex.x, mapMinBounds.x, mapMaxBounds.x);
                vertex.z = Mathf.Clamp(vertex.z, mapMinBounds.y, mapMaxBounds.y);
                vertex.y = beaconHeight;

                region.boundaryVertices.Add(vertex);
            }

            region.actualBoundaryRadius = radius;
        }

        private void InstantiateBeacons()
        {
            if (beaconPrefab == null)
            {
                Debug.LogWarning("Beacon prefab not assigned! Skipping beacon instantiation.");
                return;
            }

            for (int i = 0; i < regions.Length; i++)
            {
                if (regions[i].boundaryVertices.Count == 0) continue;

                regions[i].beacons = new Voronoi_Beacon[regions[i].boundaryVertices.Count];

                for (int v = 0; v < regions[i].boundaryVertices.Count; v++)
                {
                    GameObject beaconObj = Instantiate(beaconPrefab, regions[i].boundaryVertices[v], Quaternion.identity);
                    beaconObj.name = $"Beacon_Region{i}_Vertex{v}";
                    beaconObj.transform.parent = transform;

                    Voronoi_Beacon beacon = beaconObj.GetComponent<Voronoi_Beacon>();
                    if (beacon == null)
                    {
                        beacon = beaconObj.AddComponent<Voronoi_Beacon>();
                    }

                    regions[i].beacons[v] = beacon;
                }

                for (int v = 0; v < regions[i].beacons.Length; v++)
                {
                    int prev = (v - 1 + regions[i].beacons.Length) % regions[i].beacons.Length;
                    int next = (v + 1) % regions[i].beacons.Length;

                    regions[i].beacons[v].SetAdjascentBeacons(new Voronoi_Beacon[]
                    {
                        regions[i].beacons[prev],
                        regions[i].beacons[next]
                    });
                }
            }

            Debug.Log($"Instantiated beacons for {regions.Length} regions");
        }

        public void DrawAllBoundaryLines()
        {
            if (regions == null) return;

            foreach (var region in regions)
            {
                if (region != null)
                {
                    region.DrawBoundaryLines();
                }
            }
        }

        private void DrawDelaunayTriangulation()
        {
            foreach (var triangle in delaunayTriangles)
            {
                if (triangle != null && triangle.IsValid())
                {
                    triangle.Draw();
                }
            }
        }

        public void RegenerateVoronoi()
        {
            ClearBeacons();
            InitializeVoronoi();
        }

        private void ClearBeacons()
        {
            if (regions == null) return;

            foreach (var region in regions)
            {
                if (region.beacons != null)
                {
                    foreach (var beacon in region.beacons)
                    {
                        if (beacon != null && beacon.gameObject != null)
                        {
                            Destroy(beacon.gameObject);
                        }
                    }
                    region.beacons = null;
                }
                region.boundaryVertices.Clear();
                region.actualBoundaryRadius = 0f;
            }

            delaunayTriangles.Clear();
            voronoiInitialized = false;
        }

        private void OnDrawGizmos()
        {
            if (!voronoiInitialized || regions == null) return;

            for (int i = 0; i < regions.Length; i++)
            {
                if (regions[i].tower == null) continue;

                Vector3 towerPos = regions[i].tower.transform.position;
                float minRange = regions[i].tower.shootingRange;

                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(towerPos, 0.3f);

                if (drawMinimumRanges)
                {
                    Gizmos.color = minimumRangeColor;
                    DrawCircleGizmo(towerPos, minRange, beaconHeight);
                }

                Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
                DrawCircleGizmo(towerPos, regions[i].actualBoundaryRadius, beaconHeight);

                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(towerPos, 0.1f + regions[i].towerWeight * 0.1f);
            }

            Gizmos.color = voronoiEdgeColor;
            foreach (var region in regions)
            {
                if (region.boundaryVertices.Count < 2) continue;

                for (int i = 0; i < region.boundaryVertices.Count; i++)
                {
                    int nextIndex = (i + 1) % region.boundaryVertices.Count;
                    Gizmos.DrawLine(region.boundaryVertices[i], region.boundaryVertices[nextIndex]);
                    Gizmos.DrawWireSphere(region.boundaryVertices[i], 0.1f);
                }
            }

            // Draw detected intersections in red
            if (drawIntersections)
            {
                Gizmos.color = intersectionColor;
                foreach (var (start, end) in detectedIntersections)
                {
                    Gizmos.DrawLine(start, end);
                    Gizmos.DrawWireSphere(start, 0.15f);
                }
            }

            if (drawCellCentroids && voronoiDiagramProperties.cells != null)
            {
                Gizmos.color = Color.cyan;
                foreach (var cell in voronoiDiagramProperties.cells)
                {
                    if (cell != null)
                    {
                        Vector3 centroidPos = cell.centroidPosition;
                        centroidPos.y = beaconHeight;
                        Gizmos.DrawWireSphere(centroidPos, 0.2f);
                    }
                }
            }

            Gizmos.color = Color.white;
            Vector3 bottomLeft = new Vector3(mapMinBounds.x, beaconHeight, mapMinBounds.y);
            Vector3 bottomRight = new Vector3(mapMaxBounds.x, beaconHeight, mapMinBounds.y);
            Vector3 topRight = new Vector3(mapMaxBounds.x, beaconHeight, mapMaxBounds.y);
            Vector3 topLeft = new Vector3(mapMinBounds.x, beaconHeight, mapMaxBounds.y);

            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topRight, topLeft);
            Gizmos.DrawLine(topLeft, bottomLeft);
        }

       

        private void DrawCircleGizmo(Vector3 center, float radius, float height, int segments = 32)
        {
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + new Vector3(Mathf.Cos(0) * radius, height - center.y, Mathf.Sin(0) * radius);

            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 newPoint = center + new Vector3(
                    Mathf.Cos(angle) * radius,
                    height - center.y,
                    Mathf.Sin(angle) * radius
                );
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }
        }

        [ContextMenu("Debug Region Configuration")]
        public void DebugRegionConfiguration()
        {
            Debug.Log("=== REGION CONFIGURATION DEBUG ===");

            if (regions == null || regions.Length == 0)
            {
                Debug.LogError("No regions defined!");
                return;
            }

            for (int i = 0; i < regions.Length; i++)
            {
                Debug.Log($"--- Region {i} ---");

                if (regions[i] == null)
                {
                    Debug.LogError($"Region {i} is NULL");
                    continue;
                }

                Debug.Log($"Tower: {(regions[i].tower != null ? regions[i].tower.name : "NULL")}");
                Debug.Log($"Native Faction: {(regions[i].nativeFaction != null ? regions[i].nativeFaction.name : "NULL")}");

                if (regions[i].nativeFaction != null)
                {
                    Debug.Log($"  - privateSoldierPrefab: {(regions[i].nativeFaction.privateSoldierPrefab != null ? "Assigned" : "NULL")}");

                    if (regions[i].nativeFaction.privateSoldierPrefab != null)
                    {
                        Debug.Log($"    - soldierPrefab: {(regions[i].nativeFaction.privateSoldierPrefab.soldierPrefab != null ? regions[i].nativeFaction.privateSoldierPrefab.soldierPrefab.name : "NULL")}");
                        Debug.Log($"    - maxRecruits: {regions[i].nativeFaction.privateSoldierPrefab.maxRecruits}");
                    }
                }

                Debug.Log($"Garrison Count: {regions[i].garrisonSoldiers.Count}");
                Debug.Log($"Max Garrison Size: {regions[i].maxGarrisonSize}");
            }

            Debug.Log("=== END DEBUG ===");
        }
    }
}