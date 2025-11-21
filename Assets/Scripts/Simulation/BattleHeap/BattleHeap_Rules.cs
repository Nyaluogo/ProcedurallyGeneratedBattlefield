using System.Collections.Generic;
using UnityEngine;
using static GraphTheory.BattleSoldier;
using static GraphTheory.GraphMaster;

namespace GraphTheory
{
    public class BattleHeap_Rules : MonoBehaviour
    {
        public static BattleHeap_Rules Instance { get; private set; }

        public BattleHeap_Rules()
        {
            Instance = this;
        }

        public enum SoldierFaction
        {
            Red,
            Blue,
            Green,
            Yellow,
        }

        public enum BattleState
        {
            Recruiting,
            Organizing,
            InProgress,
            Paused,
            Completed,
        }
        public BattleState currentBattleState = BattleState.Recruiting;

        private float moveTimer = 0f;

        // engagement tuning
        [Header("Engagement Rules")]
        public int maxAttackersPerTarget = 3;        // e.g. 1 for strict 1v1, 3 for up-to-three attackers
        public float engagementSearchRadius = 30f;   // only consider enemies within this radius (reduce long runs)
        public float engagementSpacing = 1.2f;       // radial spacing multiplier so attackers do not overlap


        [System.Serializable]
        public class RecruitDefinition 
        {
            public string recruitName;
            public GameObject soldierPrefab;
            public Transform[] spawnPoints;
            public int maxRecruits=10;
            public float recruitInterval=2f;
            public float offset = 1f;
            public SoldierRank rank = SoldierRank.Private;

            public List<BattleSoldier> RecruitSoldiers()
            {
                if(soldierPrefab == null || spawnPoints.Length == 0 || maxRecruits <= 0)
                {
                    Debug.LogWarning("RecruitDefinition is not properly configured.");
                    return new List<BattleSoldier>();
                }

                List<BattleSoldier> recruitedSoldiers = new List<BattleSoldier>();
                for (int i = 0; i < maxRecruits; i++)
                {
                    //var spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
                    var spawnPoint = spawnPoints[i];
                    spawnPoint.position = new Vector3(
                        spawnPoint.position.x,
                        spawnPoint.position.y + offset,
                        spawnPoint.position.z
                    );
                    GameObject newSoldier =GameObject.Instantiate(soldierPrefab, spawnPoint.position, Quaternion.identity);

                    if(newSoldier.TryGetComponent<BattleSoldier>(out BattleSoldier soldierComponent))
                    {
                        soldierComponent.soldierName = $"{recruitName}_{i+1}";
                        soldierComponent.position = spawnPoint.position;
                        if(soldierComponent.TryGetComponent<NodeBehavior>(out NodeBehavior nodeComponent))
                        {
                            nodeComponent.position = spawnPoint.position;
                        }
                        recruitedSoldiers.Add(soldierComponent);
                    }
                }
                return recruitedSoldiers;
            }


        }

        [System.Serializable]
        public class FactionDefinition
        {
            public string name;
            public int maxSoldiers=20;
            public int maxSoldiersPerBattalion=5;
            public Vector3 battlefieldSize;
            public HeapProperties playerCommandQueue;
            public BattleSoldier rootCommander;
            public SoldierFaction faction;
            public RecruitDefinition privateSoldierPrefab;
            public RecruitDefinition corporalSoldierPrefab;
            public RecruitDefinition sergeantSoldierPrefab;
            public RecruitDefinition lieutenantSoldierPrefab;
            public RecruitDefinition captainSoldierPrefab;
            public RecruitDefinition majorSoldierPrefab;
            public RecruitDefinition colonelSoldierPrefab;
            public RecruitDefinition generalSoldierPrefab;
            
            public bool isReady=false;
            public bool isDefeated=false;

            public void GenerateCommandQueue()
            {
                playerCommandQueue = new HeapProperties()
                {
                    heapType = HeapProperties.HeapType.MinHeap,
                    heapNodes = new NodeBehavior[maxSoldiers]
                };

                var privates = privateSoldierPrefab.RecruitSoldiers();
                foreach (var soldier in privates)
                {
                    var soldier_node = soldier.GetComponent<NodeBehavior>();
                    if (soldier_node != null)
                    {
                        soldier_node.priority = (int)soldier.rank;
                        playerCommandQueue.Insert(soldier_node);
                    }
                }

                var corporals = corporalSoldierPrefab.RecruitSoldiers();
                foreach (var soldier in corporals)
                {
                    var soldier_node = soldier.GetComponent<NodeBehavior>();
                    if (soldier_node != null)
                    {
                        soldier_node.priority = (int)soldier.rank;
                        playerCommandQueue.Insert(soldier_node);
                    }
                }

                var sergeants = sergeantSoldierPrefab.RecruitSoldiers();
                foreach (var soldier in sergeants)
                {
                    var soldier_node = soldier.GetComponent<NodeBehavior>();
                    if (soldier_node != null)
                    {
                        soldier_node.priority = (int)soldier.rank;
                        playerCommandQueue.Insert(soldier_node);
                    }
                }

                var lieutenants = lieutenantSoldierPrefab.RecruitSoldiers();
                foreach (var soldier in lieutenants)
                {
                    var soldier_node = soldier.GetComponent<NodeBehavior>();
                    if (soldier_node != null)
                    {
                        soldier_node.priority = (int)soldier.rank;
                        playerCommandQueue.Insert(soldier_node);
                    }
                }

                var captains = captainSoldierPrefab.RecruitSoldiers();
                foreach (var soldier in captains)
                {
                    var soldier_node = soldier.GetComponent<NodeBehavior>();
                    if (soldier_node != null)
                    {
                        soldier_node.priority = (int)soldier.rank;
                        playerCommandQueue.Insert(soldier_node);
                    }
                }

                var majors = majorSoldierPrefab.RecruitSoldiers();
                foreach (var soldier in majors)
                {
                    var soldier_node = soldier.GetComponent<NodeBehavior>();
                    if (soldier_node != null)
                    {
                        soldier_node.priority = (int)soldier.rank;
                        playerCommandQueue.Insert(soldier_node);
                    }
                }

                var colonels = colonelSoldierPrefab.RecruitSoldiers();
                foreach (var soldier in colonels)
                {
                    var soldier_node = soldier.GetComponent<NodeBehavior>();
                    if (soldier_node != null)
                    {
                        soldier_node.priority = (int)soldier.rank;
                        playerCommandQueue.Insert(soldier_node);
                    }
                }

                var generals = generalSoldierPrefab.RecruitSoldiers();
                foreach (var soldier in generals)
                {
                    var soldier_node = soldier.GetComponent<NodeBehavior>();
                    if (soldier_node != null)
                    {
                        soldier_node.priority = (int)soldier.rank;
                        playerCommandQueue.Insert(soldier_node);
                    }
                }
            }

            public void OrganizeHeapHierarchy()
            {
                if(playerCommandQueue == null || playerCommandQueue.heapNodes.Length == 0)
                {
                    Debug.LogWarning("Command queue is empty or not initialized.");
                    return;
                }

                for (int i = 0; i < playerCommandQueue.heapSize; i++)
                {
                    var currentNode = playerCommandQueue.heapNodes[i];
                    var soldier = currentNode.GetComponent<BattleSoldier>();

                    // Set commander (parent)
                    int parentIdx = (i - 1) / 2;
                    if (parentIdx >= 0 && parentIdx < playerCommandQueue.heapSize)
                    {
                        soldier.myCommander = playerCommandQueue.heapNodes[parentIdx].GetComponent<BattleSoldier>();
                    }

                    // Set subordinates (children)
                    int leftIdx = 2 * i + 1;
                    int rightIdx = 2 * i + 2;

                    if (leftIdx < playerCommandQueue.heapSize)
                    {
                        soldier.myLeftSubordinate = playerCommandQueue.heapNodes[leftIdx].GetComponent<BattleSoldier>();
                    }

                    if (rightIdx < playerCommandQueue.heapSize)
                    {
                        soldier.myRightSubordinate = playerCommandQueue.heapNodes[rightIdx].GetComponent<BattleSoldier>();
                    }
                }

            }


            
        }
        public FactionDefinition[] factions;


        private void Awake()
        {
            Instance = this;
            
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            SetInitialReferences();
        }

        // Update is called once per frame
        void Update()
        {
            SetUpdateReferences();
        }

        void SetInitialReferences()
        {
            PrepareBattle();
            Invoke(nameof(StartWar), 5f);
        }

        void SetUpdateReferences()
        {
            // Only run the game loop if the battle is InProgress
            if (currentBattleState != BattleState.InProgress)
            {
                return;
            }

            moveTimer += Time.deltaTime;

            if (moveTimer >= .1f)
            {
                ProcessBattleTick();
                EvaluateBattlefield();
                moveTimer = 0f;
            }
        }

        public void PrepareBattle()
        {
            currentBattleState = BattleState.Recruiting;
            foreach (var faction in factions)
            {
                faction.GenerateCommandQueue();
                faction.OrganizeHeapHierarchy();
            }
            currentBattleState = BattleState.Organizing;
        }
        
        public void StartWar()
        {
            if(factions.Length > 1)
            {
                foreach (var faction in factions)
                {
                    if (faction.playerCommandQueue.heapSize == 0)
                    {
                        Debug.LogWarning($"Faction {faction.name} has no soldiers to fight.");
                        return;
                    }
                    faction.isReady = true;
                    faction.isDefeated = false;
                }
            }
            StartBattle();
        }

        public void ProcessBattleTick()
        {
            // 1. Let each faction's highest-priority soldier act.
            foreach (var faction in factions)
            {
                if (faction.isDefeated) continue;

                // Get the soldier who gets to act next (highest rank)
                NodeBehavior actingNode = faction.playerCommandQueue.ExtractMax();

                // If heap is empty, this faction can't act.
                if (actingNode == null) continue;

                // Get the soldier component from the node
                BattleSoldier actingSoldier = actingNode.GetComponent<BattleSoldier>();
                if(actingSoldier == null)
                {
                    Debug.LogWarning("Extracted node does not have a BattleSoldier component.");
                    faction.playerCommandQueue.Insert(actingNode);
                    continue;
                }

                // 2. Check if the soldier is dead
                if (actingSoldier.fightingState == BattleSoldier.FightingState.Dead)
                {
                    // This soldier is dead, don't re-insert them.
                    // By not re-inserting, they are permanently removed from the battle.
                    continue;
                }

                if(actingSoldier.IsBusy())
                {
                    // Soldier is busy (moving, attacking, etc.), re-insert and skip turn
                    faction.playerCommandQueue.Insert(actingNode);
                    continue;
                }

                // 3. The Soldier Takes Their Turn (Your AI Logic)
                // This is where you decide what the soldier does
                ExecuteSoldierTurn(actingSoldier, faction);

                // 4. If still alive, put them back in the queue for their next turn.
                // Because they are re-inserted, high-rank soldiers will
                // "bubble up" and get to act again sooner than low-rank soldiers.
                if (actingSoldier.fightingState != BattleSoldier.FightingState.Dead)
                {
                    faction.playerCommandQueue.Insert(actingNode);
                }
            }

            // 5. After everyone acts, check for a winner.
            CheckForVictory();
        }

        public void ExecuteSoldierTurn(BattleSoldier soldier, FactionDefinition myFaction)
        {
            // Example: Find an enemy and attack them.
            // 1. Find an enemy faction
            FactionDefinition enemyFaction = null;
            foreach (var f in factions)
            {
                if (f != myFaction && !f.isDefeated)
                {
                    enemyFaction = f;
                    break;
                }
            }

            if (enemyFaction == null) return; // No enemies left

            // 2. Choose the best enemy soldier to engage:
            //    prefer enemy with fewest attackers and within engagementSearchRadius.
            BattleSoldier chosenEnemy = null;
            int chosenAttackers = int.MaxValue;
            float chosenDist = float.MaxValue;

            for (int i = 0; i < enemyFaction.playerCommandQueue.heapSize; i++)
            {
                var node = enemyFaction.playerCommandQueue.heapNodes[i];
                if (node == null) continue;
                var candidate = node.GetComponent<BattleSoldier>();
                if (candidate == null) continue;
                if (candidate.fightingState == BattleSoldier.FightingState.Dead) continue;

                float dist = Vector3.Distance(soldier.position, candidate.position);
                if (dist > engagementSearchRadius) continue; // skip too-far targets

                int attackers = candidate.CurrentAttackersCount;
                if (attackers >= maxAttackersPerTarget) continue; // candidate saturated

                // choose candidate with fewest attackers, tie-break by distance
                if (attackers < chosenAttackers || (attackers == chosenAttackers && dist < chosenDist))
                {
                    chosenEnemy = candidate;
                    chosenAttackers = attackers;
                    chosenDist = dist;
                }
            }

            // fallback to root if none found (still ensures saturation check)
            if (chosenEnemy == null)
            {
                NodeBehavior enemyRootNode = enemyFaction.playerCommandQueue.Peek();
                if (enemyRootNode != null)
                {
                    var rootCandidate = enemyRootNode.GetComponent<BattleSoldier>();
                    if (rootCandidate != null && rootCandidate.fightingState != BattleSoldier.FightingState.Dead && rootCandidate.CurrentAttackersCount < maxAttackersPerTarget)
                    {
                        chosenEnemy = rootCandidate;
                    }
                }
            }

            if (chosenEnemy == null) return;

            // assign soldier -> chosenEnemy, update bookkeeping
            // remove from previous enemy if any
            if (soldier.myEnemy != null && soldier.myEnemy != chosenEnemy)
            {
                soldier.myEnemy.RemoveAttacker(soldier);
                soldier.myEnemy = null;
            }

            soldier.myEnemy = chosenEnemy;
            chosenEnemy.AddAttacker(soldier);

            // If already in range, attempt immediate attack
            if (soldier.IsInRangeOf(chosenEnemy, soldier.attackRange))
            {
                bool did_attack = soldier.TryAttack(chosenEnemy);
                if (did_attack)
                {
                    Debug.Log($"{myFaction.name}'s {soldier.rank} successfully attacked {enemyFaction.name}'s {chosenEnemy.rank} for {soldier.attackPower} damage!");
                }
                return;
            }

            // Not in range: compute a spreaded engagement position around the enemy so attackers don't stack.
            int attackerIndex = chosenEnemy.CurrentAttackersCount - 1; // index of this attacker in list
            attackerIndex = Mathf.Clamp(attackerIndex, 0, maxAttackersPerTarget - 1);

            float angleStep = 360f / Mathf.Max(1, maxAttackersPerTarget);
            float angle = angleStep * attackerIndex + Random.Range(-10f, 10f); // small jitter
            // radial distance: enemy attackRange + spacing + index-scaling
            float radius = Mathf.Max(1f, chosenEnemy.attackRange + (engagementSpacing * (0.5f + attackerIndex * 0.5f)));

            Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * radius;
            Vector3 engagePos = chosenEnemy.position + offset;

            soldier.SetTargetPosition(engagePos);
            Debug.Log($"{myFaction.name}'s {soldier.rank} assigned to engage {enemyFaction.name}'s {chosenEnemy.rank} at offset {offset}.");
        }

        public void CheckForVictory()
        {
            foreach (var faction in factions)
            {
                // A faction is defeated if its queue is empty (all soldiers are dead
                // and have been extracted)
                if (faction.playerCommandQueue.heapSize == 0)
                {
                    faction.isDefeated = true;
                    Debug.Log($"Faction {faction.name} has been defeated!");
                }
            }

            // Check if only one faction remains
            int activeFactions = 0;
            FactionDefinition winner = null;
            foreach (var faction in factions)
            {
                if (!faction.isDefeated)
                {
                    activeFactions++;
                    winner = faction;
                }
            }

            if (activeFactions <= 1)
            {
                if (winner != null)
                {
                    Debug.Log($"BATTLE OVER! The winner is {winner.name}!");
                }
                else
                {
                    Debug.Log("BATTLE OVER! It's a draw!");
                }
                CompleteBattle();
            }
        }

        public void StartBattle()
        {
            currentBattleState = BattleState.InProgress;
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
        }

        public void EvaluateBattlefield()
        {             foreach (var faction in factions)
            {
                if (faction.playerCommandQueue.heapSize == 0)
                {
                    faction.isDefeated = true;
                }
            }
            int activeFactions = 0;
            foreach (var faction in factions)
            {
                if (!faction.isDefeated)
                {
                    activeFactions++;
                }
            }
            if (activeFactions <= 1)
            {
                CompleteBattle();
            }
        }

        public void ReportSoldierDeath(BattleSoldier deadSoldier)
        {
            if (deadSoldier == null) return;

            // find owning faction
            FactionDefinition owner = null;
            foreach (var f in factions)
            {
                if (f != null && f.faction == deadSoldier.soldierFaction)
                {
                    owner = f;
                    break;
                }
            }
            if (owner == null)
            {
                Debug.LogWarning("ReportSoldierDeath: owner faction not found.");
                return;
            }

            // Before removing the dead from heap, reassign its attackers (they are now free)
            var attackersSnapshot = new List<BattleSoldier>(deadSoldier.currentAttackers);
            foreach (var attacker in attackersSnapshot)
            {
                if (attacker == null) continue;
                // remove attacker reference to dead target
                deadSoldier.RemoveAttacker(attacker);
                attacker.myEnemy = null;
                // immediately try to find a new target for the freed attacker
                // find the attacker's faction definition (should be same owner)
                ExecuteSoldierTurn(attacker, owner);
            }

            // Remove dead soldier's node from the heap (best-effort).
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
                    // restore heap property - try to use available methods on your HeapProperties
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

            // Reorganize the heap relationships so subordinate/commander links remain coherent.
            owner.OrganizeHeapHierarchy();
        }
    }
}
