using System.Collections;
using UnityEngine;

namespace GraphTheory
{
    public class SnakeBody : MonoBehaviour
    {
        NodeBehavior nodeBehavior;
        // Use this for initialization
        void Start()
        {
            nodeBehavior = GetComponent<NodeBehavior>();
        }

        // Update is called once per frame
        void Update()
        {

        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                //Cut the snake if it collides with itself
                if(nodeBehavior != null) 
                {
                    Snake_LinkedList.Instance.CutSnakeAtNode(nodeBehavior);
                }

            }
        }
    
    }
}