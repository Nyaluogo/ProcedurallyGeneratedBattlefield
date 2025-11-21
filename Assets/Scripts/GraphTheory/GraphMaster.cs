using UnityEngine;

namespace GraphTheory
{
    public class GraphMaster : MonoBehaviour
    {
        public static GraphMaster Instance { get; private set; }
        GraphMaster() { Instance = this; }
        [System.Serializable]
        public class LinkedListProperties
        {
            public NodeBehavior head = null;
            public NodeBehavior current = null;
            public NodeBehavior tail = null;
            public GameObject dataBodyPrefab;


            public void GenerateLinkedList()
            {
                if(dataBodyPrefab == null)
                {
                    Debug.LogError("Data Body Prefab is not assigned.");
                    return;
                }
                GameObject vertex = Instantiate(dataBodyPrefab);
                NodeBehavior node1 = vertex.GetComponent<NodeBehavior>();
                node1.nodeName = "Node 1";
                node1 = head;
                //print head info

                //start from beginning
                while(node1 != null)
                {
                    node1.DisplayInfo();
                    node1 = node1.nextNode;
                }
            }

            public void InsertAtBeginning(string name)
            {
                if (dataBodyPrefab == null)
                {
                    Debug.LogError("Data Body Prefab is not assigned.");
                    return;
                }
                GameObject vertex = Instantiate(dataBodyPrefab);
                NodeBehavior newNode = vertex.GetComponent<NodeBehavior>();
                if (head == null)
                {
                    head = newNode;
                    tail = newNode;
                }
                else
                {
                    newNode.nextNode = head;
                    head.previousNode = newNode;
                    head = newNode;
                }
            }

            public NodeBehavior InsertAtEnd(string name)
            {
                if (dataBodyPrefab == null)
                {
                    Debug.LogError("Data Body Prefab is not assigned.");
                    return null;
                }
                GameObject vertex = Instantiate(dataBodyPrefab);
                NodeBehavior newNode = vertex.GetComponent<NodeBehavior>();

                newNode.nodeName = name;
                if (tail == null)
                {
                    head = newNode;
                    tail = newNode;
                }
                else
                {
                    tail.nextNode = newNode;
                    newNode.previousNode = tail;
                    tail = newNode;
                }

                if(newNode != null)
                {
                    return newNode;
                }
                else
                {
                    return null;
                }
            }

            public void InsertAfterNode(NodeBehavior previousNode, string name)
            {
                if (previousNode == null)
                {
                    Debug.LogError("The given previous node cannot be null.");
                    return;
                }

                if (dataBodyPrefab == null)
                {
                    Debug.LogError("Data Body Prefab is not assigned.");
                    return;
                }
                GameObject vertex = Instantiate(dataBodyPrefab);
                NodeBehavior newNode = vertex.GetComponent<NodeBehavior>();

                newNode.nodeName = name;
                newNode.nextNode = previousNode.nextNode;
                previousNode.nextNode = newNode;
                newNode.previousNode = previousNode;
                if (newNode.nextNode != null)
                {
                    newNode.nextNode.previousNode = newNode;
                }
                else
                {
                    tail = newNode; // Update tail if new node is added at the end
                }
            }

            public void InsertBeforeNode(NodeBehavior nextNode, string name)
            {
                if (nextNode == null)
                {
                    Debug.LogError("The given next node cannot be null.");
                    return;
                }

                if (dataBodyPrefab == null)
                {
                    Debug.LogError("Data Body Prefab is not assigned.");
                    return;
                }
                GameObject vertex = Instantiate(dataBodyPrefab);
                NodeBehavior newNode = vertex.GetComponent<NodeBehavior>();

                newNode.nodeName = name;
                newNode.previousNode = nextNode.previousNode;
                nextNode.previousNode = newNode;
                newNode.nextNode = nextNode;
                if (newNode.previousNode != null)
                {
                    newNode.previousNode.nextNode = newNode;
                }
                else
                {
                    head = newNode; // Update head if new node is added at the beginning
                }
            }


            public void DeleteAtBeginning()
            {
                if (head == null)
                {
                    Debug.LogError("The list is empty.");
                    return;
                }
                head = head.nextNode;
                if (head != null)
                {
                    head.previousNode = null;
                }
                else
                {
                    tail = null; // List became empty
                }
            }

            public void DeleteAtEnd()
            {
                if (tail == null)
                {
                    Debug.LogError("The list is empty.");
                    return;
                }
                tail = tail.previousNode;
                if (tail != null)
                {
                    tail.nextNode = null;
                }
                else
                {
                    head = null; // List became empty
                }
            }

            public NodeBehavior RemoveVertex(NodeBehavior node)
            {
                if(head == null)
                {
                    Debug.LogError("The list is empty.");
                    return null;
                }
                NodeBehavior currentNode = head;

                while (currentNode != null)
                {
                    if(currentNode == node)
                    {
                        return DeleteVertexByName(currentNode.name);
                    }
                }

                return null;
            }

            public NodeBehavior DeleteVertexByName(string name)
            {
                if (head == null)
                {
                    Debug.LogError("The list is empty.");
                    return null;
                }
                NodeBehavior currentNode = head;
                while (currentNode != null)
                {
                    if (currentNode.nodeName == name)
                    {
                        NodeBehavior temp = currentNode;
                        if (currentNode.previousNode != null)
                        {
                            currentNode.previousNode.nextNode = currentNode.nextNode;
                        }
                        else
                        {
                            head = currentNode.nextNode; // Update head if needed
                        }

                        if (currentNode.nextNode != null)
                        {
                            currentNode.nextNode.previousNode = currentNode.previousNode;
                        }
                        else
                        {
                            tail = currentNode.previousNode; // Update tail if needed
                        }
                        return temp;
                    }
                    currentNode = currentNode.nextNode;
                }
                Debug.LogError("Node with the given name not found.");
                return null;
            }

            public void DeleteAfterNode(NodeBehavior previousNode)
            {
                if (previousNode == null || previousNode.nextNode == null)
                {
                    Debug.LogError("The given previous node is invalid.");
                    return;
                }
                NodeBehavior nodeToDelete = previousNode.nextNode;
                previousNode.nextNode = nodeToDelete.nextNode;
                if (nodeToDelete.nextNode != null)
                {
                    nodeToDelete.nextNode.previousNode = previousNode;
                }
                else
                {
                    tail = previousNode; // Update tail if needed
                }
            }

            public void DeleteBeforeNode(NodeBehavior nextNode)
            {
                if (nextNode == null || nextNode.previousNode == null)
                {
                    Debug.LogError("The given next node is invalid.");
                    return;
                }
                NodeBehavior nodeToDelete = nextNode.previousNode;
                nextNode.previousNode = nodeToDelete.previousNode;
                if (nodeToDelete.previousNode != null)
                {
                    nodeToDelete.previousNode.nextNode = nextNode;
                }
                else
                {
                    head = nextNode; // Update head if needed
                }
            }

            public void RemoveFromNodeToEnd(NodeBehavior startNode)
            {
                if (startNode == null)
                {
                    Debug.LogError("The given start node is invalid.");
                    return;
                }

                // Capture the node before the start so we can reconnect the remaining list after deletion.
                NodeBehavior prev = startNode.previousNode;
                string startName = startNode.nodeName;

                int removedCount = 0;
                NodeBehavior node = startNode;

                // Iterate from the start node to the end, destroy each node GameObject and clear references.
                while (node != null)
                {
                    NodeBehavior next = node.nextNode;

                    // Clear links to avoid accidental dangling references while destroyed.
                    node.previousNode = null;
                    node.nextNode = null;

                    // Destroy the GameObject that holds the NodeBehavior.
                    if (node.gameObject != null)
                        Object.Destroy(node.gameObject);

                    removedCount++;
                    node = next;
                }

                // Reconnect the remaining list (nodes before startNode).
                if (prev != null)
                {
                    prev.nextNode = null;
                    tail = prev;
                }
                else
                {
                    // startNode was head, list is now empty
                    head = null;
                    tail = null;
                }

                Debug.Log($"Removed {removedCount} node(s) starting at '{startName}'.");
            }

            public void ReverseList()
            {
                NodeBehavior currentNode = head;
                NodeBehavior temp = null;
                while (currentNode != null)
                {
                    // Swap next and previous pointers
                    temp = currentNode.previousNode;
                    currentNode.previousNode = currentNode.nextNode;
                    currentNode.nextNode = temp;
                    // Move to the next node (which is previous before swapping)
                    currentNode = currentNode.previousNode;
                }
                // Swap head and tail
                if (temp != null)
                {
                    head = temp.previousNode;
                }
            }

            public int SearchNode(string name)
            {
                NodeBehavior currentNode = head;
                int position = 0;
                while (currentNode != null)
                {
                    if (currentNode.nodeName == name)
                    {
                        return position;
                    }
                    currentNode = currentNode.nextNode;
                    position++;
                }
                return -1; // Node not found
            }

            public int CountNodes()
            {
                NodeBehavior currentNode = head;
                int count = 0;
                while (currentNode != null)
                {
                    count++;
                    currentNode = currentNode.nextNode;
                }
                return count;
            }
        }

        [System.Serializable]
        public class StackProperties
        {
            public const int maxNodesToSpawn = 100;
            public NodeBehavior[] stack = new NodeBehavior[maxNodesToSpawn];
            public int top = -1;
            public void Push(NodeBehavior value)
            {
                // Implementation for pushing a value onto the stack
                stack[++top] = value;
            }
            public NodeBehavior Pop()
            {
                // Implementation for popping a value from the stack
                return stack[top--]; // Placeholder return
            }
            public NodeBehavior Peek()
            {
                // Implementation for peeking at the top value of the stack
                return stack[top]; // Placeholder return
            }
            public bool IsStackEmpty()
            {
                // Implementation to check if the stack is empty
                return top == -1; // Placeholder return
            }
        }

        [System.Serializable]
        public class QueueProperties
        {
            public const int maxNodesToSpawn = 100;
            public NodeBehavior[] queue = new NodeBehavior[maxNodesToSpawn];
            int front = 0;
            int rear = -1;
            public int queueSize = 0;
            public void Insert(NodeBehavior value)
            {
                if (value == null)
                {
                    Debug.LogWarning("Queue.Insert: cannot insert null value.");
                    return;
                }

                if (IsQueueFull())
                {
                    Debug.LogWarning("Queue.Insert: queue is full, cannot insert.");
                    return;
                }

                rear = (rear + 1) % maxNodesToSpawn;
                queue[rear] = value;
                queueSize++;
            }
            public NodeBehavior Remove()
            {
                if (IsQueueEmpty())
                {
                    Debug.LogWarning("Queue.Remove: queue is empty.");
                    return null;
                }

                NodeBehavior value = queue[front];
                queue[front] = null; // clear reference to allow GC / avoid stale refs
                front = (front + 1) % maxNodesToSpawn;
                queueSize--;

                // reset indices when queue becomes empty to keep state clean
                if (queueSize == 0)
                {
                    front = 0;
                    rear = -1;
                }

                return value;
            }

            public NodeBehavior Peek()
            {
                if (IsQueueEmpty()) return null;
                return queue[front];
            }

            // Clear queue quickly
            public void Clear()
            {
                if (queueSize == 0) return;
                // null out used slots
                for (int i = 0; i < queueSize; i++)
                {
                    int idx = (front + i) % maxNodesToSpawn;
                    queue[idx] = null;
                }
                front = 0;
                rear = -1;
                queueSize = 0;
            }

            // Return a snapshot array (front..rear order). O(n)
            public NodeBehavior[] ToArray()
            {
                NodeBehavior[] arr = new NodeBehavior[queueSize];
                for (int i = 0; i < queueSize; i++)
                {
                    arr[i] = queue[(front + i) % maxNodesToSpawn];
                }
                return arr;
            }

            // Rotate front element to rear in O(1) without extra Remove+Insert calls.
            // Useful for SwitchTurn semantics (move front to back).
            public void RotateOne()
            {
                if (queueSize <= 1) return;

                // place a copy/reference of front at new rear position
                rear = (rear + 1) % maxNodesToSpawn;
                queue[rear] = queue[front];
                queue[front] = null;
                front = (front + 1) % maxNodesToSpawn;
                // queueSize unchanged
            }

            public bool IsQueueEmpty()
            {
                // Implementation to check if the queue is empty
                return queueSize == 0;
            }

            public bool IsQueueFull()
            {
                // Implementation to check if the queue is full
                return queueSize == maxNodesToSpawn;
            }
        }

        [System.Serializable]
        public class HeapProperties
        {
            public enum HeapType
            {
                MaxHeap,
                MinHeap
            }
            public HeapType heapType = HeapType.MaxHeap;

            public const int maxNodesToSpawn = 100;
            public NodeBehavior[] heapNodes = new NodeBehavior[maxNodesToSpawn];
            public int heapSize = 0;
            public NodeBehavior rootNode;

            /// <summary>
            /// Gets the index of the parent node. Parent(i) = floor((i - 1) / 2)
            /// </summary>
            private int Parent(int i)
            {
                return (i - 1) / 2;
            }

            /// <summary>
            /// Gets the index of the left child. Left(i) = 2i + 1
            /// </summary>
            private int Left(int i)
            {
                return 2 * i + 1;
            }

            /// <summary>
            /// Gets the index of the right child. Right(i) = 2i + 2
            /// </summary>
            private int Right(int i)
            {
                return 2 * i + 2;
            }

            /// <summary>
            /// Swaps two NodeBehavior elements in the heapNodes array.
            /// </summary>
            private void Swap(int i, int j)
            {
                NodeBehavior temp = heapNodes[i];
                heapNodes[i] = heapNodes[j];
                heapNodes[j] = temp;
            }

            public bool IsHeapEmpty()
            {
                return heapSize == 0;
            }


            // --- Core Heap Operations ---

            /// <summary>
            /// The core function to maintain the max-heap property (sift-down).
            /// Restores the Max-Heap property starting from the given index 'i'.
            /// </summary>
            /// <param name="i">The index of the node to check and fix.</param>
            public void MaxHeapify(int i)
            {
                int left = Left(i);
                int right = Right(i);
                int largest = i;

                // 1. Check if the left child exists and is greater than the current node.
                if (left < heapSize && heapNodes[left].priority > heapNodes[i].priority)
                {
                    largest = left;
                }

                // 2. Check if the right child exists and is greater than the current largest.
                if (right < heapSize && heapNodes[right].priority > heapNodes[largest].priority)
                {
                    largest = right;
                }

                // 3. If the largest element is not the current node, swap them and recursively call MaxHeapify on the affected subtree.
                if (largest != i)
                {
                    Swap(i, largest);
                    MaxHeapify(largest);
                }
            }

            /// <summary>
            /// A general Heapify method that defaults to MaxHeapify(0) to fix a heap from the root.
            /// </summary>
            public void Heapify()
            {
                if (heapSize > 0)
                {
                    MaxHeapify(0);
                }
            }

            /// <summary>
            /// Rearranges the heapNodes array into a Max-Heap structure efficiently.
            /// It starts from the last non-leaf node and works backwards to the root.
            /// </summary>
            public void BuildMaxHeap()
            {
                // For a clean build, we assume the initial 'heapSize' is the number of elements 
                // that have been populated in the array, up to maxNodesToSpawn.
                // The original prompt used `heapNodes.Length`, so we'll enforce the current `heapSize`.

                // The first index of a non-leaf node is at (heapSize / 2) - 1.
                for (int i = (heapSize / 2) - 1; i >= 0; i--)
                {
                    MaxHeapify(i);
                }
            }

            /// <summary>
            /// Inserts a new node into the max-heap (sift-up).
            /// </summary>
            public void Insert(NodeBehavior node)
            {
                if (heapSize >= maxNodesToSpawn)
                {
                    // Prevent array overflow in a game scenario
                    // In a real Unity app, you might use Debug.LogError here.
                    System.Console.WriteLine("Heap is full. Cannot insert new node.");
                    return;
                }

                // Place the new element at the end and increase size
                int i = heapSize;
                heapNodes[i] = node;
                heapSize++;

                // Restore the max-heap property by "sifting up"
                while (i > 0 && heapNodes[Parent(i)].priority < heapNodes[i].priority)
                {
                    Swap(i, Parent(i));
                    i = Parent(i);
                }
            }

            /// <summary>
            /// Extracts and returns the node with the maximum priority (the root).
            /// </summary>
            public NodeBehavior ExtractMax()
            {
                if (heapSize < 1)
                {
                    System.Console.WriteLine("Heap is empty. Cannot extract max.");
                    return null;
                }

                // 1. Get the max element
                NodeBehavior maxNode = heapNodes[0];

                // 2. Move the last element to the root position
                heapNodes[0] = heapNodes[heapSize - 1];
                // 3. Null out the last slot and decrease size
                heapNodes[heapSize - 1] = null;
                heapSize--;

                // 4. Restore the max-heap property by "sifting down"
                MaxHeapify(0);

                return maxNode;
            }

            public NodeBehavior Peek()
            {
                if (heapSize < 1)
                {
                    return null;
                }
                return heapNodes[0];
            }
        }

        [System.Serializable]
        public class TreeProperties
        {
            public const int maxNodesToSpawn = 10;
            public NodeBehavior rootNode;
            public NodeBehavior[] nodes;
            public EdgeBehavior[] edges;

            public GameObject dataBodyPrefab; // Prefab for instantiating nodes
            public GameObject edgePrefab; // Prefab for instantiating edges
            private int nodeCount = 0;
            private int edgeCount = 0;

            public void InOrderTraversal(NodeBehavior node)
            {
                if (node == null) return;
                InOrderTraversal(node.previousNode); // Left subtree
                node.DisplayInfo();                   // Visit node
                InOrderTraversal(node.nextNode);     // Right subtree
            }

            public void PreOrderTraversal(NodeBehavior node)
            {
                if (node == null) return;
                node.DisplayInfo();                   // Visit node
                PreOrderTraversal(node.previousNode); // Left subtree
                PreOrderTraversal(node.nextNode);     // Right subtree
            }

            public void PostOrderTraversal(NodeBehavior node)
            {
                if (node == null) return;
                PostOrderTraversal(node.previousNode); // Left subtree
                PostOrderTraversal(node.nextNode);     // Right subtree
                node.DisplayInfo();                     // Visit node
            }

            public void GenerateBinaryTree()
            {
                if (dataBodyPrefab == null)
                {
                    Debug.LogError("Data Body Prefab is not assigned.");
                    return;
                }

                // Clear existing tree
                ClearTree();
                nodes = new NodeBehavior[maxNodesToSpawn];
                edges = new EdgeBehavior[maxNodesToSpawn * 2]; // Binary tree can have at most 2*n edges
                nodeCount = 0;
                edgeCount = 0;

                // Create root node
                rootNode = CreateNode("Root", Vector3.zero, 0);
                nodes[nodeCount++] = rootNode;

                // Generate a random binary tree with depth-first approach
                int maxDepth = 4; // Adjust as needed
                GenerateBinaryTreeRecursive(rootNode, 0, maxDepth, -4f, 4f);

                Debug.Log($"Binary Tree generated with {nodeCount} nodes and {edgeCount} edges");
            }

            private void GenerateBinaryTreeRecursive(NodeBehavior parent, int depth, int maxDepth, float leftBound, float rightBound)
            {
                if (depth >= maxDepth || nodeCount >= maxNodesToSpawn - 1) return;

                float yOffset = -2.5f; // Vertical spacing between levels
                float xMid = (leftBound + rightBound) / 2f;

                // Create left child (previousNode)
                if (Random.value > 0.3f) // 70% chance to create left child
                {
                    Vector3 leftPos = new Vector3(
                        (parent.position.x + leftBound) / 2f,
                        parent.position.y + yOffset,
                        parent.position.z
                    );
                    NodeBehavior leftChild = CreateNode($"L{depth}-{nodeCount}", leftPos, nodeCount);
                    parent.previousNode = leftChild;
                    nodes[nodeCount++] = leftChild;

                    // Create edge from parent to left child
                    CreateEdge(parent, leftChild, Color.cyan);

                    GenerateBinaryTreeRecursive(leftChild, depth + 1, maxDepth, leftBound, parent.position.x);
                }

                // Create right child (nextNode)
                if (Random.value > 0.3f && nodeCount < maxNodesToSpawn) // 70% chance to create right child
                {
                    Vector3 rightPos = new Vector3(
                        (parent.position.x + rightBound) / 2f,
                        parent.position.y + yOffset,
                        parent.position.z
                    );
                    NodeBehavior rightChild = CreateNode($"R{depth}-{nodeCount}", rightPos, nodeCount);
                    parent.nextNode = rightChild;
                    nodes[nodeCount++] = rightChild;

                    // Create edge from parent to right child
                    CreateEdge(parent, rightChild, Color.magenta);

                    GenerateBinaryTreeRecursive(rightChild, depth + 1, maxDepth, parent.position.x, rightBound);
                }
            }

            public void GenerateMinHeapTree()
            {
                if (dataBodyPrefab == null)
                {
                    Debug.LogError("Data Body Prefab is not assigned.");
                    return;
                }

                ClearTree();
                nodes = new NodeBehavior[maxNodesToSpawn];
                edges = new EdgeBehavior[maxNodesToSpawn * 2];
                nodeCount = 0;
                edgeCount = 0;

                // Generate random priorities for min-heap
                int[] priorities = new int[Mathf.Min(15, maxNodesToSpawn)];
                for (int i = 0; i < priorities.Length; i++)
                {
                    priorities[i] = Random.Range(1, 100);
                }

                // Build min-heap from array
                BuildHeapTree(priorities, true);

                Debug.Log($"Min-Heap Tree generated with {nodeCount} nodes and {edgeCount} edges");
            }

            public void GenerateMaxHeapTree()
            {
                if (dataBodyPrefab == null)
                {
                    Debug.LogError("Data Body Prefab is not assigned.");
                    return;
                }

                ClearTree();
                nodes = new NodeBehavior[maxNodesToSpawn];
                edges = new EdgeBehavior[maxNodesToSpawn * 2];
                nodeCount = 0;
                edgeCount = 0;

                // Generate random priorities for max-heap
                int[] priorities = new int[Mathf.Min(15, maxNodesToSpawn)];
                for (int i = 0; i < priorities.Length; i++)
                {
                    priorities[i] = Random.Range(1, 100);
                }

                // Build max-heap from array
                BuildHeapTree(priorities, false);

                Debug.Log($"Max-Heap Tree generated with {nodeCount} nodes and {edgeCount} edges");
            }

            private void BuildHeapTree(int[] values, bool isMinHeap)
            {
                if (values.Length == 0) return;

                // Create heap structure
                for (int i = 0; i < values.Length && nodeCount < maxNodesToSpawn; i++)
                {
                    Vector3 position = CalculateHeapNodePosition(i, values.Length);
                    NodeBehavior node = CreateNode($"Node-{i}", position, i);
                    node.priority = values[i];
                    nodes[nodeCount++] = node;
                }

                // Heapify
                for (int i = values.Length / 2 - 1; i >= 0; i--)
                {
                    Heapify(nodes, i, values.Length, isMinHeap);
                }

                // Link nodes based on heap structure and create edges
                rootNode = nodes[0];
                for (int i = 0; i < nodeCount; i++)
                {
                    int leftIndex = 2 * i + 1;
                    int rightIndex = 2 * i + 2;

                    if (leftIndex < nodeCount)
                    {
                        nodes[i].previousNode = nodes[leftIndex];
                        CreateEdge(nodes[i], nodes[leftIndex], Color.green);
                    }
                    if (rightIndex < nodeCount)
                    {
                        nodes[i].nextNode = nodes[rightIndex];
                        CreateEdge(nodes[i], nodes[rightIndex], Color.yellow);
                    }
                }
            }

            private void Heapify(NodeBehavior[] array, int index, int heapSize, bool isMinHeap)
            {
                int targetIndex = index;
                int leftChild = 2 * index + 1;
                int rightChild = 2 * index + 2;

                if (isMinHeap)
                {
                    if (leftChild < heapSize && array[leftChild].priority < array[targetIndex].priority)
                        targetIndex = leftChild;
                    if (rightChild < heapSize && array[rightChild].priority < array[targetIndex].priority)
                        targetIndex = rightChild;
                }
                else
                {
                    if (leftChild < heapSize && array[leftChild].priority > array[targetIndex].priority)
                        targetIndex = leftChild;
                    if (rightChild < heapSize && array[rightChild].priority > array[targetIndex].priority)
                        targetIndex = rightChild;
                }

                if (targetIndex != index)
                {
                    // Swap priorities and names
                    int tempPriority = array[index].priority;
                    string tempName = array[index].nodeName;
                    array[index].priority = array[targetIndex].priority;
                    array[index].nodeName = array[targetIndex].nodeName;
                    array[targetIndex].priority = tempPriority;
                    array[targetIndex].nodeName = tempName;

                    Heapify(array, targetIndex, heapSize, isMinHeap);
                }
            }

            private Vector3 CalculateHeapNodePosition(int index, int totalNodes)
            {
                int level = (int)Mathf.Floor(Mathf.Log(index + 1, 2));
                int levelStart = (int)Mathf.Pow(2, level) - 1;
                int positionInLevel = index - levelStart;
                int nodesInLevel = (int)Mathf.Pow(2, level);

                float xSpacing = 8f / (nodesInLevel + 1);
                float x = -4f + xSpacing * (positionInLevel + 1);
                float y = 3f - level * 2.5f;

                return new Vector3(x, y, 0);
            }

            public void GenerateBalancedTree()
            {
                if (dataBodyPrefab == null)
                {
                    Debug.LogError("Data Body Prefab is not assigned.");
                    return;
                }

                ClearTree();
                nodes = new NodeBehavior[maxNodesToSpawn];
                edges = new EdgeBehavior[maxNodesToSpawn * 2];
                nodeCount = 0;
                edgeCount = 0;

                // Create sorted array for balanced BST
                int[] sortedValues = new int[Mathf.Min(15, maxNodesToSpawn)];
                for (int i = 0; i < sortedValues.Length; i++)
                {
                    sortedValues[i] = i + 1;
                }

                // Build balanced BST from sorted array
                rootNode = BuildBalancedBST(sortedValues, 0, sortedValues.Length - 1, 0, -4f, 4f, null);

                Debug.Log($"Balanced BST generated with {nodeCount} nodes and {edgeCount} edges");
            }

            private NodeBehavior BuildBalancedBST(int[] sortedArray, int start, int end, int depth, float leftBound, float rightBound, NodeBehavior parent)
            {
                if (start > end || nodeCount >= maxNodesToSpawn) return null;

                int mid = (start + end) / 2;
                float x = (leftBound + rightBound) / 2f;
                float y = 3f - depth * 2.5f;
                Vector3 position = new Vector3(x, y, 0);

                NodeBehavior node = CreateNode($"Node-{sortedArray[mid]}", position, nodeCount);
                node.priority = sortedArray[mid];
                nodes[nodeCount++] = node;

                // Create edge from parent to current node
                if (parent != null)
                {
                    CreateEdge(parent, node, Color.blue);
                }

                // Build left and right subtrees
                node.previousNode = BuildBalancedBST(sortedArray, start, mid - 1, depth + 1, leftBound, x, node);
                node.nextNode = BuildBalancedBST(sortedArray, mid + 1, end, depth + 1, x, rightBound, node);

                return node;
            }

            public void GeneratePrimMinimumSpanningTree()
            {
                if (dataBodyPrefab == null)
                {
                    Debug.LogError("Data Body Prefab is not assigned.");
                    return;
                }

                ClearTree();
                nodes = new NodeBehavior[maxNodesToSpawn];
                edges = new EdgeBehavior[maxNodesToSpawn];
                nodeCount = 0;
                edgeCount = 0;

                // Create a connected graph first
                int numNodes = Mathf.Min(10, maxNodesToSpawn);
                NodeBehavior[] graphNodes = new NodeBehavior[numNodes];

                // Create nodes in circular pattern
                for (int i = 0; i < numNodes; i++)
                {
                    float angle = (i * 360f / numNodes) * Mathf.Deg2Rad;
                    Vector3 position = new Vector3(Mathf.Cos(angle) * 3f, Mathf.Sin(angle) * 3f, 0);
                    graphNodes[i] = CreateNode($"Node-{i}", position, i);
                    nodes[nodeCount++] = graphNodes[i];
                }

                // Prim's algorithm for MST
                bool[] inMST = new bool[numNodes];
                float[] minWeight = new float[numNodes];
                int[] parent = new int[numNodes];

                for (int i = 0; i < numNodes; i++)
                {
                    minWeight[i] = float.MaxValue;
                    parent[i] = -1;
                }

                minWeight[0] = 0;
                rootNode = graphNodes[0];

                for (int count = 0; count < numNodes; count++)
                {
                    int u = MinWeightVertex(minWeight, inMST, numNodes);
                    inMST[u] = true;

                    for (int v = 0; v < numNodes; v++)
                    {
                        float weight = Vector3.Distance(graphNodes[u].position, graphNodes[v].position);
                        if (!inMST[v] && weight < minWeight[v])
                        {
                            minWeight[v] = weight;
                            parent[v] = u;
                        }
                    }
                }

                // Build tree structure from MST with edges
                for (int i = 1; i < numNodes; i++)
                {
                    if (parent[i] != -1)
                    {
                        // Link child to parent (use previousNode for first child, nextNode for siblings)
                        if (graphNodes[parent[i]].previousNode == null)
                            graphNodes[parent[i]].previousNode = graphNodes[i];
                        else if (graphNodes[parent[i]].nextNode == null)
                            graphNodes[parent[i]].nextNode = graphNodes[i];

                        // Create MST edge
                        CreateEdge(graphNodes[parent[i]], graphNodes[i], Color.green);
                    }
                }

                Debug.Log($"Prim's MST generated with {numNodes} nodes and {edgeCount} edges");
            }

            public void GenerateKruskalMinimumSpanningTree()
            {
                if (dataBodyPrefab == null)
                {
                    Debug.LogError("Data Body Prefab is not assigned.");
                    return;
                }

                ClearTree();
                nodes = new NodeBehavior[maxNodesToSpawn];
                edges = new EdgeBehavior[maxNodesToSpawn];
                nodeCount = 0;
                edgeCount = 0;

                // Create nodes
                int numNodes = Mathf.Min(10, maxNodesToSpawn);
                NodeBehavior[] graphNodes = new NodeBehavior[numNodes];

                for (int i = 0; i < numNodes; i++)
                {
                    float angle = (i * 360f / numNodes) * Mathf.Deg2Rad;
                    Vector3 position = new Vector3(Mathf.Cos(angle) * 3f, Mathf.Sin(angle) * 3f, 0);
                    graphNodes[i] = CreateNode($"Node-{i}", position, i);
                    nodes[nodeCount++] = graphNodes[i];
                }

                // Create edges list
                System.Collections.Generic.List<(int, int, float)> edgesList = new System.Collections.Generic.List<(int, int, float)>();
                for (int i = 0; i < numNodes; i++)
                {
                    for (int j = i + 1; j < numNodes; j++)
                    {
                        float weight = Vector3.Distance(graphNodes[i].position, graphNodes[j].position);
                        edgesList.Add((i, j, weight));
                    }
                }

                // Sort edges by weight
                edgesList.Sort((a, b) => a.Item3.CompareTo(b.Item3));

                // Kruskal's algorithm with Union-Find
                int[] parent = new int[numNodes];
                for (int i = 0; i < numNodes; i++) parent[i] = i;

                int edgesAdded = 0;
                rootNode = graphNodes[0];

                foreach (var edge in edgesList)
                {
                    int u = edge.Item1;
                    int v = edge.Item2;

                    int rootU = Find(parent, u);
                    int rootV = Find(parent, v);

                    if (rootU != rootV)
                    {
                        Union(parent, rootU, rootV);

                        // Add edge to tree structure
                        if (graphNodes[u].previousNode == null)
                            graphNodes[u].previousNode = graphNodes[v];
                        else if (graphNodes[u].nextNode == null)
                            graphNodes[u].nextNode = graphNodes[v];

                        // Create visual edge
                        CreateEdge(graphNodes[u], graphNodes[v], Color.red);

                        edgesAdded++;
                        if (edgesAdded >= numNodes - 1) break;
                    }
                }

                Debug.Log($"Kruskal's MST generated with {numNodes} nodes and {edgesAdded} edges");
            }

            // Helper methods
            private NodeBehavior CreateNode(string name, Vector3 position, int id)
            {
                GameObject nodeObj = Object.Instantiate(dataBodyPrefab);
                NodeBehavior node = nodeObj.GetComponent<NodeBehavior>();
                node.nodeName = name;
                node.position = position;
                node.nodeId = id;
                node.Initialize(id, position);
                return node;
            }

            /// <summary>
            /// Creates a visual edge between two nodes using EdgeBehavior.
            /// </summary>
            private EdgeBehavior CreateEdge(NodeBehavior source, NodeBehavior target, Color edgeColor)
            {
                if (edgeCount >= edges.Length)
                {
                    Debug.LogWarning("Edge array is full. Cannot create more edges.");
                    return null;
                }

                // Create edge GameObject
                GameObject edgeObj;

                if (edgePrefab != null)
                {
                    // Use prefab if available
                    edgeObj = Object.Instantiate(edgePrefab);
                }
                else
                {
                    // Create simple line renderer edge
                    edgeObj = new GameObject($"Edge_{source.nodeName}_to_{target.nodeName}");
                    LineRenderer lineRenderer = edgeObj.AddComponent<LineRenderer>();

                    // Configure line renderer
                    lineRenderer.startWidth = 0.1f;
                    lineRenderer.endWidth = 0.1f;
                    lineRenderer.positionCount = 2;
                    lineRenderer.useWorldSpace = true;

                    // Set material and color
                    Material lineMaterial = new Material(Shader.Find("Sprites/Default"));
                    lineMaterial.color = edgeColor;
                    lineRenderer.material = lineMaterial;

                    // Set positions
                    lineRenderer.SetPosition(0, source.position);
                    lineRenderer.SetPosition(1, target.position);
                }

                // Add or get EdgeBehavior component
                EdgeBehavior edge = edgeObj.GetComponent<EdgeBehavior>();
                if (edge == null)
                {
                    edge = edgeObj.AddComponent<EdgeBehavior>();
                }

                // Initialize edge connection data
                edge.connections = new EdgeBehavior.NodeConnection[1];
                edge.connections[0] = new EdgeBehavior.NodeConnection
                {
                    sourceNode = source,
                    targetNode = target,
                    weight = Vector3.Distance(source.position, target.position)
                };

                edges[edgeCount++] = edge;
                return edge;
            }

            /// <summary>
            /// Updates all edge positions based on their connected nodes.
            /// Call this when nodes move.
            /// </summary>
            public void UpdateEdgePositions()
            {
                for (int i = 0; i < edgeCount; i++)
                {
                    if (edges[i] != null && edges[i].connections != null && edges[i].connections.Length > 0)
                    {
                        LineRenderer lineRenderer = edges[i].GetComponent<LineRenderer>();
                        if (lineRenderer != null)
                        {
                            var connection = edges[i].connections[0];
                            if (connection.sourceNode != null && connection.targetNode != null)
                            {
                                lineRenderer.SetPosition(0, connection.sourceNode.position);
                                lineRenderer.SetPosition(1, connection.targetNode.position);
                            }
                        }
                    }
                }
            }

            private void ClearTree()
            {
                // Clear nodes
                if (nodes != null)
                {
                    for (int i = 0; i < nodeCount; i++)
                    {
                        if (nodes[i] != null && nodes[i].gameObject != null)
                            Object.Destroy(nodes[i].gameObject);
                    }
                }

                // Clear edges
                if (edges != null)
                {
                    for (int i = 0; i < edgeCount; i++)
                    {
                        if (edges[i] != null && edges[i].gameObject != null)
                            Object.Destroy(edges[i].gameObject);
                    }
                }

                rootNode = null;
                nodeCount = 0;
                edgeCount = 0;
            }

            private int MinWeightVertex(float[] minWeight, bool[] inMST, int numNodes)
            {
                float min = float.MaxValue;
                int minIndex = -1;

                for (int v = 0; v < numNodes; v++)
                {
                    if (!inMST[v] && minWeight[v] < min)
                    {
                        min = minWeight[v];
                        minIndex = v;
                    }
                }

                return minIndex;
            }

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

            // Additional utility methods
            public int GetTreeHeight()
            {
                return GetHeightRecursive(rootNode);
            }

            private int GetHeightRecursive(NodeBehavior node)
            {
                if (node == null) return 0;
                int leftHeight = GetHeightRecursive(node.previousNode);
                int rightHeight = GetHeightRecursive(node.nextNode);
                return 1 + Mathf.Max(leftHeight, rightHeight);
            }

            public int CountNodes()
            {
                return nodeCount;
            }

            public int CountEdges()
            {
                return edgeCount;
            }

            public void LevelOrderTraversal()
            {
                if (rootNode == null) return;

                QueueProperties queue = new QueueProperties();
                queue.Insert(rootNode);

                while (!queue.IsQueueEmpty())
                {
                    NodeBehavior current = queue.Remove();
                    current.DisplayInfo();

                    if (current.previousNode != null)
                        queue.Insert(current.previousNode);
                    if (current.nextNode != null)
                        queue.Insert(current.nextNode);
                }
            }
        }

        /// <summary>
        /// Represents a Voronoi Diagram and its dual graph (Delaunay Triangulation).
        /// 
        /// VORONOI DIAGRAM OVERVIEW:
        /// A Voronoi diagram partitions a plane into regions based on distance to a set of seed points (nuclei).
        /// Each region (cell) contains all points closer to its nucleus than to any other nucleus.
        /// 
        /// DELAUNAY TRIANGULATION:
        /// The dual graph of a Voronoi diagram. It connects nuclei whose Voronoi cells share an edge.
        /// Key property: No point lies inside the circumcircle of any triangle (Delaunay condition).
        /// 
        /// APPLICATIONS:
        /// - Procedural map generation (biomes, territories)
        /// - Pathfinding and spatial partitioning
        /// - Natural pattern simulation (cells, crystals)
        /// - Lloyd's relaxation for evenly distributed points
        /// </summary>
        [System.Serializable]
        public class VoronoiDiagramProperties
        {
            /// <summary>
            /// Represents a single region in the Voronoi diagram.
            /// Each cell is defined by a nucleus (seed point) and the edges that bound its region.
            /// Points within a cell are closer to its nucleus than to any other nucleus.
            /// </summary>
            [System.Serializable]
            public class VoronoiCell
            {
                /// <summary>
                /// The seed point (site) that defines this Voronoi cell.
                /// All points in this cell are closest to this nucleus.
                /// </summary>
                public NodeBehavior nucleus;

                /// <summary>
                /// The edges that form the boundary of this Voronoi cell.
                /// These edges are perpendicular bisectors between neighboring nuclei.
                /// In the dual graph, each edge corresponds to an edge in the Delaunay triangulation.
                /// </summary>
                public EdgeBehavior[] boundaryEdges;

                /// <summary>
                /// Adjacent Voronoi cells that share an edge with this cell.
                /// In the Delaunay triangulation, these correspond to connected vertices.
                /// </summary>
                public VoronoiCell[] neighboringCells;

                /// <summary>
                /// The geometric center (centroid) of the cell's polygon.
                /// Used in Lloyd's relaxation to move the nucleus toward a more uniform distribution.
                /// Calculated as the average of all boundary vertices.
                /// </summary>
                public Vector3 centroidPosition;

                /// <summary>
                /// The area of the Voronoi cell's polygon.
                /// Larger areas indicate regions where the nucleus has more influence.
                /// Used for weighted calculations and force simulations.
                /// </summary>
                public float area = 1f;

                /// <summary>
                /// The magnitude of force applied to this cell during physics simulations.
                /// Used for relaxation algorithms or dynamic Voronoi animations.
                /// </summary>
                public float forceMagnitude = 1f;

                /// <summary>
                /// The current velocity of the cell's nucleus.
                /// Used in physics-based relaxation or animated Voronoi diagrams.
                /// </summary>
                public Vector3 velocity = Vector3.zero;

                /// <summary>
                /// Weight/importance factor for weighted Voronoi diagrams.
                /// Higher weights cause cells to "claim" more space around their nucleus.
                /// In standard Voronoi diagrams, all weights are equal (1.0).
                /// </summary>
                public float weight = 1f;

                /// <summary>
                /// Renders the Voronoi cell's boundary for visualization.
                /// Should draw lines connecting the boundary vertices in order.
                /// </summary>
                public void Draw()
                {
                    if (boundaryEdges == null || boundaryEdges.Length == 0) return;

                    // Draw each boundary edge
                    foreach (EdgeBehavior edge in boundaryEdges)
                    {
                        if (edge != null && edge.connections != null && edge.connections.Length > 0)
                        {
                            LineRenderer lr = edge.GetComponent<LineRenderer>();
                            if (lr != null)
                            {
                                lr.enabled = true;
                            }
                        }
                    }

                    // Draw nucleus marker
                    if (nucleus != null)
                    {
                        Debug.DrawRay(nucleus.position, Vector3.up * 0.5f, Color.red, 0.1f);
                    }

                    // Draw centroid marker
                    Debug.DrawRay(centroidPosition, Vector3.up * 0.3f, Color.green, 0.1f);
                }

                /// <summary>
                /// Calculates and updates the centroid position of this cell.
                /// 
                /// ALGORITHM:
                /// 1. Find all vertices of the boundary polygon
                /// 2. Calculate average position: centroid = Σ(vertices) / vertex_count
                /// 
                /// The centroid represents the "center of mass" of the cell.
                /// </summary>
                public void UpdateCentroid()
                {
                    if (boundaryEdges == null || boundaryEdges.Length == 0)
                    {
                        // If no boundary edges, centroid is at the nucleus
                        centroidPosition = nucleus != null ? nucleus.position : Vector3.zero;
                        return;
                    }

                    // Collect all unique vertices from boundary edges
                    System.Collections.Generic.List<Vector3> vertices = new System.Collections.Generic.List<Vector3>();
                    System.Collections.Generic.HashSet<Vector3> uniqueVertices = new System.Collections.Generic.HashSet<Vector3>();

                    foreach (EdgeBehavior edge in boundaryEdges)
                    {
                        if (edge != null && edge.connections != null && edge.connections.Length > 0)
                        {
                            EdgeBehavior.NodeConnection conn = edge.connections[0];
                            if (conn.sourceNode != null && uniqueVertices.Add(conn.sourceNode.position))
                            {
                                vertices.Add(conn.sourceNode.position);
                            }
                            if (conn.targetNode != null && uniqueVertices.Add(conn.targetNode.position))
                            {
                                vertices.Add(conn.targetNode.position);
                            }
                        }
                    }

                    // Calculate average position
                    if (vertices.Count > 0)
                    {
                        Vector3 sum = Vector3.zero;
                        foreach (Vector3 vertex in vertices)
                        {
                            sum += vertex;
                        }
                        centroidPosition = sum / vertices.Count;
                    }
                    else
                    {
                        centroidPosition = nucleus != null ? nucleus.position : Vector3.zero;
                    }
                }

                /// <summary>
                /// Calculates the area of the Voronoi cell's polygon.
                /// 
                /// SHOELACE FORMULA (for 2D polygons):
                /// Area = 0.5 * |Σ(x[i] * y[i+1] - x[i+1] * y[i])|
                /// where vertices are ordered clockwise or counterclockwise.
                /// 
                /// Used for weighted forces and determining cell importance.
                /// </summary>
                public void UpdateArea()
                {
                    if (boundaryEdges == null || boundaryEdges.Length == 0)
                    {
                        area = 1f; // Default area
                        return;
                    }

                    // Collect all unique vertices and order them
                    System.Collections.Generic.List<Vector3> vertices = new System.Collections.Generic.List<Vector3>();
                    System.Collections.Generic.HashSet<Vector3> uniqueVertices = new System.Collections.Generic.HashSet<Vector3>();

                    foreach (EdgeBehavior edge in boundaryEdges)
                    {
                        if (edge != null && edge.connections != null && edge.connections.Length > 0)
                        {
                            EdgeBehavior.NodeConnection conn = edge.connections[0];
                            if (conn.sourceNode != null && uniqueVertices.Add(conn.sourceNode.position))
                            {
                                vertices.Add(conn.sourceNode.position);
                            }
                            if (conn.targetNode != null && uniqueVertices.Add(conn.targetNode.position))
                            {
                                vertices.Add(conn.targetNode.position);
                            }
                        }
                    }

                    if (vertices.Count < 3)
                    {
                        area = 0f;
                        return;
                    }

                    // Order vertices by angle from centroid (for proper polygon traversal)
                    Vector3 center = nucleus != null ? nucleus.position : centroidPosition;
                    vertices.Sort((a, b) =>
                    {
                        float angleA = Mathf.Atan2(a.y - center.y, a.x - center.x);
                        float angleB = Mathf.Atan2(b.y - center.y, b.x - center.x);
                        return angleA.CompareTo(angleB);
                    });

                    // Apply Shoelace formula
                    float sum = 0f;
                    for (int i = 0; i < vertices.Count; i++)
                    {
                        Vector3 current = vertices[i];
                        Vector3 next = vertices[(i + 1) % vertices.Count];
                        sum += (current.x * next.y) - (next.x * current.y);
                    }

                    area = Mathf.Abs(sum) * 0.5f;
                }

                /// <summary>
                /// Applies Lloyd's Relaxation algorithm to this cell.
                /// 
                /// LLOYD'S ALGORITHM:
                /// 1. Calculate the centroid of the Voronoi cell
                /// 2. Move the nucleus toward the centroid
                /// 3. Rebuild the Voronoi diagram
                /// 4. Repeat until convergence
                /// 
                /// RESULT:
                /// Creates a more uniform, aesthetically pleasing distribution of points.
                /// Cells become more regular and similar in size (approaching hexagons).
                /// 
                /// APPLICATIONS:
                /// - Stippling and artistic rendering
                /// - Procedural content generation (evenly distributed resources)
                /// - Blue noise sampling
                /// </summary>
                public void ApplyLloydRelaxation()
                {
                    if (nucleus == null) return;

                    // Update centroid first
                    UpdateCentroid();

                    // Relaxation factor (0.0 = no movement, 1.0 = full movement to centroid)
                    float relaxationFactor = 0.5f;

                    // Move nucleus toward centroid
                    nucleus.position = Vector3.Lerp(nucleus.position, centroidPosition, relaxationFactor);
                }

                /// <summary>
                /// Updates the position of this cell's nucleus based on its velocity.
                /// Used in physics-based simulations or animated relaxation.
                /// </summary>
                public void MoveCell()
                {
                    if (nucleus == null) return;

                    // Apply velocity to nucleus position
                    nucleus.position += velocity * Time.deltaTime;
                }

                /// <summary>
                /// Applies a repulsive force to move a neighboring cell away.
                /// Used in force-directed layout algorithms.
                /// 
                /// PHYSICS ANALOGY:
                /// Treats cells like charged particles that repel each other,
                /// pushing toward equilibrium spacing.
                /// </summary>
                /// <param name="neighbour">The neighboring cell to push away</param>
                public void MoveNeighbor(VoronoiCell neighbour)
                {
                    if (neighbour == null || neighbour.nucleus == null || nucleus == null) return;

                    // Calculate repulsion vector
                    Vector3 direction = neighbour.nucleus.position - nucleus.position;
                    float distance = direction.magnitude;

                    // Avoid division by zero
                    if (distance < 0.01f) return;

                    direction.Normalize();

                    // Repulsive force inversely proportional to distance
                    float repulsionStrength = 1.0f / (distance * distance);
                    repulsionStrength *= weight * neighbour.weight; // Consider cell weights

                    // Apply force to neighbor's velocity
                    Vector3 force = direction * repulsionStrength;
                    neighbour.velocity += force * Time.deltaTime;
                }

                /// <summary>
                /// Calculates forces acting on this cell from neighboring cells.
                /// 
                /// FORCE TYPES:
                /// - Repulsion: Cells push each other apart
                /// - Attraction to centroid: For Lloyd's relaxation
                /// - Boundary forces: Keep cells within bounds
                /// 
                /// Results stored in forceMagnitude and used to update velocity.
                /// </summary>
                public void CalculateForces()
                {
                    if (nucleus == null) return;

                    Vector3 totalForce = Vector3.zero;

                    // 1. Attraction to centroid (Lloyd's relaxation force)
                    Vector3 toCentroid = centroidPosition - nucleus.position;
                    float centroidDistance = toCentroid.magnitude;
                    if (centroidDistance > 0.01f)
                    {
                        totalForce += toCentroid.normalized * centroidDistance * 0.5f;
                    }

                    // 2. Repulsion from neighbors
                    if (neighboringCells != null)
                    {
                        foreach (VoronoiCell neighbor in neighboringCells)
                        {
                            if (neighbor == null || neighbor.nucleus == null) continue;

                            Vector3 toNeighbor = neighbor.nucleus.position - nucleus.position;
                            float distance = toNeighbor.magnitude;

                            if (distance > 0.01f)
                            {
                                // Repulsive force (inverse square law)
                                float repulsion = (weight * neighbor.weight) / (distance * distance);
                                totalForce -= toNeighbor.normalized * repulsion;
                            }
                        }
                    }

                    // 3. Boundary forces (optional - keeps cells within bounds)
                    // Example: push cells away from edges of the map
                    // This would require knowing the map bounds

                    forceMagnitude = totalForce.magnitude;
                }

                /// <summary>
                /// Updates the cell's velocity based on calculated forces.
                /// 
                /// PHYSICS:
                /// velocity += (force / mass) * deltaTime
                /// Apply damping to prevent oscillation: velocity *= dampingFactor
                /// </summary>
                public void UpdateVelocity()
                {
                    if (nucleus == null) return;

                    // Calculate acceleration from forces
                    // Assume mass = area (larger cells have more inertia)
                    float mass = Mathf.Max(area, 0.1f);
                    Vector3 acceleration = (forceMagnitude * velocity.normalized) / mass;

                    // Update velocity
                    velocity += acceleration * Time.deltaTime;

                    // Apply damping to prevent oscillation (0.9 = 10% velocity loss per frame)
                    float dampingFactor = 0.9f;
                    velocity *= dampingFactor;

                    // Clamp velocity to prevent extreme speeds
                    float maxVelocity = 10f;
                    if (velocity.magnitude > maxVelocity)
                    {
                        velocity = velocity.normalized * maxVelocity;
                    }
                }

                /// <summary>
                /// Updates the nucleus position based on current velocity.
                /// Should be called after UpdateVelocity() in physics simulation loop.
                /// </summary>
                public void UpdatePosition()
                {
                    if (nucleus == null) return;

                    // Update position based on velocity
                    nucleus.position += velocity * Time.deltaTime;

                    // Optional: Clamp to boundaries
                    // nucleus.position.x = Mathf.Clamp(nucleus.position.x, minX, maxX);
                    // nucleus.position.y = Mathf.Clamp(nucleus.position.y, minY, maxY);
                }

                /// <summary>
                /// Resets all force accumulators to zero.
                /// Should be called at the start of each physics frame before calculating new forces.
                /// </summary>
                public void ResetForces()
                {
                    forceMagnitude = 0f;
                    // If using Vector3 for directional forces, reset that too
                }
            }

            /// <summary>
            /// Represents a triangle in the Delaunay Triangulation.
            /// 
            /// DELAUNAY PROPERTY:
            /// No vertex lies inside the circumcircle of any triangle.
            /// This maximizes the minimum angle of all triangles, avoiding "sliver" triangles.
            /// 
            /// RELATIONSHIP TO VORONOI:
            /// - Each triangle's circumcenter is a vertex in the Voronoi diagram
            /// - Edges connect circumcenters of adjacent triangles
            /// - Delaunay edges are dual to Voronoi edges (perpendicular bisectors)
            /// </summary>
            [System.Serializable]
            public class DelaunayTriangle
            {
                /// <summary>
                /// The three vertices that define this triangle.
                /// In a Voronoi diagram, these are the nuclei of three adjacent cells.
                /// Order matters for determining orientation (clockwise/counterclockwise).
                /// </summary>
                public NodeBehavior vertexA;
                public NodeBehavior vertexB;
                public NodeBehavior vertexC;

                /// <summary>
                /// The area of the triangle.
                /// Used for quality metrics and weighted calculations.
                /// Can be calculated using the cross product: 0.5 * |AB × AC|
                /// </summary>
                public float area;

                /// <summary>
                /// Renders the Delaunay triangle for visualization.
                /// Useful for debugging and understanding the dual graph relationship.
                /// </summary>
                public void Draw()
                {
                    if (vertexA == null || vertexB == null || vertexC == null) return;

                    // Draw triangle edges
                    Debug.DrawLine(vertexA.position, vertexB.position, Color.blue, 0.1f);
                    Debug.DrawLine(vertexB.position, vertexC.position, Color.blue, 0.1f);
                    Debug.DrawLine(vertexC.position, vertexA.position, Color.blue, 0.1f);

                    // Draw circumcenter
                    Vector3 circumcenter = GetCircumcenter();
                    Debug.DrawRay(circumcenter, Vector3.up * 0.2f, Color.yellow, 0.1f);
                }

                /// <summary>
                /// Checks if this triangle is valid (non-degenerate).
                /// 
                /// A triangle is invalid if:
                /// - Any two vertices are the same
                /// - All three vertices are collinear (zero area)
                /// - Any vertex is null
                /// </summary>
                /// <returns>True if the triangle is valid for use in the triangulation</returns>
                public bool IsValid()
                {
                    // Check for null vertices
                    if (vertexA == null || vertexB == null || vertexC == null)
                        return false;

                    // Check vertices are distinct
                    if (vertexA == vertexB || vertexB == vertexC || vertexC == vertexA)
                        return false;

                    // Check if positions are distinct
                    if (Vector3.Distance(vertexA.position, vertexB.position) < 0.001f ||
                        Vector3.Distance(vertexB.position, vertexC.position) < 0.001f ||
                        Vector3.Distance(vertexC.position, vertexA.position) < 0.001f)
                        return false;

                    // Calculate area and ensure it's above epsilon threshold
                    CalculateArea();
                    return area > 0.001f; // Epsilon for numerical stability
                }

                /// <summary>
                /// Tests if a point lies inside this triangle.
                /// 
                /// BARYCENTRIC COORDINATE METHOD:
                /// A point P is inside triangle ABC if all barycentric coordinates are positive:
                /// - P = uA + vB + wC, where u + v + w = 1
                /// - If u, v, w ≥ 0, point is inside
                /// 
                /// USED FOR:
                /// - Point location queries
                /// - Delaunay condition checking (is point in circumcircle?)
                /// - Interpolation within the triangle
                /// </summary>
                /// <param name="point">The point to test</param>
                /// <returns>True if the point is inside the triangle</returns>
                public bool ContainsPoint(Vector3 point)
                {
                    if (vertexA == null || vertexB == null || vertexC == null)
                        return false;

                    // Use barycentric coordinates
                    Vector3 v0 = vertexC.position - vertexA.position;
                    Vector3 v1 = vertexB.position - vertexA.position;
                    Vector3 v2 = point - vertexA.position;

                    float dot00 = Vector3.Dot(v0, v0);
                    float dot01 = Vector3.Dot(v0, v1);
                    float dot02 = Vector3.Dot(v0, v2);
                    float dot11 = Vector3.Dot(v1, v1);
                    float dot12 = Vector3.Dot(v1, v2);

                    float invDenom = 1f / (dot00 * dot11 - dot01 * dot01);
                    float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
                    float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

                    // Check if point is in triangle
                    return (u >= 0) && (v >= 0) && (u + v <= 1);
                }

                /// <summary>
                /// Calculates the circumcenter of the triangle.
                /// 
                /// CIRCUMCENTER:
                /// The point equidistant from all three vertices.
                /// Center of the circumcircle (the circle passing through all vertices).
                /// 
                /// IMPORTANCE IN VORONOI:
                /// The circumcenter becomes a vertex in the Voronoi diagram!
                /// Voronoi edges connect circumcenters of adjacent Delaunay triangles.
                /// 
                /// CALCULATION (2D on XZ plane for Unity 3D):
                /// Uses perpendicular bisectors of triangle edges in the XZ plane.
                /// Formula involves edge lengths and determinants, adapted for XZ coordinates.
                /// </summary>
                /// <returns>The circumcenter position</returns>
                public Vector3 GetCircumcenter()
                {
                    if (vertexA == null || vertexB == null || vertexC == null)
                        return Vector3.zero;

                    Vector3 a = vertexA.position;
                    Vector3 b = vertexB.position;
                    Vector3 c = vertexC.position;

                    // Calculate using the formula for 2D circumcenter in XZ plane
                    // Using X and Z coordinates instead of X and Y
                    float d = 2f * (a.x * (b.z - c.z) + b.x * (c.z - a.z) + c.x * (a.z - b.z));

                    if (Mathf.Abs(d) < 0.001f)
                        return (a + b + c) / 3f; // Fallback to centroid if degenerate

                    // Calculate squared magnitudes in XZ plane
                    float aSq = a.x * a.x + a.z * a.z;
                    float bSq = b.x * b.x + b.z * b.z;
                    float cSq = c.x * c.x + c.z * c.z;

                    // Calculate circumcenter X coordinate
                    float ux = (aSq * (b.z - c.z) + bSq * (c.z - a.z) + cSq * (a.z - b.z)) / d;

                    // Calculate circumcenter Z coordinate (using X positions with swapped formula)
                    float uz = (aSq * (c.x - b.x) + bSq * (a.x - c.x) + cSq * (b.x - a.x)) / d;

                    // Return with Y coordinate averaged from input vertices (maintains height)
                    return new Vector3(ux, (a.y + b.y + c.y) / 3f, uz);
                }

                /// <summary>
                /// Calculates the radius of the circumcircle.
                /// 
                /// USED FOR:
                /// - Delaunay condition testing (is point inside circumcircle?)
                /// - Quality metrics (aspect ratio)
                /// 
                /// FORMULA:
                /// R = (abc) / (4 * Area)
                /// where a, b, c are edge lengths
                /// </summary>
                /// <returns>The circumradius</returns>
                public float GetCircumradius()
                {
                    if (vertexA == null || vertexB == null || vertexC == null)
                        return 0f;

                    Vector3 circumcenter = GetCircumcenter();
                    return Vector3.Distance(circumcenter, vertexA.position);
                }

                /// <summary>
                /// Checks if this triangle shares an edge with another triangle.
                /// 
                /// TWO TRIANGLES SHARE AN EDGE IF:
                /// They have exactly two vertices in common.
                /// 
                /// USED FOR:
                /// - Building the dual Voronoi diagram
                /// - Triangle adjacency queries
                /// - Edge-flipping in incremental construction
                /// </summary>
                /// <param name="other">The triangle to compare with</param>
                /// <returns>True if the triangles share an edge</returns>
                public bool SharesEdge(DelaunayTriangle other)
                {
                    if (other == null) return false;

                    int sharedVertices = 0;

                    if (vertexA == other.vertexA || vertexA == other.vertexB || vertexA == other.vertexC)
                        sharedVertices++;
                    if (vertexB == other.vertexA || vertexB == other.vertexB || vertexB == other.vertexC)
                        sharedVertices++;
                    if (vertexC == other.vertexA || vertexC == other.vertexB || vertexC == other.vertexC)
                        sharedVertices++;

                    return sharedVertices == 2;
                }

                /// <summary>
                /// Gets the shared edge between this triangle and another.
                /// 
                /// VORONOI CONSTRUCTION:
                /// The shared edge in Delaunay triangulation corresponds to
                /// a Voronoi edge (perpendicular bisector) connecting the circumcenters.
                /// </summary>
                /// <param name="other">The adjacent triangle</param>
                /// <returns>The shared edge, or null if triangles don't share an edge</returns>
                public EdgeBehavior GetSharedEdge(DelaunayTriangle other)
                {
                    if (!SharesEdge(other)) return null;

                    // Find the two shared vertices
                    System.Collections.Generic.List<NodeBehavior> sharedVerts = new System.Collections.Generic.List<NodeBehavior>();

                    if (vertexA == other.vertexA || vertexA == other.vertexB || vertexA == other.vertexC)
                        sharedVerts.Add(vertexA);
                    if (vertexB == other.vertexA || vertexB == other.vertexB || vertexB == other.vertexC)
                        sharedVerts.Add(vertexB);
                    if (vertexC == other.vertexA || vertexC == other.vertexB || vertexC == other.vertexC)
                        sharedVerts.Add(vertexC);

                    if (sharedVerts.Count != 2) return null;

                    // Create an EdgeBehavior connecting the shared vertices
                    GameObject edgeObj = new GameObject($"Edge_{sharedVerts[0].nodeName}_to_{sharedVerts[1].nodeName}");
                    EdgeBehavior edge = edgeObj.AddComponent<EdgeBehavior>();
                    edge.connections = new EdgeBehavior.NodeConnection[1];
                    edge.connections[0] = new EdgeBehavior.NodeConnection
                    {
                        sourceNode = sharedVerts[0],
                        targetNode = sharedVerts[1],
                        weight = Vector3.Distance(sharedVerts[0].position, sharedVerts[1].position)
                    };

                    return edge;
                }

                /// <summary>
                /// Updates the triangle's computed properties (area, circumcenter, etc.).
                /// Should be called when any vertex position changes.
                /// </summary>
                public void Update()
                {
                    CalculateArea();
                    // If caching circumcenter, recalculate it here
                }

                /// <summary>
                /// Calculates and stores the triangle's area.
                /// 
                /// FORMULA (3D cross product):
                /// Area = 0.5 * |AB × AC|
                /// where AB = B - A, AC = C - A
                /// 
                /// FORMULA (2D shoelace):
                /// Area = 0.5 * |x₁(y₂-y₃) + x₂(y₃-y₁) + x₃(y₁-y₂)|
                /// </summary>
                public void CalculateArea()
                {
                    if (vertexA == null || vertexB == null || vertexC == null)
                    {
                        area = 0f;
                        return;
                    }

                    // Using cross product method (works in 3D)
                    Vector3 AB = vertexB.position - vertexA.position;
                    Vector3 AC = vertexC.position - vertexA.position;
                    Vector3 cross = Vector3.Cross(AB, AC);
                    area = cross.magnitude * 0.5f;
                }
            }

            /// <summary>
            /// Doubly-Connected Edge List (DCEL) - A data structure for representing planar subdivisions.
            /// 
            /// DCEL COMPONENTS:
            /// - Vertices: Points in space (origins of half-edges)
            /// - Half-edges: Directed edges with a twin pointing the opposite direction
            /// - Faces: Regions bounded by half-edges (Voronoi cells or Delaunay triangles)
            /// 
            /// ADVANTAGES:
            /// - Efficient traversal of boundaries
            /// - Easy to find adjacent faces
            /// - Supports topological queries in O(1) time
            /// 
            /// STRUCTURE:
            /// Each half-edge stores: origin vertex, twin edge, next edge, previous edge, and incident face.
            /// This allows efficient navigation around faces and between adjacent faces.
            /// </summary>
            [System.Serializable]
            public class DoublyConnectedEdgeList
            {
                /// <summary>
                /// The face (Delaunay triangle or Voronoi cell) that this half-edge bounds.
                /// In a Voronoi DCEL, this would be a VoronoiCell.
                /// In a Delaunay DCEL, this would be a DelaunayTriangle.
                /// </summary>
                public DelaunayTriangle face;

                /// <summary>
                /// The next half-edge in counterclockwise order around the face.
                /// Following next repeatedly traces the boundary of the face.
                /// </summary>
                public DoublyConnectedEdgeList next;

                /// <summary>
                /// The previous half-edge in counterclockwise order around the face.
                /// Used for bidirectional traversal of face boundaries.
                /// </summary>
                public DoublyConnectedEdgeList prev;

                /// <summary>
                /// The twin (opposite) half-edge.
                /// Points in the opposite direction and bounds the adjacent face.
                /// 
                /// PROPERTY:
                /// If e is a half-edge, then e.twin.twin == e
                /// e.origin == e.twin.destination
                /// </summary>
                public DoublyConnectedEdgeList twin;

                /// <summary>
                /// The vertex at the origin (starting point) of this directed half-edge.
                /// The destination is implicitly twin.origin (or next.origin).
                /// </summary>
                public NodeBehavior origin;

                /// <summary>
                /// Renders this half-edge for visualization.
                /// Can draw an arrow from origin to destination to show direction.
                /// </summary>
                public void Draw()
                {
                    if (origin == null || twin == null || twin.origin == null) return;

                    Vector3 start = origin.position;
                    Vector3 end = twin.origin.position;

                    // Draw the edge
                    Debug.DrawLine(start, end, Color.cyan, 0.1f);

                    // Draw an arrow to show direction
                    Vector3 direction = (end - start).normalized;
                    Vector3 arrowTip = end - direction * 0.2f;
                    Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0) * 0.1f;

                    Debug.DrawLine(end, arrowTip + perpendicular, Color.cyan, 0.1f);
                    Debug.DrawLine(end, arrowTip - perpendicular, Color.cyan, 0.1f);
                }

                /// <summary>
                /// Validates the half-edge's topological integrity.
                /// 
                /// CHECKS:
                /// - Twin exists and twin.twin == this
                /// - Next and prev form a closed loop
                /// - Origin vertex is not null
                /// - Face reference is valid
                /// </summary>
                /// <returns>True if the half-edge structure is valid</returns>
                public bool IsValid()
                {
                    // Check origin is not null
                    if (origin == null) return false;

                    // Check twin relationship
                    if (twin == null || twin.twin != this) return false;

                    // Check next/prev chain
                    if (next == null || prev == null) return false;

                    // Verify next.prev == this and prev.next == this
                    if (next.prev != this || prev.next != this) return false;

                    // Check that following next eventually returns to this edge (closed loop)
                    int maxSteps = 1000; // Prevent infinite loop
                    DoublyConnectedEdgeList current = next;
                    int steps = 0;
                    while (current != this && steps < maxSteps)
                    {
                        current = current.next;
                        steps++;
                    }
                    if (current != this) return false;

                    return true;
                }
            }

            /// <summary>
            /// Maximum number of Voronoi cells (seed points) to support.
            /// Increase for larger maps or more complex diagrams.
            /// </summary>
            public const int maxCellsToSpawn = 100;

            /// <summary>
            /// Array of all Voronoi cells in the diagram.
            /// Each cell represents a region around a nucleus (seed point).
            /// </summary>
            public VoronoiCell[] cells = new VoronoiCell[maxCellsToSpawn];

            /// <summary>
            /// Array of half-edges in the DCEL representation.
            /// Used for efficient topological queries and boundary traversal.
            /// Typically, there are about 3 times as many edges as cells.
            /// </summary>
            public DoublyConnectedEdgeList[] edges = new DoublyConnectedEdgeList[maxCellsToSpawn];

            /// <summary>
            /// Visualizes the Voronoi diagram by drawing cell boundaries and centroids.
            /// 
            /// VISUALIZATION TECHNIQUES:
            /// 1. Draw Voronoi edges (perpendicular bisectors)
            /// 2. Draw cell boundaries (closed polygons)
            /// 3. Mark nuclei (seed points)
            /// 4. Mark centroids (for Lloyd's relaxation)
            /// 5. Color cells by properties (area, distance, etc.)
            /// 
            /// ALGORITHM OUTLINE:
            /// - Iterate through all cells
            /// - For each cell, draw its boundary edges
            /// - Draw a marker at nucleus and centroid positions
            /// - Optionally draw Delaunay triangulation for comparison
            /// </summary>
            public void DrawCentroidVoronoiDiagram()
            {
                if (cells == null) return;

                // Iterate through all cells
                for (int i = 0; i < maxCellsToSpawn; i++)
                {
                    if (cells[i] == null || cells[i].nucleus == null) continue;

                    // Draw each cell
                    cells[i].Draw();

                    // Draw connection from nucleus to centroid
                    Debug.DrawLine(cells[i].nucleus.position, cells[i].centroidPosition, Color.yellow, 0.1f);
                }

                // Optionally draw all DCEL edges
                if (edges != null)
                {
                    for (int i = 0; i < maxCellsToSpawn; i++)
                    {
                        if (edges[i] != null)
                        {
                            edges[i].Draw();
                        }
                    }
                }
            }
        }


        [System.Serializable]
        public class DepthFirstSearch
        {
            public const int maxNodesToSpawn = 100;

            public int[] stack = new int[maxNodesToSpawn];
            public NodeBehavior[] nodes=new NodeBehavior[maxNodesToSpawn];
            public int[,] adjacencyMatrix=new int[maxNodesToSpawn,maxNodesToSpawn];
            public int vertexCount = 10;
            public int top = -1;

            public void Push(int value)
            {
                // Implementation for pushing a value onto the stack
                stack[++top] = value;
            }
            public int Pop()
            {
                // Implementation for popping a value from the stack
                return stack[top--]; // Placeholder return
            }

            public int Peek()
            {
                // Implementation for peeking at the top value of the stack
                return stack[top]; // Placeholder return
            }

            public void AddVertex(string name)
            {
                // Implementation for adding a vertex
                NodeBehavior vertex = new NodeBehavior();
                vertex.nodeName = name;
                vertex.visited = false;

                nodes[vertexCount++] = vertex;
            }

            public void AddEdge(int start, int end)
            {
                // Implementation for adding an edge
                adjacencyMatrix[start, end] = 1;
                adjacencyMatrix[end, start] = 1; // For undirected graph
            }

            public int GetUnvisitedAdjacentVertex(int vertexIndex)
            {
                // Implementation to get an unvisited adjacent vertex
                for (int i = 0; i < vertexCount; i++)
                {
                    if (adjacencyMatrix[vertexIndex, i] == 1 && !nodes[i].visited)
                    {
                        return i;
                    }
                }
                return -1; // No unvisited adjacent vertex found
            }

            public bool IsStackEmpty()
            {
                // Implementation to check if the stack is empty
                return top == -1; // Placeholder return
            }

            public void ResetVisitedFlags()
            {
                for (int i = 0; i < vertexCount; i++)
                {
                    nodes[i].visited = false;
                }
            }

            public void InitAdjacencyMatrix()
            {
                for (int i = 0; i < maxNodesToSpawn; i++)
                {
                    for (int j = 0; j < maxNodesToSpawn; j++)
                    {
                        adjacencyMatrix[i, j] = 0;
                    }
                }
            }

            public void DepthFirstSearchTraversal()
            {
                // Implementation for depth-first search traversal
                nodes[0].visited = true;
                nodes[0].DisplayInfo();
                Push(0);
                while (!IsStackEmpty())
                {
                    int currentVertex = Peek();
                    int adjacentVertex = GetUnvisitedAdjacentVertex(currentVertex);
                    if (adjacentVertex == -1)
                    {
                        Pop();
                    }
                    else
                    {
                        nodes[adjacentVertex].visited = true;
                        nodes[adjacentVertex].DisplayInfo();
                        Push(adjacentVertex);
                    }
                }

                // Reset the visited flags for future traversals
                ResetVisitedFlags();
            }
        }


        [System.Serializable]
        public class BreadthFirstSearch
        {
            public const int maxNodesToSpawn = 100;
            public int[] queue = new int[maxNodesToSpawn];
            int front = 0;
            int rear = -1;
            public int queueSize = 0;
            public NodeBehavior[] listNodes = new NodeBehavior[maxNodesToSpawn];
            public int[,] adjacencyMatrixBFS = new int[maxNodesToSpawn, maxNodesToSpawn];
            public int vertexCountBFS = 10;

            public void Insert(int value)
            {
                // Implementation for inserting a value into the queue
                queue[++rear] = value;
                queueSize++;
            }

            public int Remove()
            {
                // Implementation for removing a value from the queue
                queueSize--;
                return queue[front++];
            }

            public bool IsQueueEmpty()
            {
                // Implementation to check if the queue is empty
                return queueSize == 0;
            }

            public void AddVertexBFS(string name)
            {
                // Implementation for adding a vertex
                NodeBehavior vertex = new NodeBehavior();
                vertex.nodeName = name;
                vertex.visited = false;
                listNodes[vertexCountBFS++] = vertex;
            }

            public void AddEdgeBFS(int start, int end)
            {
                // Implementation for adding an edge
                adjacencyMatrixBFS[start, end] = 1;
                adjacencyMatrixBFS[end, start] = 1; // For undirected graph
            }

            public int GetUnvisitedAdjacentVertexBFS(int vertexIndex)
            {
                // Implementation to get an unvisited adjacent vertex
                for (int i = 0; i < vertexCountBFS; i++)
                {
                    if (adjacencyMatrixBFS[vertexIndex, i] == 1 && !listNodes[i].visited)
                    {
                        return i;
                    }
                }
                return -1; // No unvisited adjacent vertex found
            }

            public void ResetVisitedFlagsBFS()
            {
                for (int i = 0; i < vertexCountBFS; i++)
                {
                    listNodes[i].visited = false;
                }
            }

            public void InitAdjacencyMatrixBFS()
            {
                for (int i = 0; i < maxNodesToSpawn; i++)
                {
                    for (int j = 0; j < maxNodesToSpawn; j++)
                    {
                        adjacencyMatrixBFS[i, j] = 0;
                    }
                }
            }

            public void BreadthFirstSearchTraversal()
            {
                // Implementation for breadth-first search traversal
                listNodes[0].visited = true;
                listNodes[0].DisplayInfo();
                Insert(0);
                while (!IsQueueEmpty())
                {
                    int currentVertex = Remove();
                    int adjacentVertex;
                    while ((adjacentVertex = GetUnvisitedAdjacentVertexBFS(currentVertex)) != -1)
                    {
                        listNodes[adjacentVertex].visited = true;
                        listNodes[adjacentVertex].DisplayInfo();
                        Insert(adjacentVertex);
                    }
                }
                // Reset the visited flags for future traversals
                ResetVisitedFlagsBFS();
            }
        }

        

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Awake()
        {
            Instance = this;
        }

        // Update is called once per frame
        void Update()
        {

        }
    }

}
