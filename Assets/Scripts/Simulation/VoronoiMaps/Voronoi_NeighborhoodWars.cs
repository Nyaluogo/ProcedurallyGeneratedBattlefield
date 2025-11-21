using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static GraphTheory.GraphMaster;
using static GraphTheory.Voronoi_MapBorderController;

namespace GraphTheory
{
    /// <summary>
    /// Rules of Engagement.
    /// Factions fight to control regions on the Voronoi map.
    /// There must always be a minimum of two factions at war.
    /// In order to control a region, a faction must defeat all opposing forces within it and form a voronoi circle around the tower at the center of the region to defend it.
    /// Depending on the strategic cost, soldiers may prioritize attacking border regions, defending the tower, or seeking out weaker enemy forces.
    /// There must always be a minimum number of soldiers left to protect the tower, hence attackers should be popped from the queue and the defence line redrawn.
    /// Once a faction defence is defeated and it loses a region, the ruling faction of that region changes to the victorious faction and the remaining native attackers are thus recruited to the ruling faction.
    /// In order to win the war, a faction must control all regions on the map.
    /// There must always be a minimum of two factions at war.
    /// </summary>
    public class Voronoi_NeighborhoodWars : MonoBehaviour
    {
        public static Voronoi_NeighborhoodWars Instance { get; private set; }

        public Voronoi_NeighborhoodWars()
        {
            Instance = this;
        }

        public enum SoldierFaction
        {
            Red,
            Blue,
            Green,
            Yellow,
            Orange,
            Purple,
            Pink,
            Cyan,
            Magenta,
            Grey
        }

        public enum BattleState
        {
            Recruiting,
            Organizing,
            InProgress,
            Paused,
            Completed,
        }

        /// <summary>
        /// Defines the faction's current overarching strategic goal, inspired by Clausewitz.
        /// </summary>
        public enum StrategicFocus
        {
            Consolidate, // Strengthen defenses, build up forces
            Attrition,   // Weaken a specific enemy faction by targeting their forces
            Conquest     // Focus all efforts on capturing a specific region
        }

        public BattleState currentBattleState = BattleState.Recruiting;

        private float moveTimer = 0f;
        private float battleTickInterval = 0.1f;

        // engagement tuning
        [Header("Engagement Rules")]
        public int maxAttackersPerTarget = 3;
        public float engagementSearchRadius = 30f;
        public float engagementSpacing = 1.2f;
        public int minTowerDefenders = 3; // Minimum soldiers that must stay to defend tower
        public float towerDefenseRadius = 5f; // Distance from tower where defenders are positioned

        [Header("War Declaration Settings")]
        public float warDeclarationInterval = 5f; // How often to check for war opportunities
        public float aggressionLevel = 0.6f; // 0 = defensive, 1 = very aggressive
        public int minSoldiersForWar = 5; // Minimum soldiers needed to declare war
        private float warDeclarationTimer = 0f;


        [System.Serializable]
        public class RecruitDefinition
        {
            public string recruitName;
            public GameObject soldierPrefab;
            private Transform[] spawnPoints;
            public int maxRecruits = 10;
            public float recruitInterval = 2f;
            public float offset = 1f;
            public BattleSoldier.SoldierRank rank = BattleSoldier.SoldierRank.Private;

            public List<Voronoi_ElectronSoldier> RecruitSoldiers()
            {
                // Unified validation: check all prerequisites
                if (soldierPrefab == null || spawnPoints == null || spawnPoints.Length == 0 || maxRecruits <= 0)
                {
                    Debug.LogError($"RecruitDefinition '{recruitName}' is not properly configured. SoldierPrefab: {soldierPrefab != null}, SpawnPoints: {spawnPoints?.Length ?? 0}, MaxRecruits: {maxRecruits}");
                    return new List<Voronoi_ElectronSoldier>();
                }

                List<Voronoi_ElectronSoldier> recruitedSoldiers = new List<Voronoi_ElectronSoldier>();
                int numberOfRecruits = Mathf.Min(maxRecruits, spawnPoints.Length);

                for (int i = 0; i < numberOfRecruits; i++)
                {
                    // Validate individual spawn point before use
                    if (spawnPoints[i] == null)
                    {
                        Debug.LogError($"Spawn point {i} is null for recruit '{recruitName}', skipping.");
                        continue;
                    }

                    var spawnPoint = spawnPoints[i];

                    // Apply offset to spawn position
                    Vector3 spawnPosition = new Vector3(
                        spawnPoint.position.x,
                        spawnPoint.position.y + offset,
                        spawnPoint.position.z
                    );

                    // Instantiate soldier
                    GameObject newSoldier = GameObject.Instantiate(soldierPrefab, spawnPosition, Quaternion.identity);

                    // Configure soldier component
                    if (newSoldier.TryGetComponent<Voronoi_ElectronSoldier>(out Voronoi_ElectronSoldier soldierComponent))
                    {
                        // Set soldier properties
                        soldierComponent.soldierName = $"{recruitName}_{i + 1}";
                        soldierComponent.rank = rank;
                        soldierComponent.position = spawnPosition;

                        // Configure NodeBehavior if present
                        if (soldierComponent.TryGetComponent<NodeBehavior>(out NodeBehavior nodeComponent))
                        {
                            nodeComponent.position = spawnPosition;
                            nodeComponent.priority = (int)rank;
                        }

                        recruitedSoldiers.Add(soldierComponent);
                    }
                    else
                    {
                        Debug.LogError($"Soldier prefab for '{recruitName}' does not have Voronoi_ElectronSoldier component!");
                        GameObject.Destroy(newSoldier);
                    }
                }

                return recruitedSoldiers;
            }

            public void AssignSpawnpoints(Transform[] points)
            {
                spawnPoints = points;
            }
        }

        /// <summary>
        /// Helper class for evaluating war targets
        /// </summary>
        private class WarTarget
        {
            public Voronoi_MapBorderController.Region region;
            public RegionFactionDefinition enemyFaction;
            public float strategicScore;
            public int defenderCount;
            public bool isWeakened;
        }

        [System.Serializable]
        public class RegionFactionDefinition
        {
            public string name;
            public int maxSoldiers = 20;
            public int maxSoldiersPerBattalion = 5;
            public Vector3 battlefieldSize;
            public HeapProperties playerCommandQueue;
            public Voronoi_ElectronSoldier rootCommander;
            public SoldierFaction faction;
            public RecruitDefinition privateSoldierPrefab;
            public RecruitDefinition corporalSoldierPrefab;
            public RecruitDefinition sergeantSoldierPrefab;
            public RecruitDefinition lieutenantSoldierPrefab;
            public RecruitDefinition captainSoldierPrefab;
            public RecruitDefinition majorSoldierPrefab;
            public RecruitDefinition colonelSoldierPrefab;
            public RecruitDefinition generalSoldierPrefab;

            public bool isReady = false;
            public bool isDefeated = false;

            // --- New Strategic Properties ---
            public StrategicFocus currentFocus = StrategicFocus.Consolidate;
            public RegionFactionDefinition strategicTargetFaction;
            public Voronoi_MapBorderController.Region strategicTargetRegion;
            public float timeInCurrentStrategy = 0f;


            // Controlled regions by this faction
            public List<Voronoi_MapBorderController.Region> controlledRegions = new List<Voronoi_MapBorderController.Region>();

            public void GenerateCommandQueue()
            {
                playerCommandQueue = new HeapProperties()
                {
                    heapType = HeapProperties.HeapType.MinHeap,
                    heapNodes = new NodeBehavior[maxSoldiers]
                };

                // Generate spawn points for each controlled region BEFORE recruiting
                foreach (var region in controlledRegions)
                {
                    if (region == null || region.tower == null)
                    {
                        Debug.LogError($"Region or tower is null for faction {name}");
                        continue;
                    }

                    // Generate spawn points for this region
                    Transform[] spawnPoints = GenerateSpawnPoints(region, maxSoldiersPerBattalion);


                }
            }

            /// <summary>
            /// Generate spawn points in a circle around the region tower
            /// </summary>
            private Transform[] GenerateSpawnPoints(Voronoi_MapBorderController.Region region, int count)
            {
                if (region.tower == null)
                {
                    Debug.LogError("Cannot generate spawn points: Tower is null!");
                    return new Transform[0];
                }

                Vector3 towerPos = region.tower.transform.position;
                float angleStep = 360f / count;

                Transform[] spawnPoints = new Transform[count];
                for (int i = 0; i < count; i++)
                {
                    float angle = i * angleStep * Mathf.Deg2Rad;
                    Vector3 offset = new Vector3(
                        Mathf.Cos(angle) * region.defenseRingRadius,
                        0.5f,
                        Mathf.Sin(angle) * region.defenseRingRadius
                    );

                    GameObject spawnPoint = new GameObject($"SpawnPoint_{name}_{i}");
                    spawnPoint.transform.position = towerPos + offset;
                    spawnPoints[i] = spawnPoint.transform;
                }

                return spawnPoints;
            }


            /// <summary>
            /// Clean up spawn point GameObjects after recruitment
            /// </summary>
            private void CleanupSpawnPoints(Transform[] spawnPoints)
            {
                foreach (var sp in spawnPoints)
                {
                    if (sp != null) GameObject.Destroy(sp.gameObject);
                }
            }


            public void OrganizeHeapHierarchy()
            {
                if (playerCommandQueue == null || playerCommandQueue.heapNodes.Length == 0)
                {
                    Debug.LogError("Command queue is empty or not initialized.");
                    return;
                }

                for (int i = 0; i < playerCommandQueue.heapSize; i++)
                {
                    var currentNode = playerCommandQueue.heapNodes[i];
                    if (currentNode == null) continue;

                    var soldier = currentNode.GetComponent<Voronoi_ElectronSoldier>();
                    if (soldier == null) continue;

                    // Set commander (parent)
                    int parentIdx = (i - 1) / 2;
                    if (parentIdx >= 0 && parentIdx < playerCommandQueue.heapSize)
                    {
                        soldier.myCommander = playerCommandQueue.heapNodes[parentIdx].GetComponent<Voronoi_ElectronSoldier>();
                    }

                    // Set subordinates (children)
                    int leftIdx = 2 * i + 1;
                    int rightIdx = 2 * i + 2;

                    if (leftIdx < playerCommandQueue.heapSize)
                    {
                        soldier.myLeftSubordinate = playerCommandQueue.heapNodes[leftIdx].GetComponent<Voronoi_ElectronSoldier>();
                    }

                    if (rightIdx < playerCommandQueue.heapSize)
                    {
                        soldier.myRightSubordinate = playerCommandQueue.heapNodes[rightIdx].GetComponent<Voronoi_ElectronSoldier>();
                    }
                }
            }

            /// <summary>
            /// Get all living soldiers from controlled regions
            /// </summary>
            public List<Voronoi_ElectronSoldier> GetAllLivingSoldiers()
            {
                List<Voronoi_ElectronSoldier> soldiers = new List<Voronoi_ElectronSoldier>();
                foreach (var region in controlledRegions)
                {
                    if (region == null) continue;
                    soldiers.AddRange(region.garrisonSoldiers.Where(s => s != null && s.fightingState != BattleSoldier.FightingState.Dead));
                }
                return soldiers;
            }

            /// <summary>
            /// Count total living soldiers
            /// </summary>
            public int GetLivingSoldierCount()
            {
                return GetAllLivingSoldiers().Count;
            }
        }

        public List<RegionFactionDefinition> factions = new List<RegionFactionDefinition>();

        GraphMaster graphMaster;

        private void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            SetInitialReferences();
        }

        void Update()
        {
            SetUpdateReferences();
        }

        void SetInitialReferences()
        {
            graphMaster = GetComponent<GraphMaster>();
            // Initialize factions AFTER map is generated
            Invoke(nameof(InitializeRegionFactions), 1f); // Short delay to ensure map is ready
            Invoke(nameof(StartWar), 3f);
        }

        void SetUpdateReferences()
        {
            if (currentBattleState != BattleState.InProgress) return;

            moveTimer += Time.deltaTime;
            warDeclarationTimer += Time.deltaTime;

            if (moveTimer >= battleTickInterval)
            {
                ProcessBattleTick();
                EvaluateBattlefield();
                moveTimer = 0f;
            }

            // Check for war declaration opportunities
            if (warDeclarationTimer >= warDeclarationInterval)
            {
                EvaluateWarDeclarations();
                warDeclarationTimer = 0f;
            }
        }

        public void InitialiseFactions()
        {
            var map = Voronoi_MapBorderController.Instance;

            if (map == null || map.regions == null || map.regions.Length < 2)
            {
                Debug.LogError("Not enough regions to initialize factions. Minimum 2 required.");
                return;
            }

            factions.Clear();

            foreach (var region in map.regions)
            {
                if (region != null && region.nativeFaction != null)
                {
                    if (!factions.Contains(region.nativeFaction))
                    {
                        factions.Add(region.nativeFaction);
                        region.nativeFaction.controlledRegions.Clear(); // Clear before adding
                    }

                    // Add region to faction's controlled list
                    if (!region.nativeFaction.controlledRegions.Contains(region))
                    {
                        region.nativeFaction.controlledRegions.Add(region);
                    }
                }
            }

            Debug.Log($"<color=lime>Initialized {factions.Count} factions for war</color>");

            // Debug: Print faction-region mapping
            foreach (var faction in factions)
            {
                Debug.Log($"<color=cyan>Faction {faction.name}: {faction.controlledRegions.Count} controlled regions</color>");
            }
        }

        public void PrepareBattle()
        {
            currentBattleState = BattleState.Recruiting;

            if (factions.Count < 2)
            {
                Debug.LogError("Cannot start war: minimum 2 factions required!");
                return;
            }

            // Organize existing soldiers into the heap
            foreach (var faction in factions)
            {
                // Initialize the heap if not already done
                if (faction.playerCommandQueue == null)
                {
                    faction.playerCommandQueue = new HeapProperties()
                    {
                        heapType = HeapProperties.HeapType.MinHeap,
                        heapNodes = new NodeBehavior[faction.maxSoldiers]
                    };
                }

                // IMPORTANT: Refresh controlled regions list before adding soldiers
                // This ensures we capture all regions that were assigned during InitializeRegionFactions
                faction.controlledRegions.Clear();
                var allRegions = Voronoi_MapBorderController.Instance.regions;
                foreach (var region in allRegions)
                {
                    if (region != null && region.rulingFaction == faction)
                    {
                        faction.controlledRegions.Add(region);
                    }
                }

                Debug.Log($"<color=yellow>Faction {faction.name}: {faction.controlledRegions.Count} controlled regions</color>");

                // Add already-spawned soldiers from regions to the heap
                int soldiersAdded = 0;
                foreach (var region in faction.controlledRegions)
                {
                    if (region == null)
                    {
                        Debug.LogWarning($"Null region in {faction.name}'s controlled regions");
                        continue;
                    }

                    Debug.Log($"<color=cyan>Processing region {region.tower?.name ?? "Unknown"} with {region.garrisonSoldiers.Count} garrison soldiers</color>");

                    foreach (var soldier in region.garrisonSoldiers)
                    {
                        if (soldier == null)
                        {
                            Debug.LogWarning("Null soldier in garrison, skipping");
                            continue;
                        }

                        var soldier_node = soldier.GetComponent<NodeBehavior>();
                        if (soldier_node == null)
                        {
                            Debug.LogError($"Soldier {soldier.soldierName} missing NodeBehavior component!");
                            continue;
                        }

                        // Set rank and priority
                        soldier_node.priority = (int)soldier.rank;

                        // Insert into heap
                        try
                        {
                            faction.playerCommandQueue.Insert(soldier_node);
                            soldiersAdded++;
                            Debug.Log($"<color=green>Added {soldier.soldierName} (rank {soldier.rank}) to {faction.name}'s queue</color>");
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"Failed to insert soldier into heap: {e.Message}");
                        }
                    }
                }

                Debug.Log($"<color=lime>Faction {faction.name}: Added {soldiersAdded} soldiers to command queue (heap size: {faction.playerCommandQueue.heapSize})</color>");

                // Organize heap hierarchy
                if (faction.playerCommandQueue.heapSize > 0)
                {
                    faction.OrganizeHeapHierarchy();
                }
                else
                {
                    Debug.LogError($"<color=red>Faction {faction.name} has ZERO soldiers in heap after preparation!</color>");
                }
            }

            currentBattleState = BattleState.Organizing;
            Debug.Log($"<color=yellow>Battle preparation complete. Organized {factions.Count} factions.</color>");
        }

        public void StartWar()
        {
            PrepareBattle();

            if (factions.Count < 2)
            {
                Debug.LogError("War requires at least 2 factions!");
                return;
            }

            int readyFactions = 0;
            foreach (var faction in factions)
            {
                if (faction.playerCommandQueue == null || faction.playerCommandQueue.heapSize == 0)
                {
                    Debug.LogError($"Faction {faction.name} has no soldiers to fight.");
                    faction.isDefeated = true;
                }
                else
                {
                    faction.isReady = true;
                    faction.isDefeated = false;
                    readyFactions++;
                    Debug.Log($"<color=cyan>Faction {faction.name} is ready with {faction.GetLivingSoldierCount()} soldiers</color>");
                }
            }

            if (readyFactions >= 2)
            {
                StartBattle();
                Debug.Log($"<color=cyan>War started with {readyFactions} factions!</color>");
            }
            else
            {
                Debug.LogError($"Cannot start war: only {readyFactions} ready factions (need at least 2)");
            }
        }

        /// <summary>
        /// Evaluate strategic opportunities and declare wars
        /// </summary>
        private void EvaluateWarDeclarations()
        {
            if (factions.Count < 2) return;

            var allRegions = Voronoi_MapBorderController.Instance.regions;

            foreach (var faction in factions)
            {
                if (faction == null || faction.isDefeated) continue;

                // Get living soldiers count
                int availableSoldiers = faction.GetLivingSoldierCount();

                // Check if faction has enough forces to consider offensive action
                if (availableSoldiers < minSoldiersForWar) continue;

                // Count soldiers currently defending vs attacking
                int defendingSoldiers = 0;
                int attackingSoldiers = 0;

                foreach (var soldier in faction.GetAllLivingSoldiers())
                {
                    if (soldier.fightingState == BattleSoldier.FightingState.Defending)
                        defendingSoldiers++;
                    else if (soldier.fightingState == BattleSoldier.FightingState.Attacking)
                        attackingSoldiers++;
                }

                // Calculate how many soldiers can be allocated to offense
                int reserveDefenders = Mathf.CeilToInt(faction.controlledRegions.Count * minTowerDefenders * 1.5f);
                int offensivePotential = availableSoldiers - reserveDefenders;

                if (offensivePotential < 3) continue; // Need at least 3 soldiers for attack

                // Find potential targets
                List<WarTarget> potentialTargets = EvaluatePotentialTargets(faction, allRegions);

                if (potentialTargets.Count == 0) continue;

                // Sort by strategic value (highest first)
                potentialTargets.Sort((a, b) => b.strategicScore.CompareTo(a.strategicScore));

                // Decide whether to declare war based on aggression level
                float warChance = aggressionLevel * 0.8f + Random.Range(0f, 0.2f);

                if (Random.value < warChance)
                {
                    // Select target (weighted towards high-value targets)
                    WarTarget chosenTarget = SelectWarTarget(potentialTargets);

                    if (chosenTarget != null)
                    {
                        InitiateInvasion(faction, chosenTarget.region, offensivePotential);
                    }
                }
            }
        }

        /// <summary>
        /// Evaluate potential invasion targets for a faction
        /// </summary>
        private List<WarTarget> EvaluatePotentialTargets(RegionFactionDefinition attackingFaction,
            Voronoi_MapBorderController.Region[] allRegions)
        {
            List<WarTarget> targets = new List<WarTarget>();

            foreach (var region in allRegions)
            {
                if (region == null || region.rulingFaction == null) continue;
                if (region.rulingFaction == attackingFaction) continue; // Skip own regions
                if (region.isAtWar) continue; // Skip regions already at war

                // Calculate strategic score
                float score = 0f;

                // 1. Strategic value of the region
                score += region.strategicValue * 50f;

                // 2. Adjacency bonus (prefer neighboring regions)
                bool isAdjacent = false;
                foreach (var ownedRegion in attackingFaction.controlledRegions)
                {
                    if (ownedRegion != null && ownedRegion.IsAdjacent(region))
                    {
                        isAdjacent = true;
                        score += 100f; // Strong preference for adjacent regions
                        break;
                    }
                }

                // Skip non-adjacent regions if aggression is low
                if (!isAdjacent && aggressionLevel < 0.5f) continue;

                // 3. Defender strength analysis
                int livingDefenders = region.garrisonSoldiers.Count(s =>
                    s != null && s.fightingState != BattleSoldier.FightingState.Dead);

                // Prefer weakly defended regions
                if (livingDefenders < region.maxGarrisonSize * 0.5f)
                {
                    score += 80f; // Weakened target bonus
                }
                else if (livingDefenders > region.maxGarrisonSize * 0.8f)
                {
                    score -= 40f; // Strong defense penalty
                }

                // 4. Enemy faction strength
                int enemyTotalForces = region.rulingFaction.GetLivingSoldierCount();
                int myTotalForces = attackingFaction.GetLivingSoldierCount();

                if (myTotalForces > enemyTotalForces * 1.5f)
                {
                    score += 60f; // Overwhelming advantage
                }
                else if (myTotalForces < enemyTotalForces * 0.7f)
                {
                    score -= 50f; // Risky target
                }

                // 5. Distance penalty (prefer closer targets)
                if (!isAdjacent)
                {
                    float minDistance = float.MaxValue;
                    foreach (var ownedRegion in attackingFaction.controlledRegions)
                    {
                        if (ownedRegion != null && ownedRegion.tower != null && region.tower != null)
                        {
                            float dist = Vector3.Distance(ownedRegion.tower.transform.position,
                                region.tower.transform.position);
                            minDistance = Mathf.Min(minDistance, dist);
                        }
                    }
                    score -= minDistance * 0.5f; // Distance penalty
                }

                // 6. Control strength (prefer unstable regions)
                score += (1f - region.controlStrength) * 30f;

                // Add random variance for unpredictability
                score += Random.Range(-20f, 20f);

                targets.Add(new WarTarget
                {
                    region = region,
                    enemyFaction = region.rulingFaction,
                    strategicScore = score,
                    defenderCount = livingDefenders,
                    isWeakened = livingDefenders < region.maxGarrisonSize * 0.5f
                });
            }

            return targets;
        }

        /// <summary>
        /// Select a war target with weighted random selection
        /// </summary>
        private WarTarget SelectWarTarget(List<WarTarget> targets)
        {
            if (targets.Count == 0) return null;

            // High aggression = always pick best target
            if (aggressionLevel > 0.8f)
            {
                return targets[0];
            }

            // Medium/Low aggression = weighted random selection from top targets
            int considerCount = Mathf.Min(3, targets.Count);
            float totalWeight = 0f;

            for (int i = 0; i < considerCount; i++)
            {
                totalWeight += Mathf.Max(0f, targets[i].strategicScore);
            }

            if (totalWeight <= 0f) return targets[0];

            float randomValue = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            for (int i = 0; i < considerCount; i++)
            {
                cumulative += Mathf.Max(0f, targets[i].strategicScore);
                if (randomValue <= cumulative)
                {
                    return targets[i];
                }
            }

            return targets[0];
        }

        /// <summary>
        /// Launch an invasion against a target region
        /// </summary>
        private void InitiateInvasion(RegionFactionDefinition attackingFaction,
            Voronoi_MapBorderController.Region targetRegion, int maxInvaders)
        {
            if (targetRegion == null || attackingFaction == null) return;

            List<Voronoi_ElectronSoldier> invasionForce = new List<Voronoi_ElectronSoldier>();

            // Gather invasion force from border regions (prioritize those adjacent to target)
            foreach (var ownedRegion in attackingFaction.controlledRegions)
            {
                if (ownedRegion == null) continue;

                // Prioritize adjacent regions
                bool isAdjacent = ownedRegion.IsAdjacent(targetRegion);
                int maxFromThisRegion = isAdjacent ?
                    Mathf.FloorToInt(ownedRegion.garrisonSoldiers.Count * 0.6f) :
                    Mathf.FloorToInt(ownedRegion.garrisonSoldiers.Count * 0.3f);

                int recruited = 0;
                foreach (var soldier in ownedRegion.garrisonSoldiers)
                {
                    if (soldier == null || soldier.fightingState == BattleSoldier.FightingState.Dead)
                        continue;

                    // Don't take soldiers already engaged
                    if (soldier.myEnemy != null) continue;

                    // Ensure minimum defenders remain
                    int remainingDefenders = ownedRegion.garrisonSoldiers.Count - recruited;
                    if (remainingDefenders <= minTowerDefenders) break;

                    invasionForce.Add(soldier);
                    recruited++;

                    if (recruited >= maxFromThisRegion || invasionForce.Count >= maxInvaders)
                        break;
                }

                if (invasionForce.Count >= maxInvaders) break;
            }

            if (invasionForce.Count < 2)
            {
                Debug.Log($"{attackingFaction.name} couldn't muster enough forces for invasion");
                return;
            }

            // Remove soldiers from their current garrisons and mark as invaders
            foreach (var soldier in invasionForce)
            {
                // Find and remove from current region
                foreach (var region in attackingFaction.controlledRegions)
                {
                    if (region != null && region.garrisonSoldiers.Contains(soldier))
                    {
                        region.garrisonSoldiers.Remove(soldier);
                        break;
                    }
                }
            }

            // Execute the invasion
            InvadeRegion(targetRegion, invasionForce, attackingFaction);

            Debug.Log($"<color=yellow>{attackingFaction.name} declares WAR on {targetRegion.rulingFaction.name}! " +
                      $"Invasion force: {invasionForce.Count} soldiers</color>");
        }

        public void ProcessBattleTick()
        {
            var regions = Voronoi_MapBorderController.Instance.regions;

            // 1. Update all region battle states and strategic values first
            foreach (var region in regions)
            {
                if (region == null) continue;

                if (region.isAtWar)
                {
                    UpdateRegionBattleState(region);
                }
                else
                {
                    // Passive actions for regions not in combat
                    RebuildRegionMilitary(region, Time.deltaTime);
                }
                region.UpdateStrategicValue(regions);
            }

            // 2. Execute turns for all active factions using the new strategic model
            foreach (var faction in factions)
            {
                if (faction == null || faction.isDefeated) continue;

                // Update strategy periodically
                faction.timeInCurrentStrategy += battleTickInterval;
                if (faction.timeInCurrentStrategy > 15f) // Re-evaluate strategy every 15 seconds
                {
                    DetermineStrategicFocus(faction, factions.Where(f => f != faction).ToList());
                }

                // Execute the strategy for all soldiers of this faction
                ExecuteClausewitzianStrategy(faction, regions);
            }
        }

        /// <summary>
        /// Determines the best strategic focus for a faction based on Clausewitzian and Sun Tzu principles.
        /// </summary>
        private void DetermineStrategicFocus(RegionFactionDefinition faction, List<RegionFactionDefinition> enemies)
        {
            faction.timeInCurrentStrategy = 0;
            int myStrength = faction.GetLivingSoldierCount();

            // Clausewitz: Is the political object (winning the war) achievable?
            // Sun Tzu: If you are weak, avoid conflict.
            if (myStrength < minSoldiersForWar * 1.5f)
            {
                faction.currentFocus = StrategicFocus.Consolidate;
                Debug.Log($"<color=cyan>Strategy for {faction.name}: CONSOLIDATE (low forces)</color>");
                return;
            }

            // Find the most threatening or weakest enemy (the "center of gravity")
            var primaryEnemy = enemies
                .Where(e => e != null && !e.isDefeated)
                .OrderByDescending(e => e.GetLivingSoldierCount()) // Most threatening
                .FirstOrDefault();

            if (primaryEnemy == null)
            {
                faction.currentFocus = StrategicFocus.Consolidate;
                return;
            }

            // Sun Tzu: Attack weakness. Find a vulnerable, high-value region.
            var targetRegion = Voronoi_MapBorderController.Instance.regions
                .Where(r => r.rulingFaction != faction && r.rulingFaction != null && !r.isAtWar)
                .OrderByDescending(r => CalculateStrategicRegionPriority(r, faction)) // Use the new, safe method
                .FirstOrDefault();

            if (targetRegion != null)
            {
                faction.currentFocus = StrategicFocus.Conquest;
                faction.strategicTargetRegion = targetRegion;
                Debug.Log($"<color=orange>Strategy for {faction.name}: CONQUEST of {targetRegion.tower.name}</color>");
            }
            else
            {
                // No good region to attack, so weaken the main enemy's army
                faction.currentFocus = StrategicFocus.Attrition;
                faction.strategicTargetFaction = primaryEnemy;
                Debug.Log($"<color=red>Strategy for {faction.name}: ATTRITION against {primaryEnemy.name}</color>");
            }
        }

        /// <summary>
        /// Calculates the strategic priority of a region for conquest. A higher score is better.
        /// This is a simplified version for high-level strategy, avoiding null references.
        /// </summary>
        private float CalculateStrategicRegionPriority(Voronoi_MapBorderController.Region targetRegion, RegionFactionDefinition attackingFaction)
        {
            if (targetRegion == null || attackingFaction == null) return 0;

            float priority = 0;

            // 1. High-value regions are prime targets
            priority += targetRegion.strategicValue * 100f;

            // 2. Weakly defended regions are easier to conquer
            int defenderCount = targetRegion.garrisonSoldiers.Count(s => s != null && s.fightingState != BattleSoldier.FightingState.Dead);
            if (defenderCount < minTowerDefenders)
            {
                priority += 150f; // Very high bonus for under-defended regions
            }
            priority -= defenderCount * 10f; // Penalty for each defender

            // 3. Adjacency is critical (Sun Tzu: "Do not attack an army on its home territory")
            bool isAdjacent = attackingFaction.controlledRegions.Any(r => r.IsAdjacent(targetRegion));
            if (isAdjacent)
            {
                priority += 200f; // Strongest bonus for adjacent regions
            }
            else
            {
                priority -= 300f; // Heavy penalty for non-adjacent targets to discourage scattered attacks
            }

            return priority;
        }

        /// <summary>
        /// Executes a faction's turn based on its current StrategicFocus.
        /// This is the new central command function.
        /// </summary>
        private void ExecuteClausewitzianStrategy(RegionFactionDefinition faction, Voronoi_MapBorderController.Region[] allRegions)
        {
            var allSoldiers = faction.GetAllLivingSoldiers();
            var enemySoldiers = factions.Where(f => f != faction && !f.isDefeated)
                                        .SelectMany(f => f.GetAllLivingSoldiers()).ToList();

            // Assign roles based on the grand strategy
            switch (faction.currentFocus)
            {
                case StrategicFocus.Consolidate:
                    // Defend all regions, prioritize borders.
                    AssignConsolidationRoles(faction, allSoldiers, allRegions);
                    break;

                case StrategicFocus.Attrition:
                    // Seek and destroy enemy units, prioritizing the target faction.
                    AssignAttritionRoles(faction, allSoldiers, enemySoldiers);
                    break;

                case StrategicFocus.Conquest:
                    // Focus forces on capturing the strategic target region.
                    AssignConquestRoles(faction, allSoldiers, allRegions);
                    break;
            }

            // Now, execute the assigned roles for each soldier
            foreach (var soldier in allSoldiers)
            {
                soldier.ExecuteCurrentRole();
            }
        }

        private void AssignConsolidationRoles(RegionFactionDefinition faction, List<Voronoi_ElectronSoldier> soldiers, Voronoi_MapBorderController.Region[] allRegions)
        {
            // Similar to old OrganizeRegionDefense but applied faction-wide
            foreach (var region in faction.controlledRegions)
            {
                OrganizeRegionDefense(region);
            }
        }

        private void AssignAttritionRoles(RegionFactionDefinition faction, List<Voronoi_ElectronSoldier> soldiers, List<Voronoi_ElectronSoldier> enemySoldiers)
        {
            // Sun Tzu: "The supreme art of war is to subdue the enemy without fighting."
            // This just means Prioritize weak, isolated, pussy enemies.
            var availableAttackers = new List<Voronoi_ElectronSoldier>(soldiers);

            // Ensure minimum defense first
            foreach (var region in faction.controlledRegions)
            {
                var defenders = availableAttackers.OrderBy(s => Vector3.Distance(s.position, region.tower.transform.position)).Take(minTowerDefenders).ToList();
                foreach (var defender in defenders)
                {
                    defender.AssignDefendTower(region, region.tower.transform.position + Random.insideUnitSphere * region.defenseRingRadius, -1);
                    availableAttackers.Remove(defender);
                }
            }

            // Use remaining soldiers to hunt, using the new targeting logic
            foreach (var attacker in availableAttackers)
            {
                var target = FindBestTargetFor(attacker, enemySoldiers);
                if (target != null)
                {
                    attacker.AssignAttackRegion(null, target);
                }
            }
        }

        private void AssignConquestRoles(RegionFactionDefinition faction, List<Voronoi_ElectronSoldier> soldiers, Voronoi_MapBorderController.Region[] allRegions)
        {
            if (faction.strategicTargetRegion == null || faction.strategicTargetRegion.rulingFaction == faction)
            {
                DetermineStrategicFocus(faction, factions.Where(f => f != faction).ToList()); // Pick a new target
                return;
            }

            var invasionForce = new List<Voronoi_ElectronSoldier>(soldiers);
            var targetRegion = faction.strategicTargetRegion;

            // Keep a minimal guard back in home regions
            foreach (var region in faction.controlledRegions)
            {
                var defendersToKeep = Mathf.Min(1, region.garrisonSoldiers.Count - 1); // Keep at least 1, if possible
                if (defendersToKeep > 0)
                {
                    var defenders = invasionForce.OrderBy(s => Vector3.Distance(s.position, region.tower.transform.position)).Take(defendersToKeep).ToList();
                    foreach (var defender in defenders)
                    {
                        defender.AssignDefendRegion(region);
                        invasionForce.Remove(defender);
                    }
                }
            }

            // All other units are dedicated to the invasion of the target region.
            var enemiesInTargetRegion = targetRegion.garrisonSoldiers
                .Where(e => e != null && e.fightingState != BattleSoldier.FightingState.Dead).ToList();

            foreach (var invader in invasionForce)
            {
                // Find the best target within the specific conquest region.
                var target = FindBestTargetFor(invader, enemiesInTargetRegion);
                invader.AssignAttackBorder(targetRegion, target);
            }
        }

        /// <summary>
        /// Finds the best enemy for a soldier to attack based on a threat priority system.
        /// </summary>
        /// <param name="soldier">The soldier seeking a target.</param>
        /// <param name="potentialEnemies">A list of enemies to evaluate.</param>
        /// <returns>The best target, or null if none are suitable.</returns>
        public Voronoi_ElectronSoldier FindBestTargetFor(Voronoi_ElectronSoldier soldier, List<Voronoi_ElectronSoldier> potentialEnemies)
        {
            if (soldier == null || potentialEnemies == null || potentialEnemies.Count == 0)
            {
                return null;
            }

            return potentialEnemies
                .Where(e => e != null && e.fightingState != BattleSoldier.FightingState.Dead && e.CurrentAttackersCount < maxAttackersPerTarget)
                .OrderByDescending(e => {
                    // Calculate a dynamic priority score for each potential target.
                    float score = 0;
                    float distance = Vector3.Distance(soldier.position, e.position);

                    // 1. Threat Level: Higher threat is much higher priority.
                    score += e.GetThreatLevel() * 10f;

                    // 2. Proximity: Closer targets are better.
                    score -= distance * 1.5f;

                    // 3. Health: Finishing off a weak enemy is a high priority.
                    if (e.health < e.maxHealth * 0.4f)
                    {
                        score += 20f;
                    }

                    // 4. Engagement: Prefer targets that are not already swarmed.
                    score -= e.CurrentAttackersCount * 15f;

                    return score;
                })
                .FirstOrDefault();
        }


        /// <summary>
        /// Execute a soldier's turn following the rules of engagement
        /// </summary>
        public void ExecuteSoldierTurn(Voronoi_ElectronSoldier soldier, RegionFactionDefinition myFaction,
            Voronoi_MapBorderController.Region currentRegion)
        {
            if (soldier == null || myFaction == null) return;

            var allRegions = Voronoi_MapBorderController.Instance.regions;

            // 1. Find potential enemy targets across all fronts
            List<EnemyTargetCandidate> enemyCandidates = GatherEnemyTargets(soldier, myFaction, currentRegion, allRegions);

            // 2. No valid targets found - move to strategic position
            if (enemyCandidates.Count == 0)
            {
                MoveToStrategicPosition(soldier, myFaction, currentRegion, allRegions);
                return;
            }

            // 3. Sort by priority (highest first) and select best target
            enemyCandidates.Sort((a, b) => b.priority.CompareTo(a.priority));
            var chosenCandidate = enemyCandidates[0];
            var chosenEnemy = chosenCandidate.soldier;
            var enemyFactionChosen = chosenCandidate.faction;

            // 4. Check if we need to maintain minimum tower defenders
            if (currentRegion != null && currentRegion.rulingFaction == myFaction)
            {
                int defendersNearTower = CountDefendersNearTower(currentRegion);
                if (defendersNearTower <= minTowerDefenders)
                {
                    // Force this soldier to defend the tower
                    DefendTower(soldier, currentRegion);
                    return;
                }
            }

            // 5. Assign soldier to chosen enemy
            soldier.myEnemy = chosenEnemy;
            chosenEnemy.AddAttacker(soldier);
            soldier.fightingState = BattleSoldier.FightingState.Attacking;

            // 6. If already in range, attempt immediate attack
            if (soldier.IsInRangeOf(chosenEnemy, soldier.attackRange))
            {
                soldier.TryAttack(chosenEnemy);
                return;
            }

            // 7. Not in range: compute engagement position with spread to avoid stacking
            int attackerIndex = chosenEnemy.CurrentAttackersCount - 1;
            attackerIndex = Mathf.Clamp(attackerIndex, 0, maxAttackersPerTarget - 1);

            float angleStep = 360f / Mathf.Max(1, maxAttackersPerTarget);
            float angle = angleStep * attackerIndex + Random.Range(-10f, 10f);
            float radius = Mathf.Max(1f, chosenEnemy.attackRange + (engagementSpacing * (0.5f + attackerIndex * 0.5f)));

            Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * radius;
            Vector3 engagePos = chosenEnemy.position + offset;

            soldier.SetTargetPosition(engagePos);
            Debug.Log($"{myFaction.name}'s {soldier.rank} engaging {enemyFactionChosen.name}'s {chosenEnemy.rank} at distance {chosenCandidate.distance:F1}");
        }

        /// <summary>
        /// Gather all potential enemy targets for a soldier
        /// </summary>
        private List<EnemyTargetCandidate> GatherEnemyTargets(Voronoi_ElectronSoldier soldier,
            RegionFactionDefinition myFaction, Voronoi_MapBorderController.Region myRegion,
            Voronoi_MapBorderController.Region[] allRegions)
        {
            List<EnemyTargetCandidate> candidates = new List<EnemyTargetCandidate>();

            foreach (var region in allRegions)
            {
                if (region == null || region.rulingFaction == null) continue;
                if (region.rulingFaction == myFaction) continue; // Skip own regions

                // Check garrison soldiers
                foreach (var enemy in region.garrisonSoldiers)
                {
                    if (enemy == null || enemy.fightingState == BattleSoldier.FightingState.Dead) continue;
                    if (enemy.CurrentAttackersCount >= maxAttackersPerTarget) continue;

                    float distance = Vector3.Distance(soldier.position, enemy.position);
                    if (distance > engagementSearchRadius) continue;

                    float priority = CalculateTargetPriority(soldier, enemy, region.rulingFaction, myRegion, allRegions, distance);

                    candidates.Add(new EnemyTargetCandidate
                    {
                        soldier = enemy,
                        faction = region.rulingFaction,
                        distance = distance,
                        priority = priority
                    });
                }

                // Check invading soldiers in our regions
                foreach (var enemy in region.invadingSoldiers)
                {
                    if (enemy == null || enemy.fightingState == BattleSoldier.FightingState.Dead) continue;
                    if (enemy.CurrentAttackersCount >= maxAttackersPerTarget) continue;

                    // Find enemy faction
                    RegionFactionDefinition enemyFaction = factions.Find(f => f.faction == enemy.soldierFaction);
                    if (enemyFaction == myFaction) continue;

                    float distance = Vector3.Distance(soldier.position, enemy.position);
                    if (distance > engagementSearchRadius) continue;

                    float priority = CalculateTargetPriority(soldier, enemy, enemyFaction, myRegion, allRegions, distance);

                    candidates.Add(new EnemyTargetCandidate
                    {
                        soldier = enemy,
                        faction = enemyFaction,
                        distance = distance,
                        priority = priority
                    });
                }
            }

            return candidates;
        }

        /// <summary>
        /// Calculate strategic priority for targeting a specific enemy
        /// Higher priority = better target
        /// Follows rules of engagement
        /// </summary>
        private float CalculateTargetPriority(Voronoi_ElectronSoldier mySoldier, Voronoi_ElectronSoldier enemySoldier,
            RegionFactionDefinition enemyFaction, Voronoi_MapBorderController.Region myRegion,
            Voronoi_MapBorderController.Region[] allRegions, float distance)
        {
            float priority = 0f;

            // 1. Proximity bonus (closer = higher priority, with diminishing returns)
            float proximityScore = 100f / Mathf.Max(1f, distance);
            priority += proximityScore * 2f;

            // 2. Bonus for enemies within engagement search radius
            if (distance <= engagementSearchRadius)
            {
                priority += 50f;
            }

            // 3. Rank-based threat assessment (higher rank enemies = higher priority)
            priority += (int)enemySoldier.rank * 5f;

            // 4. Bonus for enemies with fewer attackers (spread forces more evenly)
            int attackerGap = maxAttackersPerTarget - enemySoldier.CurrentAttackersCount;
            priority += attackerGap * 15f;

            // 5. Regional strategic bonus
            if (myRegion != null && allRegions != null)
            {
                Voronoi_MapBorderController.Region enemyRegion = FindSoldierRegion(enemySoldier, allRegions);

                if (enemyRegion != null)
                {
                    // Same region = highest priority (defend home or attack invaders)
                    if (enemyRegion == myRegion)
                    {
                        priority += 100f;
                    }
                    // Adjacent region = high priority (border conflicts)
                    else if (myRegion.IsAdjacent(enemyRegion))
                    {
                        priority += 60f;
                    }
                    // Enemy in high-value region = moderate priority
                    else if (enemyRegion.strategicValue > 1.5f)
                    {
                        priority += 30f;
                    }
                }
            }

            // 6. Health-based targeting (prioritize weakened enemies for quick kills)
            float healthRatio = enemySoldier.health / (float)enemySoldier.maxHealth;
            if (healthRatio < 0.3f)
            {
                priority += 40f; // finishing blow bonus
            }

            return priority;
        }

        /// <summary>
        /// Find which region a soldier is currently in
        /// </summary>
        private Voronoi_MapBorderController.Region FindSoldierRegion(Voronoi_ElectronSoldier soldier,
            Voronoi_MapBorderController.Region[] allRegions)
        {
            if (soldier == null || allRegions == null) return null;

            foreach (var region in allRegions)
            {
                if (region == null || region.tower == null) continue;

                // Check garrison soldiers
                if (region.garrisonSoldiers.Contains(soldier))
                {
                    return region;
                }

                // Check invading soldiers
                if (region.invadingSoldiers.Contains(soldier))
                {
                    return region;
                }

                // Fallback: proximity check to tower
                float dist = Vector3.Distance(soldier.position, region.tower.transform.position);
                if (dist <= region.actualBoundaryRadius)
                {
                    return region;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds all living, friendly soldiers within a given radius of a soldier.
        /// </summary>
        public List<Voronoi_ElectronSoldier> FindNearbyAllies(Voronoi_ElectronSoldier soldier, float radius)
        {
            List<Voronoi_ElectronSoldier> allies = new List<Voronoi_ElectronSoldier>();
            if (soldier == null) return allies;

            var faction = factions.FirstOrDefault(f => f.faction == soldier.soldierFaction);
            if (faction == null) return allies;

            float radiusSqr = radius * radius;

            foreach (var potentialAlly in faction.GetAllLivingSoldiers())
            {
                if (potentialAlly == soldier) continue;

                if (Vector3.SqrMagnitude(soldier.position - potentialAlly.position) <= radiusSqr)
                {
                    allies.Add(potentialAlly);
                }
            }
            return allies;
        }

        /// <summary>
        /// Finds the closest friendly tower for a soldier to retreat to.
        /// </summary>
        public Voronoi_Tower FindClosestFriendlyTower(Voronoi_ElectronSoldier soldier)
        {
            if (soldier == null) return null;

            var friendlyFaction = factions.FirstOrDefault(f => f.faction == soldier.soldierFaction);
            if (friendlyFaction == null) return null;

            Voronoi_Tower closestTower = null;
            float minDistance = float.MaxValue;

            foreach (var region in friendlyFaction.controlledRegions)
            {
                if (region == null || region.tower == null) continue;

                float distance = Vector3.Distance(soldier.position, region.tower.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestTower = region.tower;
                }
            }

            return closestTower;
        }

        /// <summary>
        /// Count defenders near tower (within defense radius)
        /// </summary>
        private int CountDefendersNearTower(Voronoi_MapBorderController.Region region)
        {
            if (region == null || region.tower == null) return 0;

            int count = 0;
            Vector3 towerPos = region.tower.transform.position;

            foreach (var soldier in region.garrisonSoldiers)
            {
                if (soldier == null || soldier.fightingState == BattleSoldier.FightingState.Dead) continue;

                float dist = Vector3.Distance(soldier.position, towerPos);
                if (dist <= towerDefenseRadius)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Force soldier to defend the tower
        /// </summary>
        private void DefendTower(Voronoi_ElectronSoldier soldier, Voronoi_MapBorderController.Region region)
        {
            if (soldier == null || region == null || region.tower == null) return;

            soldier.fightingState = BattleSoldier.FightingState.Defending;
            soldier.myEnemy = null;

            // Position soldier in defensive ring around tower
            Vector3 towerPos = region.tower.transform.position;
            Vector3 randomOffset = Random.insideUnitSphere.normalized * region.defenseRingRadius;
            randomOffset.y = 0;

            soldier.SetTargetPosition(towerPos + randomOffset);
        }

        /// <summary>
        /// Move soldier to a strategic position
        /// Follows rules of engagement priority system
        /// </summary>
        private void MoveToStrategicPosition(Voronoi_ElectronSoldier soldier, RegionFactionDefinition myFaction,
    Voronoi_MapBorderController.Region currentRegion, Voronoi_MapBorderController.Region[] allRegions)
        {
            if (currentRegion == null || allRegions == null) return;

            // Priority 1: If already assigned to invade, move to invasion target
            foreach (var region in allRegions)
            {
                if (region != null && region.invadingSoldiers.Contains(soldier) && region.tower != null)
                {
                    // Head towards enemy tower aggressively
                    Vector3 attackPos = region.tower.transform.position +
                        Random.insideUnitSphere.normalized * region.defenseRingRadius * 0.8f;
                    attackPos.y = soldier.position.y;
                    soldier.SetTargetPosition(attackPos);
                    soldier.fightingState = BattleSoldier.FightingState.Attacking;
                    return;
                }
            }

            // Priority 2: Defend critical border regions
            foreach (var region in allRegions)
            {
                if (region == null || region.rulingFaction != myFaction) continue;

                if (region.isBorderRegion && region.adjacentEnemyRegions > 0)
                {
                    int defendersNearTower = CountDefendersNearTower(region);
                    if (defendersNearTower < minTowerDefenders && region.tower != null)
                    {
                        Vector3 defensePos = region.tower.transform.position +
                            Random.insideUnitSphere.normalized * region.defenseRingRadius;
                        defensePos.y = soldier.position.y;
                        soldier.SetTargetPosition(defensePos);
                        soldier.fightingState = BattleSoldier.FightingState.Defending;
                        Debug.Log($"{soldier.soldierName} moving to defend border region at {region.tower.name}");
                        return;
                    }
                }
            }

            // Priority 3: AGGRESSIVE - Move to border and prepare for attack
            if (aggressionLevel > 0.4f)
            {
                var adjacentEnemyRegions = allRegions.Where(r =>
                    r != null &&
                    r.rulingFaction != myFaction &&
                    currentRegion != null &&
                    currentRegion.IsAdjacent(r) &&
                    !r.isAtWar // Don't overcrowd existing battles
                ).OrderByDescending(r => r.strategicValue).ToList();

                if (adjacentEnemyRegions.Count > 0)
                {
                    var targetRegion = adjacentEnemyRegions[0];
                    if (targetRegion.tower != null)
                    {
                        // Position aggressively at the border
                        Vector3 attackPos = targetRegion.tower.transform.position +
                            (currentRegion.tower.transform.position - targetRegion.tower.transform.position).normalized *
                            targetRegion.defenseRingRadius * 1.2f;
                        attackPos.y = soldier.position.y;
                        soldier.SetTargetPosition(attackPos);
                        soldier.fightingState = BattleSoldier.FightingState.Moving;
                        return;
                    }
                }
            }

            // Priority 4: Move to high-value regions within own territory
            var highValueRegions = myFaction.controlledRegions
                .Where(r => r != null && r.strategicValue > 1.5f && r.tower != null)
                .OrderByDescending(r => r.strategicValue).ToList();

            if (highValueRegions.Count > 0)
            {
                var targetRegion = highValueRegions[0];
                Vector3 patrolPos = targetRegion.tower.transform.position +
                    Random.insideUnitSphere.normalized * targetRegion.actualBoundaryRadius * 0.6f;
                patrolPos.y = soldier.position.y;
                soldier.SetTargetPosition(patrolPos);
                soldier.fightingState = BattleSoldier.FightingState.Moving;
                return;
            }

            // Priority 5: Patrol within current region
            if (currentRegion != null && currentRegion.tower != null)
            {
                Vector3 patrolPos = currentRegion.tower.transform.position +
                    Random.insideUnitSphere.normalized * currentRegion.actualBoundaryRadius * 0.5f;
                patrolPos.y = soldier.position.y;
                soldier.SetTargetPosition(patrolPos);
                soldier.fightingState = BattleSoldier.FightingState.Moving;
            }
        }

        /// <summary>
        /// Helper class to store enemy target candidates with their evaluation metrics
        /// </summary>
        private class EnemyTargetCandidate
        {
            public Voronoi_ElectronSoldier soldier;
            public RegionFactionDefinition faction;
            public float distance;
            public float priority;
        }

        /// <summary>
        /// Check for victory condition according to rules of engagement
        /// War ends when one faction controls all regions
        /// </summary>
        public void CheckForVictory()
        {
            if (factions.Count < 2) return;

            var activeFactions = factions.Where(f => !f.isDefeated && f.GetLivingSoldierCount() > 0).ToList();

            if (activeFactions.Count <= 1)
            {
                if (activeFactions.Count == 1)
                {
                    Debug.Log($"<color=green>VICTORY! {activeFactions[0].name} has won the war!</color>");
                }
                else
                {
                    Debug.Log("<color=yellow>War ended in mutual destruction!</color>");
                }

                CompleteBattle();
            }

            // Check region control victory
            var regionsControlled = new Dictionary<RegionFactionDefinition, int>();
            var allRegions = Voronoi_MapBorderController.Instance.regions;

            foreach (var region in allRegions)
            {
                if (region == null || region.rulingFaction == null) continue;

                if (!regionsControlled.ContainsKey(region.rulingFaction))
                {
                    regionsControlled[region.rulingFaction] = 0;
                }
                regionsControlled[region.rulingFaction]++;
            }

            // Check if any faction controls all regions
            foreach (var kvp in regionsControlled)
            {
                if (kvp.Value == allRegions.Length)
                {
                    Debug.Log($"<color=green>TOTAL VICTORY! {kvp.Key.name} controls all {kvp.Value} regions!</color>");
                    CompleteBattle();
                    return;
                }
            }
        }

        public void StartBattle()
        {
            currentBattleState = BattleState.InProgress;
            Debug.Log("<color=cyan>Battle has begun!</color>");
        }

        public void PauseBattle()
        {
            currentBattleState = BattleState.Paused;
        }

        public void ResumeBattle()
        {
            currentBattleState = BattleState.InProgress;
        }

        public void CompleteBattle()
        {
            currentBattleState = BattleState.Completed;
            Debug.Log("<color=cyan>Battle has ended!</color>");
        }

        /// <summary>
        /// Evaluate battlefield state according to rules of engagement
        /// Check for defeated factions and victory conditions
        /// </summary>
        public void EvaluateBattlefield()
        {
            // Update faction defeat status based on living soldiers
            foreach (var faction in factions)
            {
                if (faction == null) continue;

                int livingSoldiers = faction.GetLivingSoldierCount();
                if (livingSoldiers == 0 && !faction.isDefeated)
                {
                    faction.isDefeated = true;
                    Debug.Log($"<color=red>{faction.name} has been defeated!</color>");
                }
            }

            // Count active factions
            int activeFactions = factions.Count(f => !f.isDefeated);

            // Check if minimum faction requirement is violated
            if (activeFactions < 2)
            {
                CheckForVictory();
            }
        }

        /// <summary>
        /// Report soldier death and handle attacker reassignment
        /// Implements rules: remaining attackers are recruited to victorious faction after region loss
        /// </summary>
        public void ReportSoldierDeath(Voronoi_ElectronSoldier deadSoldier)
        {
            if (deadSoldier == null) return;

            // Find owning faction
            RegionFactionDefinition owner = factions.Find(f => f != null && f.faction == deadSoldier.soldierFaction);

            if (owner == null)
            {
                Debug.LogWarning("ReportSoldierDeath: owner faction not found.");
                return;
            }

            // Before removing the dead from heap, reassign its attackers (they are now free)
            var attackersSnapshot = new List<Voronoi_ElectronSoldier>(deadSoldier.currentAttackers);
            foreach (var attacker in attackersSnapshot)
            {
                if (attacker == null) continue;
                // Remove attacker reference to dead target
                deadSoldier.RemoveAttacker(attacker);
                attacker.myEnemy = null;
                attacker.fightingState = BattleSoldier.FightingState.Idle;
            }

            // Remove dead soldier's node from the heap
            if (owner.playerCommandQueue != null && owner.playerCommandQueue.heapSize > 0)
            {
                int foundIndex = -1;
                for (int i = 0; i < owner.playerCommandQueue.heapSize; i++)
                {
                    var node = owner.playerCommandQueue.heapNodes[i];
                    if (node == null) continue;
                    if (node.gameObject == deadSoldier.gameObject)
                    {
                        foundIndex = i;
                        break;
                    }
                }

                if (foundIndex >= 0)
                {
                    int last = owner.playerCommandQueue.heapSize - 1;
                    owner.playerCommandQueue.heapNodes[foundIndex] = owner.playerCommandQueue.heapNodes[last];
                    owner.playerCommandQueue.heapNodes[last] = null;
                    owner.playerCommandQueue.heapSize--;

                    // Restore heap property
                    try
                    {
                        owner.playerCommandQueue.BuildMaxHeap();
                    }
                    catch
                    {
                        try
                        {
                            owner.playerCommandQueue.Heapify();
                        }
                        catch { }
                    }
                }
            }

            // Reorganize the heap relationships
            owner.OrganizeHeapHierarchy();

            // Check if this death triggers a region loss
            CheckRegionControl(deadSoldier);
        }

        /// <summary>
        /// Check if soldier death affects region control
        /// Implements rule: defeated faction's region changes control, attackers are recruited
        /// </summary>
        private void CheckRegionControl(Voronoi_ElectronSoldier deadSoldier)
        {
            var regions = Voronoi_MapBorderController.Instance.regions;

            foreach (var region in regions)
            {
                if (region == null || region.rulingFaction == null) continue;

                // Check if this was a garrison soldier
                if (region.garrisonSoldiers.Contains(deadSoldier))
                {
                    region.garrisonSoldiers.Remove(deadSoldier);

                    // Check if garrison is completely wiped out
                    int livingDefenders = region.garrisonSoldiers.Count(s =>
                        s != null && s.fightingState != BattleSoldier.FightingState.Dead);

                    if (livingDefenders == 0 && region.invadingSoldiers.Count > 0)
                    {
                        // Region conquered! Implement recruitment rule
                        var invader = region.invadingSoldiers[0];
                        var invadingFaction = factions.Find(f => f.faction == invader.soldierFaction);

                        if (invadingFaction != null)
                        {
                            Debug.Log($"<color=yellow>{region.rulingFaction.name}'s region conquered by {invadingFaction.name}! Remaining defenders recruited.</color>");

                            // Remove from old faction's controlled regions
                            if (region.rulingFaction.controlledRegions.Contains(region))
                            {
                                region.rulingFaction.controlledRegions.Remove(region);
                            }

                            // Change region control
                            //ChangeRulingFaction(invadingFaction);
                            region.rulingFaction = invadingFaction;
                            // Add to new faction's controlled regions
                            if (!invadingFaction.controlledRegions.Contains(region))
                            {
                                invadingFaction.controlledRegions.Add(region);
                            }
                        }
                    }
                }

                // Check if this was an invading soldier
                if (region.invadingSoldiers.Contains(deadSoldier))
                {
                    region.invadingSoldiers.Remove(deadSoldier);
                }
            }
        }

        /// <summary>
        /// Initialize all regions with their factions and spawn garrisons
        /// Call this after map generation is complete
        /// </summary>
        public void InitializeRegionFactions()
        {
            var map = Voronoi_MapBorderController.Instance;

            if (map == null || map.regions == null || map.regions.Length < 2)
            {
                Debug.LogError("Cannot initialize factions: Invalid map or insufficient regions!");
                return;
            }

            Debug.Log("Initializing region factions and garrisons...");

            for (int i = 0; i < map.regions.Length; i++)
            {
                var region = map.regions[i];

                if (region == null)
                {
                    Debug.LogError($"Region {i} is null, skipping faction initialization.");
                    continue;
                }

                if (region.nativeFaction == null)
                {
                    Debug.LogError($"Region {i} has no native faction assigned, skipping garrison spawn.");
                    continue;
                }

                // Validate soldier prefab configuration
                if (region.nativeFaction.privateSoldierPrefab == null ||
                    region.nativeFaction.privateSoldierPrefab.soldierPrefab == null)
                {
                    Debug.LogError($"Region {i}: Invalid soldier prefab configuration!");
                    continue;
                }

                // Initialize ruling faction
                region.rulingFaction = region.nativeFaction;
                region.territoryColor = region.GetFactionColor(region.nativeFaction.faction);
                region.controlStrength = 1f;

                // Spawn garrison for this region (THIS IS THE ONLY PLACE SOLDIERS ARE SPAWNED)
                SpawnGarrisonForRegion(region, region.nativeFaction, region.maxGarrisonSize);

                Debug.Log($"Region {i} initialized: Faction={region.rulingFaction.name}, Garrison={region.garrisonSoldiers.Count}");
            }

            // Initialize factions list
            InitialiseFactions();

            Debug.Log($"<color=green>Faction initialization complete. Total regions: {map.regions.Length}</color>");
        }

        /// <summary>
        /// Spawn garrison soldiers for a specific region
        /// </summary>
        private void SpawnGarrisonForRegion(Voronoi_MapBorderController.Region region,
            RegionFactionDefinition faction, int count)
        {
            if (region.tower == null)
            {
                Debug.LogError("SpawnGarrisonForRegion: Tower is null!");
                return;
            }

            Vector3 towerPos = region.tower.transform.position;
            float angleStep = 360f / count;

            // Create spawn points in a circle around the tower AT GROUND LEVEL
            Transform[] spawnPoints = new Transform[count];
            for (int i = 0; i < count; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * region.defenseRingRadius,
                    0f, // Keep at tower Y level (ground level)
                    Mathf.Sin(angle) * region.defenseRingRadius
                );

                GameObject spawnPoint = new GameObject($"SpawnPoint_{faction.name}_{i}");
                spawnPoint.transform.position = towerPos + offset;
                spawnPoints[i] = spawnPoint.transform;
            }

            // Update maxRecruits to match spawn points
            int originalMaxRecruits = faction.privateSoldierPrefab.maxRecruits;
            faction.privateSoldierPrefab.maxRecruits = count;

            // Recruit soldiers


            faction.privateSoldierPrefab.AssignSpawnpoints(spawnPoints);
            var recruited_privates = faction.privateSoldierPrefab.RecruitSoldiers();

            foreach (var soldier in recruited_privates)
            {
                if (soldier == null) continue;

                // Set faction
                soldier.soldierFaction = faction.faction;
                soldier.SetFactionMaterial(region.flagMaterial);

                // Add to command queue heap
                var soldier_node = soldier.GetComponent<NodeBehavior>();
                if (soldier_node != null)
                {
                    soldier_node.priority = (int)soldier.rank;
                    faction.playerCommandQueue.Insert(soldier_node);
                }

                soldier.fightingState = BattleSoldier.FightingState.Idle; // Start idle, not moving
                // Add to region garrison
                if (!region.garrisonSoldiers.Contains(soldier))
                    region.garrisonSoldiers.Add(soldier);
            }

            // Restore original maxRecruits
            faction.privateSoldierPrefab.maxRecruits = originalMaxRecruits;

            // Cleanup spawn points
            foreach (var sp in spawnPoints)
            {
                if (sp != null) GameObject.Destroy(sp.gameObject);
            }

            Debug.Log($"<color=green>Garrison spawned: {region.garrisonSoldiers.Count} soldiers for {faction.name} in {region.tower.name}</color>");
        }

        /// <summary>
        /// Organize region defense according to rules of engagement
        /// Forms Voronoi boundary around tower with minimum defenders
        /// </summary>
        public void OrganizeRegionDefense(Voronoi_MapBorderController.Region region)
        {
            if (region == null || region.rulingFaction == null || region.tower == null) return;

            var availableSoldiers = region.garrisonSoldiers
                .Where(s => s != null && s.fightingState != BattleSoldier.FightingState.Dead)
                .OrderByDescending(s => s.rank) // Higher ranks defend tower
                .ToList();

            if (availableSoldiers.Count == 0) return;

            // 1. Assign minimum tower defenders (Voronoi boundary)
            int towerDefenderCount = Mathf.Min(minTowerDefenders, availableSoldiers.Count);
            List<Vector3> defensePositions = GenerateVoronoiDefenseBoundary(region, towerDefenderCount);

            for (int i = 0; i < towerDefenderCount && i < availableSoldiers.Count; i++)
            {
                availableSoldiers[i].AssignDefendTower(region, defensePositions[i], i);
            }

            Debug.Log($"<color=cyan>Assigned {towerDefenderCount} tower defenders to {region.tower.name}</color>");

            // 2. Assign remaining soldiers based on threat level
            int remainingSoldiers = availableSoldiers.Count - towerDefenderCount;

            if (remainingSoldiers > 0)
            {
                // Check for invaders
                if (region.invadingSoldiers.Count > 0)
                {
                    // Assign region defenders to intercept
                    for (int i = towerDefenderCount; i < availableSoldiers.Count; i++)
                    {
                        availableSoldiers[i].AssignDefendRegion(region);
                    }
                    Debug.Log($"<color=yellow>{remainingSoldiers} soldiers assigned to intercept invaders</color>");
                }
                else if (region.isBorderRegion && aggressionLevel > 0.5f)
                {
                    // Assign border attackers if aggressive and no threats
                    AssignBorderAttackers(region.rulingFaction, region, availableSoldiers.Skip(towerDefenderCount).ToList());
                }
                else
                {
                    // Default: patrol defense
                    for (int i = towerDefenderCount; i < availableSoldiers.Count; i++)
                    {
                        availableSoldiers[i].AssignDefendRegion(region);
                    }
                }
            }
        }



        /// <summary>
        /// Generate Voronoi boundary positions around tower for defenders
        /// </summary>
        private List<Vector3> GenerateVoronoiDefenseBoundary(Voronoi_MapBorderController.Region region, int defenderCount)
        {
            List<Vector3> positions = new List<Vector3>();
            if (region == null || region.tower == null) return positions;

            Vector3 towerPos = region.tower.transform.position;
            float angleStep = 360f / defenderCount;

            for (int i = 0; i < defenderCount; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * region.defenseRingRadius,
                    0f,
                    Mathf.Sin(angle) * region.defenseRingRadius
                );

                positions.Add(towerPos + offset);
            }

            return positions;
        }

        /// <summary>
        /// Pop attackers from defense queue and assign to offensive operations
        /// Implements rule: "attackers should be popped from the queue and the defence line redrawn"
        /// </summary>
        private void AssignBorderAttackers(RegionFactionDefinition faction, Voronoi_MapBorderController.Region sourceRegion,
            List<Voronoi_ElectronSoldier> availableSoldiers)
        {
            if (faction == null || sourceRegion == null || availableSoldiers.Count == 0) return;

            var allRegions = Voronoi_MapBorderController.Instance.regions;

            // Find adjacent enemy regions
            var adjacentEnemyRegions = allRegions
                .Where(r => r != null && r.rulingFaction != faction && sourceRegion.IsAdjacent(r))
                .OrderByDescending(r => r.strategicValue)
                .ToList();

            if (adjacentEnemyRegions.Count == 0) return;

            var targetRegion = adjacentEnemyRegions[0];

            foreach (var soldier in availableSoldiers)
            {
                soldier.AssignAttackBorder(targetRegion);
            }

            Debug.Log($"<color=orange>{availableSoldiers.Count} soldiers popped from defense to attack {targetRegion.tower.name}</color>");
        }

        /// <summary>
        /// Invade a region with attacking soldiers
        /// </summary>
        public void InvadeRegion(Voronoi_MapBorderController.Region region,
            List<Voronoi_ElectronSoldier> invaders, RegionFactionDefinition invadingFaction)
        {
            if (region == null || invaders == null || invaders.Count == 0) return;
            if (invadingFaction == region.rulingFaction) return;

            region.isAtWar = true;
            region.invadingSoldiers.AddRange(invaders);
            region.totalBattles++;
            region.RecordBattleTime();
            region.SetBattleEffect(true);

            Debug.Log($"{invadingFaction.name} invades {region.rulingFaction.name}'s territory with {invaders.Count} soldiers!");

            AssignDefendersToInvaders(region);
        }

        /// <summary>
        /// Assign garrison defenders to engage invading soldiers
        /// </summary>
        private void AssignDefendersToInvaders(Voronoi_MapBorderController.Region region)
        {
            int defenderIndex = 0;

            foreach (var invader in region.invadingSoldiers)
            {
                if (invader == null || invader.fightingState == BattleSoldier.FightingState.Dead) continue;

                // Assign up to 2 defenders per invader
                for (int i = 0; i < 2 && defenderIndex < region.garrisonSoldiers.Count; i++)
                {
                    var defender = region.garrisonSoldiers[defenderIndex];
                    if (defender != null && defender.fightingState != BattleSoldier.FightingState.Dead)
                    {
                        defender.myEnemy = invader;
                        defender.fightingState = BattleSoldier.FightingState.Attacking;
                        defender.SetTargetPosition(invader.position);
                        invader.AddAttacker(defender);
                    }
                    defenderIndex++;
                }
            }
        }

        /// <summary>
        /// Recruit defeated faction's soldiers to victorious faction
        /// Implements rule: "remaining native attackers are thus recruited to the ruling faction"
        /// </summary>
        private void RecruitDefeatedSoldiers(Voronoi_MapBorderController.Region region, RegionFactionDefinition newFaction)
        {
            if (region == null || newFaction == null) return;

            List<Voronoi_ElectronSoldier> recruitedSoldiers = new List<Voronoi_ElectronSoldier>();

            // Recruit surviving garrison (now defeated defenders)
            foreach (var soldier in region.garrisonSoldiers)
            {
                if (soldier != null && soldier.fightingState != BattleSoldier.FightingState.Dead)
                {
                    RecruitSoldier(soldier, newFaction);
                    recruitedSoldiers.Add(soldier);
                }
            }

            // Convert invaders to garrison
            region.garrisonSoldiers.Clear();
            region.garrisonSoldiers.AddRange(region.invadingSoldiers);
            region.garrisonSoldiers.AddRange(recruitedSoldiers);
            region.invadingSoldiers.Clear();

            Debug.Log($"<color=lime>Recruited {recruitedSoldiers.Count} soldiers to {newFaction.name}</color>");

            // Reset command hierarchy for new faction
            ResetCommandHierarchy(newFaction);

            // Reorganize defense with new soldiers
            OrganizeRegionDefense(region);
        }

        /// <summary>
        /// Recruit a soldier to a new faction and update heap
        /// </summary>
        private void RecruitSoldier(Voronoi_ElectronSoldier soldier, RegionFactionDefinition newFaction)
        {
            if (soldier == null || newFaction == null) return;

            // Remove from old faction heap
            var oldFaction = factions.Find(f => f.faction == soldier.soldierFaction);
            if (oldFaction != null && oldFaction.playerCommandQueue != null)
            {
                RemoveSoldierFromHeap(oldFaction, soldier);
            }

            // Update soldier faction
            soldier.soldierFaction = newFaction.faction;
            soldier.ClearTacticalRole();
            soldier.myEnemy = null;
            soldier.fightingState = BattleSoldier.FightingState.Idle;

            // Add to new faction heap
            var soldierNode = soldier.GetComponent<NodeBehavior>();
            if (soldierNode != null)
            {
                soldierNode.priority = (int)soldier.rank;
                newFaction.playerCommandQueue.Insert(soldierNode);
            }

            Debug.Log($"<color=cyan>{soldier.soldierName} recruited from {oldFaction?.name} to {newFaction.name}</color>");
        }

        /// <summary>
        /// Remove soldier from faction's command heap
        /// </summary>
        private void RemoveSoldierFromHeap(RegionFactionDefinition faction, Voronoi_ElectronSoldier soldier)
        {
            if (faction?.playerCommandQueue == null || soldier == null) return;

            int foundIndex = -1;
            for (int i = 0; i < faction.playerCommandQueue.heapSize; i++)
            {
                var node = faction.playerCommandQueue.heapNodes[i];
                if (node != null && node.gameObject == soldier.gameObject)
                {
                    foundIndex = i;
                    break;
                }
            }

            if (foundIndex >= 0)
            {
                int last = faction.playerCommandQueue.heapSize - 1;
                faction.playerCommandQueue.heapNodes[foundIndex] = faction.playerCommandQueue.heapNodes[last];
                faction.playerCommandQueue.heapNodes[last] = null;
                faction.playerCommandQueue.heapSize--;

                try
                {
                    faction.playerCommandQueue.BuildMaxHeap();
                }
                catch
                {
                    try { faction.playerCommandQueue.Heapify(); } catch { }
                }
            }
        }

        /// <summary>
        /// Reset command hierarchy after recruitment
        /// </summary>
        private void ResetCommandHierarchy(RegionFactionDefinition faction)
        {
            if (faction == null || faction.playerCommandQueue == null) return;

            faction.OrganizeHeapHierarchy();
            Debug.Log($"<color=yellow>Command hierarchy reset for {faction.name}</color>");
        }

        /// <summary>
        /// Update battle state for a region
        /// </summary>
        public void UpdateRegionBattleState(Voronoi_MapBorderController.Region region)
        {
            if (region == null || !region.isAtWar) return;

            // Clean up dead soldiers
            region.garrisonSoldiers.RemoveAll(s => s == null || s.fightingState == BattleSoldier.FightingState.Dead);
            region.invadingSoldiers.RemoveAll(s => s == null || s.fightingState == BattleSoldier.FightingState.Dead);

            // Calculate battle intensity
            int totalCombatants = region.garrisonSoldiers.Count + region.invadingSoldiers.Count;
            region.battleIntensity = Mathf.Clamp01(totalCombatants / (float)region.maxGarrisonSize);

            // Check battle outcome
            if (region.garrisonSoldiers.Count == 0 && region.invadingSoldiers.Count > 0)
            {
                // Defenders eliminated - region conquered
                var invader = region.invadingSoldiers[0];
                var invadingFaction = factions.Find(f => f.faction == invader.soldierFaction);

                if (invadingFaction != null)
                {
                    ChangeRegionControl(region, invadingFaction);
                }
            }
            else if (region.invadingSoldiers.Count == 0 && region.garrisonSoldiers.Count > 0)
            {
                // Invaders eliminated - successful defense
                EndRegionBattle(region, true);
            }
            else if (region.garrisonSoldiers.Count == 0 && region.invadingSoldiers.Count == 0)
            {
                // Mutual annihilation
                EndRegionBattle(region, false);
            }
        }

        /// <summary>
        /// Change ruling faction after successful invasion
        /// </summary>
        private void ChangeRegionControl(Voronoi_MapBorderController.Region region, RegionFactionDefinition newFaction)
        {
            if (newFaction == null || region == null) return;

            RegionFactionDefinition previousFaction = region.rulingFaction;

            // Remove from old faction's controlled regions
            if (previousFaction != null && previousFaction.controlledRegions.Contains(region))
            {
                previousFaction.controlledRegions.Remove(region);
            }

            // Update region
            region.rulingFaction = newFaction;
            region.territoryColor = region.GetFactionColor(newFaction.faction);
            region.timesConquered++;
            region.controlStrength = 0.3f;

            // RECRUIT defeated soldiers (implements rules of engagement)
            RecruitDefeatedSoldiers(region, newFaction);

            // Add to new faction's controlled regions
            if (!newFaction.controlledRegions.Contains(region))
            {
                newFaction.controlledRegions.Add(region);
            }

            region.isAtWar = false;

            Debug.Log($"<color=yellow>Region conquered! {previousFaction?.name ?? "Unknown"} -> {newFaction.name}</color>");
        }

        /// <summary>
        /// End battle and restore peace
        /// </summary>
        private void EndRegionBattle(Voronoi_MapBorderController.Region region, bool defenseSuccessful)
        {
            if (region == null) return;

            region.isAtWar = false;
            region.battleIntensity = 0f;
            region.SetBattleEffect(false);

            if (defenseSuccessful)
            {
                region.successfulDefenses++;
                region.controlStrength = Mathf.Clamp01(region.controlStrength + 0.1f);
                Debug.Log($"{region.rulingFaction.name} successfully defended the region!");
            }

            region.invadingSoldiers.Clear();
        }

        /// <summary>
        /// Reinforce garrison with additional soldiers
        /// </summary>
        public void ReinforceRegionGarrison(Voronoi_MapBorderController.Region region,
            List<Voronoi_ElectronSoldier> reinforcements)
        {
            if (region == null || reinforcements == null) return;

            int spaceAvailable = region.maxGarrisonSize - region.garrisonSoldiers.Count;
            int toAdd = Mathf.Min(reinforcements.Count, spaceAvailable);

            for (int i = 0; i < toAdd; i++)
            {
                var soldier = reinforcements[i];
                if (soldier != null)
                {
                    region.garrisonSoldiers.Add(soldier);
                    soldier.fightingState = BattleSoldier.FightingState.Defending;

                    if (region.tower != null)
                    {
                        soldier.MoveTo(region.tower.transform.position + Random.insideUnitSphere * region.defenseRingRadius);
                    }
                }
            }

            Debug.Log($"Region reinforced with {toAdd} soldiers. Garrison: {region.garrisonSoldiers.Count}/{region.maxGarrisonSize}");
        }

        /// <summary>
        /// Rebuild military after peace time
        /// </summary>
        public void RebuildRegionMilitary(Voronoi_MapBorderController.Region region, float deltaTime)
        {
            if (region == null || region.rulingFaction == null) return;

            // TODO: Implement healing and reinforcement logic
            // - Heal injured soldiers over time
            // - Spawn new recruits if below optimal garrison size
            // - Call soldiers back to base if defense is overwhelmed
        }
    }
}