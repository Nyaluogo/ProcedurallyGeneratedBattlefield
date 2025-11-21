using System.Collections;
using UnityEngine;
using static GraphTheory.GraphMaster;

namespace GraphTheory
{
    public class Snake_LinkedList : MonoBehaviour
    {
        public static Snake_LinkedList Instance { get; private set; }
        
        public LinkedListProperties snakeBody;
        public int totalPoints = 0;
        public float moveSpeed = 5f;
        public float gridSize = 1f;
        private float moveTimer = 0f;
        private Vector3 direction = Vector3.right;
        private bool hasInput = false;
        bool started = false;
        bool isGrowing = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Start()
        {
            SetInitialReferences();
        }

        void Update()
        {
            HandleInput();
            SetUpdateReferences();
        }

        void SetInitialReferences()
        {
            // Create initial head if none exists
            if (snakeBody.head == null)
            {
                
                snakeBody.InsertAtBeginning("Head");
                if (snakeBody.head != null)
                {
                    transform.position = Vector3.zero;
                    snakeBody.head.position = Vector3.zero;

                    started = true; // Auto-start movement
                    direction = Vector3.right;
                }
            }
            else
            {
                transform.position = Vector3.zero;
                snakeBody.head.transform.position = Vector3.zero;
            }
        }

        void SetUpdateReferences()
        {
            moveTimer += Time.deltaTime;

            // Only update movement if the snake has been started by player input
            if (!started)
                return;

            if (moveTimer >= 1f / moveSpeed)
            {
                moveTimer = 0f;
                MoveSnake();
                hasInput = false;
            }
        }

        void HandleInput()
        {
            if (!hasInput)
            {
                Vector3 newDirection = direction;

                if (Input.GetKeyDown(KeyCode.W) && direction != Vector3.back)
                {
                    newDirection = Vector3.forward;
                    hasInput = true;
                }
                else if (Input.GetKeyDown(KeyCode.S) && direction != Vector3.forward)
                {
                    newDirection = Vector3.back;
                    hasInput = true;
                }
                else if (Input.GetKeyDown(KeyCode.A) && direction != Vector3.right)
                {
                    newDirection = Vector3.left;
                    hasInput = true;
                }
                else if (Input.GetKeyDown(KeyCode.D) && direction != Vector3.left)
                {
                    newDirection = Vector3.right;
                    hasInput = true;
                }

                if (hasInput)
                {
                    direction = newDirection;
                    Debug.Log($"Direction changed to: {direction}");
                    started = true; // Ensure started is set to true on input
                    Debug.Log($"Started is now: {started}"); // Confirm started is set
                }
            }
        }

        void MoveSnake()
        {
            if (snakeBody.head == null)
            {
                Debug.LogWarning("Cannot move snake - head is null!");
                return;
            }

            Vector3 previousPosition = snakeBody.head.position;
            Vector3 newPosition = snakeBody.head.position + (new Vector3(direction.x, 0f, direction.z) * gridSize);

            //Debug.Log($"Current Direction: {direction}, New Position: {newPosition}"); // Debug direction and newPosition

            snakeBody.head.position = newPosition;

            if (snakeBody.head.dataObj != null)
            {
                snakeBody.head.dataObj.transform.position = newPosition;
                //Debug.Log($"Head moved to position: {newPosition}");
            }

            var currentNode = snakeBody.head.nextNode;
            while (currentNode != null)
            {
                Vector3 tempPosition = currentNode.position;
                currentNode.position = previousPosition;
                if (currentNode.dataObj != null)
                    currentNode.dataObj.transform.position = currentNode.position;

                previousPosition = tempPosition;
                currentNode = currentNode.nextNode;
            }
        }

        public void Grow()
        {
            Debug.Log("Growing snake!");
            if (snakeBody.head == null)
            {
                Debug.LogError("Cannot grow snake - missing head or body prefab!");
                return;
            }

            var node_name = $"BodySegment_{snakeBody.CountNodes() + 1}";
            isGrowing = true;

            var new_segment = snakeBody.InsertAtEnd(node_name);
            if (new_segment == null)
            {
                Debug.LogError("Failed to insert new segment at end of snake body!");
                isGrowing = false;
                return;
            }
            if (new_segment.dataObj != null)
            {
                new_segment.dataObj.transform.SetParent(this.transform);
                // Disable collider temporarily
                Collider segmentCollider = new_segment.dataObj.GetComponent<Collider>();
                if (segmentCollider != null)
                {
                    segmentCollider.enabled = false;
                }
            }

            // Position the new segment
            if (snakeBody.tail != null)
            {
                Vector3 newPosition;
                if (snakeBody.tail.previousNode != null)
                {
                    newPosition = snakeBody.tail.previousNode.position;
                }
                else
                {
                    // If this is the first body segment, place it behind the head
                    newPosition = snakeBody.head.position - (direction * gridSize);
                }

                new_segment.position = newPosition; // set the new segment's position
                if (new_segment.dataObj != null)
                {
                    new_segment.dataObj.transform.position = newPosition; // and the data object
                    Collider segmentCollider = new_segment.dataObj.GetComponent<Collider>();
                    // Re-enable collider after a short delay
                    if (segmentCollider != null)
                    {
                        StartCoroutine(EnableColliderDelayed(segmentCollider, 1f));
                    }
                }

            }
            totalPoints++;
            isGrowing = false;
        }

        public void CutSnakeAtNode(NodeBehavior segment)
        {
            Debug.Log($"Cutting snake at segment: {segment.nodeName}");
            if (segment == null)
            {
                Debug.LogError("Cannot cut snake - segment is null!");
                return;
            }
            if(isGrowing)
            {
                Debug.LogWarning("Cannot cut snake while it is growing!");
                return;
            }
            snakeBody.RemoveFromNodeToEnd(segment);
        }

        private IEnumerator EnableColliderDelayed(Collider collider, float delay)
        {
            yield return new WaitForSeconds(delay);
            collider.enabled = true;
        }
    }
}