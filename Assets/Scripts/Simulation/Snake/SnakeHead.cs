using System.Collections;
using UnityEngine;

namespace GraphTheory
{
    public class SnakeHead : MonoBehaviour
    {
        NodeBehavior nodeBehavior;
        MeshRenderer meshRenderer;
        // Use this for initialization
        void Start()
        {
            nodeBehavior = GetComponent<NodeBehavior>();
            meshRenderer = GetComponent<MeshRenderer>();
        }


        // Update is called once per frame
        void Update()
        {

        }


    }
}