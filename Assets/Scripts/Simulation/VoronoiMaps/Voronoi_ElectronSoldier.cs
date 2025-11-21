using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using static GraphTheory.BattleSoldier;

namespace GraphTheory
{
    public class Voronoi_ElectronSoldier : MonoBehaviour
    {
        private float moveTimer = 0f;

        public enum TacticalRole
        {
            Unassigned,
            TowerDefender,    // Forms Voronoi boundary around tower
            RegionDefender,   // Patrols region, intercepts invaders
            RegionAttacker,   // Attacks enemy forces within region
            BorderAttacker,    // Crosses borders to attack adjacent regions
            Retreat
        }

        public SoldierRank rank = SoldierRank.Private;
        public FightingState fightingState = FightingState.Idle;
        public Voronoi_NeighborhoodWars.SoldierFaction soldierFaction = Voronoi_NeighborhoodWars.SoldierFaction.Red;

        public int soldierID;
        public string soldierName;
        public int maxHealth = 100;
        public int health;
        public int attackPower;
        public int defencePower;
        public Vector3 position;

        [Header("Tactical Assignment")]
        public TacticalRole currentRole = TacticalRole.Unassigned;
        public Voronoi_MapBorderController.Region assignedRegion = null;
        public Vector3 assignedDefensePosition = Vector3.zero; // For tower defenders
        public int defensePositionIndex = -1; // Position in Voronoi boundary

        [Header("Communication")]
        public float communicationRadius = 10f;
        public float communicationCooldown = 5f; // Time in seconds between communication attempts
        private float lastCommunicationTime = -10f;


        /// <summary>
        /// movement configuration
        /// </summary>
        [Header("Movement Configuration")]
        public float movementSpeed = 3f;
        public float arrivalThreshold = 0.2f;
        public float rotationSpeed = 5f;
        public float attackRange = 1.5f;
        public float attackCooldown = 1f;
        public float defenceCooldown = 1f;
        public bool isMoving = false;
        public bool useRigidbodyMovement = false;

        [Header("Self-Preservation")]
        public float retreatHealthThreshold = 0.3f; // Retreat if health is below 30%
        public int outnumberedThreshold = 3; // Retreat if attacked by this many enemies or more


        /// <summary>
        /// My fundamental Command Structure
        /// </summary>
        public Voronoi_ElectronSoldier myCommander = null;
        public Voronoi_ElectronSoldier myLeftSubordinate = null;
        public Voronoi_ElectronSoldier myRightSubordinate = null;

        public Voronoi_ElectronSoldier myEnemy = null;

        // list of soldiers currently attacking this soldier (others will increment/decrement this)
        public readonly List<Voronoi_ElectronSoldier> currentAttackers = new List<Voronoi_ElectronSoldier>();

        NodeBehavior myVertex;
        Rigidbody myRigidbody;
        NavMeshAgent myNavMeshAgent;
        MeshRenderer myMeshRenderer;
        public Material deathMaterial;

        // Path-following state
        private List<Vector3> pathWaypoints = new List<Vector3>();
        private int pathIndex = 0;
        private Vector3 targetPosition;
        private bool hasTarget = false;

        void Awake()
        {
            SetInitialReferences();
        }

        private void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            SetUpdateReferences();
        }

        void SetInitialReferences()
        {
            if (GetComponent<NodeBehavior>() != null)
            {
                myVertex = GetComponent<NodeBehavior>();
            }

            if (GetComponent<Rigidbody>() != null)
            {
                myRigidbody = GetComponent<Rigidbody>();
            }

            if (GetComponent<NavMeshAgent>() != null)
            {
                myNavMeshAgent = GetComponent<NavMeshAgent>();
            }

            if (GetComponent<MeshRenderer>() != null)
            {
                myMeshRenderer = GetComponent<MeshRenderer>();
            }

            health = maxHealth;
            position = transform.position;
        }

        void SetUpdateReferences()
        {
            moveTimer += Time.deltaTime;

            if (moveTimer >= 1f)
            {
                //might not be useful .... but.

                moveTimer = 0f;
            }


            if (health <= 0 && fightingState != FightingState.Dead)
            {
                Die();
            }

            // If my enemy is dead, clear it. The central command will assign a new one.
            if (myEnemy != null && myEnemy.fightingState == FightingState.Dead)
            {
                myEnemy = null;
                fightingState = FightingState.Idle;
                ClearTarget();
            }

            // Always handle movement if a target is set.
            if (hasTarget)
            {
                HandleMovement();
            }
        }

        /// <summary>
        /// Central command from NeighborhoodWars calls this to make the soldier act.
        /// </summary>
        public void ExecuteCurrentRole()
        {
            if (fightingState == FightingState.Dead) return;

            // Self-preservation is the highest priority. Check if a retreat is necessary.
            if (ShouldRetreat())
            {
                InitiateRetreat();
                return; // Do nothing else if retreating.
            }

            // If currently retreating, just keep moving.
            if (fightingState == FightingState.Retreating)
            {
                if (!hasTarget) // Reached retreat point
                {
                    fightingState = FightingState.Idle;
                    currentRole = TacticalRole.Unassigned; // Wait for new orders
                }
                return;
            }

            // Tactical Coordination: Communicate with nearby allies if needed
            if (Time.time > lastCommunicationTime + communicationCooldown)
            {
                CoordinateWithAllies();
                lastCommunicationTime = Time.time;
            }

            // If we have an enemy, the primary goal is to engage it.
            if (myEnemy != null)
            {
                if (IsInRangeOf(myEnemy, attackRange))
                {
                    fightingState = FightingState.Attacking;
                    TryAttack(myEnemy);
                }
                else
                {
                    // Move towards the enemy
                    fightingState = FightingState.Moving;
                    SetTargetPosition(myEnemy.position);
                }
                return; // Engaging an enemy overrides any other role behavior.
            }

            // If no enemy, execute behavior based on assigned role.
            switch (currentRole)
            {
                case TacticalRole.TowerDefender:
                    DefendTowerPosition();
                    break;
                case TacticalRole.RegionDefender:
                    DefendRegionPatrol();
                    break;
                case TacticalRole.RegionAttacker:
                    // This role should have assigned an enemy. If not, it's idle and waits for new orders.
                    fightingState = FightingState.Idle;
                    break;
                case TacticalRole.BorderAttacker:
                    // If a BorderAttacker has no enemy, it should find a new one in its assigned invasion region.
                    if (assignedRegion != null && assignedRegion.rulingFaction.faction != this.soldierFaction)
                    {
                        var enemiesInRegion = assignedRegion.garrisonSoldiers
                            .Where(e => e != null && e.fightingState != BattleSoldier.FightingState.Dead).ToList();

                        var newTarget = Voronoi_NeighborhoodWars.Instance.FindBestTargetFor(this, enemiesInRegion);

                        if (newTarget != null)
                        {
                            myEnemy = newTarget;
                            newTarget.AddAttacker(this);
                        }
                        else
                        {
                            // No more enemies, move to capture the tower.
                            if (assignedRegion.tower != null)
                                SetTargetPosition(assignedRegion.tower.transform.position);
                        }
                    }
                    break;
                case TacticalRole.Unassigned:
                default:
                    fightingState = FightingState.Idle;
                    break;
            }
        }



        private void Die()
        {
            fightingState = FightingState.Dead;
            if (myMeshRenderer != null && deathMaterial != null)
            {
                myMeshRenderer.sharedMaterial = deathMaterial;
            }
            if (myNavMeshAgent != null)
            {
                myNavMeshAgent.enabled = false;
            }
            Voronoi_NeighborhoodWars.Instance.ReportSoldierDeath(this);
        }

        // --- Retreat Methods ---

        /// <summary>
        /// Determines if the soldier should disengage and retreat.
        /// </summary>
        private bool ShouldRetreat()
        {
            // Don't retreat if already retreating or defending a tower (last stand)
            if (fightingState == FightingState.Retreating || currentRole == TacticalRole.TowerDefender)
            {
                return false;
            }

            // Condition 1: Health is critically low.
            bool isCriticallyWounded = (float)health / maxHealth < retreatHealthThreshold;

            // Condition 2: Soldier is outnumbered.
            bool isOutnumbered = currentAttackers.Count >= outnumberedThreshold;

            return isCriticallyWounded || isOutnumbered;
        }

        /// <summary>
        /// Initiates a retreat to the nearest friendly region's tower.
        /// </summary>
        private void InitiateRetreat()
        {
            if (fightingState == FightingState.Retreating) return;

            Debug.Log($"<color=orange>{soldierName} is retreating!</color>");

            fightingState = FightingState.Retreating;
            currentRole = TacticalRole.Retreat;

            // Disengage from current enemy
            if (myEnemy != null)
            {
                myEnemy.RemoveAttacker(this);
                myEnemy = null;
            }

            // Find the safest place to retreat to (the closest friendly tower)
            var retreatPoint = Voronoi_NeighborhoodWars.Instance.FindClosestFriendlyTower(this);
            if (retreatPoint != null)
            {
                SetTargetPosition(retreatPoint.transform.position);
            }
            else
            {
                // If no friendly tower exists (somehow), just run away from attackers
                if (currentAttackers.Count > 0)
                {
                    Vector3 averageAttackerPos = Vector3.zero;
                    foreach (var attacker in currentAttackers)
                    {
                        averageAttackerPos += attacker.position;
                    }
                    averageAttackerPos /= currentAttackers.Count;

                    Vector3 runDirection = (position - averageAttackerPos).normalized;
                    SetTargetPosition(position + runDirection * communicationRadius);
                }
            }
        }



        // --- Communication Methods ---

        /// <summary>
        /// Main communication logic hub for a soldier.
        /// </summary>
        private void CoordinateWithAllies()
        {
            // Rule 1: If I am outnumbered, call for help.
            if (myEnemy != null && currentAttackers.Count > 1)
            {
                RequestAssistance(myEnemy);
                return; // Prioritize calling for help.
            }

            // Rule 2: If I am fighting a high-rank enemy, tell others to focus fire.
            if (myEnemy != null && myEnemy.rank > this.rank)
            {
                ShareTargetInformation(myEnemy);
            }
        }

        /// <summary>
        /// Finds a nearby idle ally and asks them to attack the specified target.
        /// </summary>
        public void RequestAssistance(Voronoi_ElectronSoldier target)
        {
            var nearbyAllies = Voronoi_NeighborhoodWars.Instance.FindNearbyAllies(this, communicationRadius);
            var availableAlly = nearbyAllies
                .FirstOrDefault(ally => ally.myEnemy == null && ally.fightingState == FightingState.Idle);

            if (availableAlly != null)
            {
                Debug.Log($"<color=lightblue>{soldierName} requests assistance. {availableAlly.soldierName} is responding!</color>");
                availableAlly.RespondToAssistanceRequest(target);
            }
        }

        /// <summary>
        /// Tells nearby allies who are idle or fighting lesser targets to focus on this high-priority enemy.
        /// </summary>
        public void ShareTargetInformation(Voronoi_ElectronSoldier highPriorityTarget)
        {
            var nearbyAllies = Voronoi_NeighborhoodWars.Instance.FindNearbyAllies(this, communicationRadius);
            foreach (var ally in nearbyAllies)
            {
                // Don't give orders to allies who are already busy with a decent target
                if (ally.myEnemy != null && ally.myEnemy.rank >= highPriorityTarget.rank) continue;

                // Don't pull tower defenders off their post
                if (ally.currentRole == TacticalRole.TowerDefender) continue;

                Debug.Log($"<color=yellow>{soldierName} shares target info. {ally.soldierName} re-targets to {highPriorityTarget.soldierName}.</color>");
                ally.RespondToAssistanceRequest(highPriorityTarget);
            }
        }

        /// <summary>
        /// Called by an ally. This soldier will now attack the requested target.
        /// </summary>
        public void RespondToAssistanceRequest(Voronoi_ElectronSoldier target)
        {
            if (target == null || target.fightingState == FightingState.Dead) return;
            if (target.CurrentAttackersCount >= Voronoi_NeighborhoodWars.Instance.maxAttackersPerTarget) return;

            // If I was fighting someone else, release them
            if (myEnemy != null)
            {
                myEnemy.RemoveAttacker(this);
            }

            myEnemy = target;
            myEnemy.AddAttacker(this);
            currentRole = TacticalRole.RegionAttacker; // Temporarily adopt an attacker role
            fightingState = FightingState.Attacking;
        }

        /// <summary>
        /// Calculates the intrinsic threat level of this soldier.
        /// Higher rank and health mean a higher threat.
        /// </summary>
        /// <returns>A float representing the threat score.</returns>
        public float GetThreatLevel()
        {
            float rankBonus = ((int)rank + 1) * 10;
            float healthBonus = (float)health / maxHealth * 5;
            return rankBonus + healthBonus;
        }

        public void StateMachine()
        {
            switch (fightingState)
            {
                case FightingState.Idle:
                    if (myEnemy != null)
                    {
                        SetTargetPosition(myEnemy.position);
                    }
                    break;
                case FightingState.Moving:
                    if (myEnemy != null)
                    {
                        HandleMovement();
                        if (IsInRangeOf(myEnemy, attackRange))
                        {
                            TryAttack(myEnemy);
                        }
                    }
                    break;
                case FightingState.Attacking:
                    if (myEnemy != null)
                    {
                        if (IsInRangeOf(myEnemy, attackRange))
                        {
                            TryAttack(myEnemy);
                        }
                        else
                        {
                            SetTargetPosition(myEnemy.position);
                        }

                    }
                    break;
                case FightingState.Defending:
                    ExecuteDefensiveBehavior();
                    break;
                case FightingState.Dead:
                    // Dead behavior
                    if (myMeshRenderer != null && deathMaterial != null)
                    {
                        myMeshRenderer.sharedMaterial = deathMaterial;
                    }
                    myNavMeshAgent.enabled = false;
                    break;
            }
        }

        // --- The key property for the heap ---
        // faction's Max-Heap will read this property to organize the queue.
        public int priority
        {
            get { return (int)rank; }
        }

        public void SetFactionMaterial(Material mat)
        {
            if (myMeshRenderer == null)
            {
                myMeshRenderer = GetComponent<MeshRenderer>();
            }

            if (myMeshRenderer != null)
            {
                myMeshRenderer.sharedMaterial = mat;
            }
            else
            {
                Debug.LogWarning($"SetFactionMaterial failed: MeshRenderer not found on {soldierName ?? gameObject.name}");
            }
        }

        public void TakeDamage(int damage)
        {
            health -= damage;
            if (health <= 0)
            {
                health = 0;
                fightingState = FightingState.Dead;
                ClearPath();
                ClearTarget();

                Voronoi_NeighborhoodWars.Instance.ReportSoldierDeath(this);

                Debug.Log($"{soldierName} has died.");
            }
        }

        public bool IsBusy()
        {
            return fightingState == FightingState.Moving || HasPath() || fightingState == FightingState.Attacking || fightingState == FightingState.Defending;
        }

        public void MoveTo(Vector3 pos)/*teleport*/
        {
            transform.position = pos;
            if (myVertex != null)
            {
                myVertex.position = pos;
            }
            position = pos;
        }

        // ---: Tactical Responsibility Methods ---

        /// <summary>
        /// Assign soldier to defend the tower by forming a Voronoi boundary
        /// </summary>
        public void AssignDefendTower(Voronoi_MapBorderController.Region region, Vector3 defensePos, int positionIndex)
        {
            currentRole = TacticalRole.TowerDefender;
            assignedRegion = region;
            assignedDefensePosition = defensePos;
            defensePositionIndex = positionIndex;
            fightingState = FightingState.Defending;
            myEnemy = null; // Clear any previous attack orders

            SetTargetPosition(defensePos);
            //Debug.Log($"{soldierName} assigned to defend tower at position {positionIndex}");
        }

        /// <summary>
        /// Assign soldier to defend the region (patrol and intercept)
        /// </summary>
        public void AssignDefendRegion(Voronoi_MapBorderController.Region region)
        {
            currentRole = TacticalRole.RegionDefender;
            assignedRegion = region;
            fightingState = FightingState.Defending;
            myEnemy = null;

            // Move to a patrol position within the region
            if (region != null && region.tower != null && !hasTarget)
            {
                Vector3 patrolPos = region.tower.transform.position +
                    Random.insideUnitSphere.normalized * region.actualBoundaryRadius * 0.7f;
                patrolPos.y = region.tower.transform.position.y;
                SetTargetPosition(patrolPos);
            }

            //Debug.Log($"{soldierName} assigned to defend region {region?.tower?.name}");
        }

        /// <summary>
        /// Assign soldier to attack within the region
        /// </summary>
        public void AssignAttackRegion(Voronoi_MapBorderController.Region region, Voronoi_ElectronSoldier target = null)
        {
            currentRole = TacticalRole.RegionAttacker;
            assignedRegion = region;
            myEnemy = target;
            fightingState = FightingState.Attacking;

            if (target != null)
            {
                SetTargetPosition(target.position);
                //Debug.Log($"{soldierName} assigned to attack {target.soldierName} in region");
            }
        }

        /// <summary>
        /// Assign soldier to attack enemy border regions
        /// </summary>
        public void AssignAttackBorder(Voronoi_MapBorderController.Region targetRegion, Voronoi_ElectronSoldier target = null)
        {
            currentRole = TacticalRole.BorderAttacker;
            assignedRegion = targetRegion; // This is the enemy region
            myEnemy = target;
            fightingState = FightingState.Attacking;

            if (target != null)
            {
                SetTargetPosition(target.position);
            }
            else if (targetRegion != null && targetRegion.tower != null)
            {
                Vector3 attackPos = targetRegion.tower.transform.position;
                SetTargetPosition(attackPos);
                //Debug.Log($"{soldierName} assigned to attack border region {targetRegion.tower.name}");
            }
        }

        /// <summary>
        /// Execute defensive behavior based on current tactical role
        /// </summary>
        private void ExecuteDefensiveBehavior()
        {
            switch (currentRole)
            {
                case TacticalRole.TowerDefender:
                    DefendTowerPosition();
                    break;

                case TacticalRole.RegionDefender:
                    DefendRegionPatrol();
                    break;

                default:
                    // Default idle defense
                    break;
            }
        }

        /// <summary>
        /// Maintain position in Voronoi boundary around tower
        /// </summary>
        private void DefendTowerPosition()
        {
            if (assignedRegion == null || assignedRegion.tower == null) return;

            // The central command now finds enemies. This method just ensures the soldier returns to its post.
            if (myEnemy == null)
            {
                float distToPosition = Vector3.Distance(transform.position, assignedDefensePosition);
                if (distToPosition > arrivalThreshold)
                {
                    if (!hasTarget) // Only set target if not already moving there
                    {
                        SetTargetPosition(assignedDefensePosition);
                    }
                }
                else
                {
                    fightingState = FightingState.Defending;
                    isMoving = false;
                    hasTarget = false;
                }
            }
        }

        /// <summary>
        /// Patrol region and intercept invaders
        /// </summary>
        private void DefendRegionPatrol()
        {
            if (assignedRegion == null || assignedRegion.tower == null) return;

            // Central command assigns enemies. This method handles patrolling when idle.
            if (myEnemy == null && !isMoving && !hasTarget)
            {
                Vector3 patrolPos = assignedRegion.tower.transform.position +
                    Random.insideUnitSphere.normalized * assignedRegion.actualBoundaryRadius * 0.7f;
                patrolPos.y = assignedRegion.tower.transform.position.y;
                SetTargetPosition(patrolPos);
            }
        }

        /// <summary>
        /// Find enemies near the tower within specified radius
        /// </summary>
        private List<Voronoi_ElectronSoldier> FindEnemiesNearTower(Voronoi_MapBorderController.Region region, float radius)
        {
            List<Voronoi_ElectronSoldier> enemies = new List<Voronoi_ElectronSoldier>();
            if (region == null || region.tower == null) return enemies;

            Vector3 towerPos = region.tower.transform.position;

            // Check invading soldiers
            foreach (var invader in region.invadingSoldiers)
            {
                if (invader == null || invader.fightingState == FightingState.Dead) continue;
                if (invader.soldierFaction == soldierFaction) continue;

                float dist = Vector3.Distance(towerPos, invader.position);
                if (dist <= radius)
                {
                    enemies.Add(invader);
                }
            }

            return enemies.OrderBy(e => Vector3.Distance(transform.position, e.position)).ToList();
        }

        /// <summary>
        /// Clear tactical assignment
        /// </summary>
        public void ClearTacticalRole()
        {
            currentRole = TacticalRole.Unassigned;
            assignedRegion = null;
            assignedDefensePosition = Vector3.zero;
            defensePositionIndex = -1;
        }


        // --- Path API ---

        // Set a path as a series of nodes (waypoints will be the node positions).
        // The path should be ordered from start -> goal.
        public void SetPath(List<NodeBehavior> nodes)
        {
            pathWaypoints.Clear();
            if (nodes == null || nodes.Count == 0)
            {
                pathIndex = 0;
                hasTarget = false;
                return;
            }

            foreach (var n in nodes)
            {
                if (n == null) continue;
                pathWaypoints.Add(n.position);
            }

            pathIndex = 0;
            if (pathWaypoints.Count > 0)
            {
                SetTargetPosition(pathWaypoints[0]);
            }
        }

        public bool HasPath()
        {
            return pathWaypoints.Count > 0 && pathIndex < pathWaypoints.Count;
        }

        public void ClearPath()
        {
            pathWaypoints.Clear();
            pathIndex = 0;
        }

        // Request the soldier to move toward a world position. This is non-blocking.
        public void SetTargetPosition(Vector3 pos)
        {
            
            targetPosition = pos;
            hasTarget = true;
            fightingState = FightingState.Moving;
            isMoving = true;
        }

        public void ClearTarget()
        {
            hasTarget = false;
            isMoving = false;
            fightingState = FightingState.Idle;
        }

        // Returns true if soldier is within 'range' of 'other'
        public bool IsInRangeOf(Voronoi_ElectronSoldier other, float range)
        {
            if (other == null) return false;
            return Vector3.Distance(transform.position, other.transform.position) <= range;
        }

        // Try to attack an enemy immediately if within attackRange.
        // If not in range, this will set a movement target toward the enemy.
        // Returns true if an attack happened this call (damage applied).
        public bool TryAttack(Voronoi_ElectronSoldier enemy)
        {
            if (enemy == null || enemy.fightingState == FightingState.Dead) return false;

            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist <= attackRange)
            {
                fightingState = FightingState.Attacking;
                enemy.TakeDamage(attackPower);
                return true;
            }
            else
            {

                return false;
            }
        }

        // Attack bookkeeping: called by other soldiers when they target this soldier
        public void AddAttacker(Voronoi_ElectronSoldier attacker)
        {
            if (attacker == null) return;
            if (!currentAttackers.Contains(attacker))
            {
                currentAttackers.Add(attacker);
            }
        }

        public void RemoveAttacker(Voronoi_ElectronSoldier attacker)
        {
            if (attacker == null) return;
            currentAttackers.Remove(attacker);
        }

        public int CurrentAttackersCount => currentAttackers.Count;

        // Called each frame to smoothly approach the target position.
        private void HandleMovement()
        {
            if (!hasTarget || fightingState == FightingState.Dead) return;

            Vector3 current = transform.position;
            Vector3 toTarget = targetPosition - current;
            float dist = toTarget.magnitude;

            if (dist <= arrivalThreshold)
            {
                // Arrived
                MoveTo(targetPosition); // snap to exact position and update vertex
                hasTarget = false;
                fightingState = FightingState.Idle;
                OnReachedTarget();
                return;
            }

            Vector3 delta = toTarget.normalized * movementSpeed * Time.deltaTime;
            // clamp if overshooting
            if (delta.magnitude > dist) delta = toTarget;

            if (useRigidbodyMovement && myRigidbody != null)
            {
                myRigidbody.MovePosition(current + delta);
            }
            else if (myNavMeshAgent != null)
            {
                myNavMeshAgent.Move(delta);
            }
            else
            {
                transform.position = current + delta;
            }

            // keep node / logical position in sync
            if (myVertex != null)
            {
                myVertex.position = delta + current;

            }
            position = transform.position;
        }

        // Hook for arrival; can be extended or subscribed to by external code.
        protected virtual void OnReachedTarget()
        {
            // If we had an assigned enemy and are now in range, attempt attack.
            if (myEnemy != null && fightingState != FightingState.Dead)
            {
                TryAttack(myEnemy);
            }
        }
    }
}
