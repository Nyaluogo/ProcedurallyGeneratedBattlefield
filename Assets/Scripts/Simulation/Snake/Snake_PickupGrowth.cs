using UnityEngine;

namespace GraphTheory
{
    public class Snake_PickupGrowth : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            // Ensure pickup has a trigger collider
            if (!GetComponent<Collider>())
            {
                var collider = gameObject.AddComponent<SphereCollider>();
                collider.isTrigger = true;
            }
        }

        // Update is called once per frame
        void Update()
        {

        }

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log($"Collision detected with: {other.gameObject.name}");
            
            // Check if we hit the snake head
            if (other.gameObject.name == "Head")
            {
                Debug.Log("Snake Growth Pickup Collected by Head");
                var snake = Snake_LinkedList.Instance;
                if (snake != null)
                {
                    snake.Grow();
                    Destroy(gameObject);
                }
                else
                {
                    Debug.LogError("Could not find Snake_LinkedList instance!");
                }
            }
        }
    }
}