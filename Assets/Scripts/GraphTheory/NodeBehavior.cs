using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GraphTheory
{
    public class NodeBehavior : MonoBehaviour
    {
        public enum NodeType
        {
            Standard,
            Start,
            End,
            Obstacle
        }
        public NodeType nodeType = NodeType.Standard;

        public enum NodeState
        {
            Unvisited,
            Open,
            Closed,
            Path
        }
        public NodeState nodeState = NodeState.Unvisited;


        // Unique identifier for the node
        public int nodeId;
        public string nodeName;
        // Position of the node in the graph
        public Vector3 position;
        public int priority; // For use in sorting/searching algorithms
        // Method to initialize the node with an ID and position
        public GameObject dataObj;
        public TextMeshProUGUI label;
        public bool visited=false;

        public NodeBehavior previousNode=null;
        public NodeBehavior nextNode=null;

        public bool isStationary=true;

        public void Initialize(int id, Vector3 pos)
        {
            nodeId = id;
            position = pos;
            transform.position = pos;
        }
        // Method to display node information (for debugging purposes)
        public void DisplayInfo()
        {
            if(label != null)
            {
                   label.text = $"ID: {nodeId}_{nodeName}\nPosition: {position}";
            }

            Debug.Log($"Node ID: {nodeId}, Position: {position}");
        }

        private void Update()
        {
            if(!isStationary)
                transform.position = position;
        }


    }
}   
