using UnityEngine;

namespace GraphTheory
{
    public class SimulationController : MonoBehaviour
    {
        public enum SimulationMode
        {
            SNAKE,
            CARDS,
            BATTLEHEAP
        }
        public SimulationMode CurrentMode = SimulationMode.SNAKE;

        public Snake_LinkedList snakeGame;
        public Card_Stacks cardGame;
        public BattleHeap_Rules battleGame;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void SetSimulationMode(SimulationMode mode)
        {
            CurrentMode = mode;
            // Additional logic to handle mode change can be added here

            switch (CurrentMode)
            {
                case SimulationMode.SNAKE:
                    snakeGame.gameObject.SetActive(true);
                    cardGame.gameObject.SetActive(false);
                    battleGame.gameObject.SetActive(false);
                    break;
                case SimulationMode.CARDS:
                    snakeGame.gameObject.SetActive(false);
                    cardGame.gameObject.SetActive(true);
                    battleGame.gameObject.SetActive(false);
                    break;
                case SimulationMode.BATTLEHEAP:
                    snakeGame.gameObject.SetActive(false);
                    cardGame.gameObject.SetActive(false);
                    battleGame.gameObject.SetActive(true);
                    break;
                default:
                    break;
            }
        }


    }
}
