using System.Collections;
using UnityEngine;

namespace GraphTheory
{
    public class CardPlayer : MonoBehaviour
    {
        public string playerName = "Player 1";
        public int totalPoints = 0;
        public bool isBot = false;

        NodeBehavior vertexBehavior;
        // Use this for initialization
        void Start()
        {
            vertexBehavior = GetComponent<NodeBehavior>();
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}