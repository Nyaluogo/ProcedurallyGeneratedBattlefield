using System.Collections;
using UnityEngine;

namespace GraphTheory
{
    public class Voronoi_Tower : MonoBehaviour
    {
        NodeBehavior nodeBehavior;

        public int towerID;
        public int towerLevel;
        public float shootingRange=2f;
        public float shootingPower=5f;
        public float maxHealth = 100f;
        public float health = 100f;
        // Use this for initialization
        void Start()
        {
            nodeBehavior = GetComponent<NodeBehavior>();
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}