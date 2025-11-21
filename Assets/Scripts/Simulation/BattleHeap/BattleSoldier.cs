using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static GraphTheory.BattleHeap_Rules;

namespace GraphTheory
{

    public class BattleSoldier : MonoBehaviour
    {
        private float moveTimer = 0f;

        public enum SoldierRank
        {
            General = 0,
            Colonel = 1,
            Major = 2,
            Captain = 3,
            Lieutenant = 4,
            Sergeant = 5,
            Corporal = 6,
            Private = 7,
        }

        public enum FightingState
        {
            Idle,
            Moving,
            Attacking,
            Defending,
            Retreating,
            Dead,
        }

        public SoldierRank rank = SoldierRank.Private;
        public FightingState fightingState = FightingState.Idle;
        public SoldierFaction soldierFaction = SoldierFaction.Red;

        public int soldierID;
        public string soldierName;
        public int maxHealth=100;
        public int health;
        public int attackPower;
        public int defencePower;
        public Vector3 position;

        /// <summary>
        /// movement configuration
        /// </summary>
        [Header("Movement Configuration")]
        public float movementSpeed=3f;
        public float arrivalThreshold=0.2f;
        public float rotationSpeed=5f;
        public float attackRange=1.5f;
        public float attackCooldown=1f;
        public float defenceCooldown=1f;
        public bool isMoving=false;
        public bool useRigidbodyMovement=false;

        /// <summary>
        /// My fundamental Command Structure
        /// </summary>
        public BattleSoldier myCommander=null;
        public BattleSoldier myLeftSubordinate=null;
        public BattleSoldier myRightSubordinate=null;

        public BattleSoldier myEnemy=null;
        // list of soldiers currently attacking this soldier (others will increment/decrement this)
        public readonly List<BattleSoldier> currentAttackers = new List<BattleSoldier>();


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


        [System.Serializable]
        public class Weapon
        {
            public string weaponName;
            public int damage;
            public float range;
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

            if(fightingState == FightingState.Moving)
                HandleMovement();
        }

        void SetInitialReferences()
        {
            if(GetComponent<NodeBehavior>()!=null)
            {
                myVertex = GetComponent<NodeBehavior>();
            }

            if(GetComponent<Rigidbody>()!=null)
            {
                myRigidbody = GetComponent<Rigidbody>();
            }

            if(GetComponent<NavMeshAgent>()!=null)
            {
                myNavMeshAgent = GetComponent<NavMeshAgent>();
            }

            if(GetComponent<MeshRenderer>()!=null)
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
                if(health <= 0 && fightingState != FightingState.Dead)
                {
                    fightingState = FightingState.Dead;
                    BattleHeap_Rules.Instance.ReportSoldierDeath(this);
                }

                if(myEnemy != null && myEnemy.fightingState == FightingState.Dead)
                {
                    BattleHeap_Rules.Instance.ReportSoldierDeath(myEnemy);
                    myEnemy = null;
                    fightingState = FightingState.Idle;
                    ClearPath();
                    ClearTarget();
                    
                }

                StateMachine();

                moveTimer = 0f;
            }
        }

        public void StateMachine()
        {
            switch(fightingState)
            {
                case FightingState.Idle:
                    if(myEnemy != null)
                    {
                        SetTargetPosition(myEnemy.position);
                    }
                    break;
                case FightingState.Moving:
                    if(myEnemy != null)
                    {
                        if(IsInRangeOf(myEnemy, attackRange))
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
                    // Defending behavior
                    break;
                case FightingState.Dead:
                    // Dead behavior
                    if(myMeshRenderer != null && deathMaterial != null)
                    {
                        myMeshRenderer.sharedMaterial = deathMaterial;
                    }
                    myNavMeshAgent.enabled = false;
                    break;
            }
        }

        // --- The key property for the heap ---
        // Your Max-Heap will read this property to organize the queue.
        public int priority
        {
            get { return (int)rank; }
        }

        public void TakeDamage(int damage)
        {
            health -= damage;
            if(health <= 0)
            {
                health = 0;
                fightingState = FightingState.Dead;
                ClearPath();
                ClearTarget();

                BattleHeap_Rules.Instance.ReportSoldierDeath(this);

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
            if(myVertex != null)
            {
                myVertex.position = pos;
            }
            position = pos;
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
            // If assigned a new explicit target position, clear previous enemy attacker relationship
            if (myEnemy != null)
            {
                myEnemy.RemoveAttacker(this);
            }

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
        public bool IsInRangeOf(BattleSoldier other, float range)
        {
            if (other == null) return false;
            return Vector3.Distance(transform.position, other.transform.position) <= range;
        }

        // Try to attack an enemy immediately if within attackRange.
        // If not in range, this will set a movement target toward the enemy.
        // Returns true if an attack happened this call (damage applied).
        public bool TryAttack(BattleSoldier enemy)
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
        public void AddAttacker(BattleSoldier attacker)
        {
            if (attacker == null) return;
            if (!currentAttackers.Contains(attacker))
            {
                currentAttackers.Add(attacker);
            }
        }

        public void RemoveAttacker(BattleSoldier attacker)
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
            else if(myNavMeshAgent != null)
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
