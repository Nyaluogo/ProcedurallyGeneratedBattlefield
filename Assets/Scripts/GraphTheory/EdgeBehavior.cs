using UnityEngine;

namespace GraphTheory
{

    public class EdgeBehavior : MonoBehaviour
    {
        [System.Serializable]
        public struct NodeConnection
        {
            public NodeBehavior sourceNode;
            public NodeBehavior targetNode;
            public float weight;
        }
        public NodeConnection[] connections;

        public GameObject dataObj;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
