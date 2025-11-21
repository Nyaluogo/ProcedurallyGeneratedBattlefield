using System;
using System.Collections;
using UnityEngine;
using static GraphTheory.GraphMaster;

namespace GraphTheory
{
    public class CardRules : MonoBehaviour
    {
        public static CardRules Instance { get; private set; }

        CardRules() { Instance = this; }

        private float moveTimer = 0f;

        QueueProperties playingTurnQueue;
        public int numOfPlayers = 2;
        public CardPlayer[] players;

        // Click debounce to avoid multiple triggers on a single press/frame
        private Camera mainCamera;
        private float clickCooldown = 0.1f;
        private float lastClickTime = -1f;

        public float cpuMoveDelay = 3.0f;
        Coroutine cpuMoveCoroutine = null;

        [System.Serializable]
        public class CardFaceProperties
        {
            public string cardName;
            public string cardSymbol = "Diamonds";
            public Material cardImage;
            public int cardValue;
            public string cardDescription;
        }

        [System.Serializable]
        public class DiscardSequence
        {
            public string name;
            public string description;
            public string[] sequence;
            public int points;

        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        
        }

        // Use this for initialization
        void Start()
        {
            mainCamera = Camera.main;
            SetInitialReferences();
        }

        // Update is called once per frame
        void Update()
        {
            var stacks = Card_Stacks.Instance;

            if (stacks == null)
                return;

            // process click input on mouse-down only (single centralized raycast)
            if (Input.GetMouseButtonDown(0) && Time.time - lastClickTime > clickCooldown)
            {
                lastClickTime = Time.time;
                ProcessPointerClick();
            }

            SetUpdateReferences();
        }

        void SetInitialReferences()
        {
            SetPlayingTurnQueue();
        }

        void SetUpdateReferences()
        {
            moveTimer += Time.deltaTime;

            if (moveTimer > 0f)
            {
                var stacks = Card_Stacks.Instance;

                if (stacks == null)
                    return;

                ManagePlayTurns();

                // Keep existing periodic checks for top-card interactivity if needed
                if (stacks.GetDrawPile() != null)
                {
                    if (stacks.GetDrawPile().IsStackEmpty() == false)
                    {
                        // no longer calling CheckDrawInput which did per-card raycasts
                        // Top-card interactivity is managed by Card_Stacks.SetDrawstackDiscression()
                    }

                }

                if (stacks.GetPlayerHand() != null)
                {
                    if (stacks.GetPlayerHand().head != null)
                    {
                        // player hand updates/visibility handled elsewhere
                    }

                }
                moveTimer = 0f;
            }
        }

        public void ManagePlayTurns()
        {
            if (playingTurnQueue == null || playingTurnQueue.IsQueueEmpty())
                return;

            var currentPlayer = playingTurnQueue.Peek();

            if (currentPlayer == null) return;
            var player = currentPlayer.GetComponent<CardPlayer>();
            if (player == null) return;

            if (player.isBot == true)
            {
                // If a CPU turn isn't already scheduled, schedule one
                if (cpuMoveCoroutine == null)
                {
                    cpuMoveCoroutine = StartCoroutine(DelayedCpuMove(currentPlayer));
                }
            }
            else
            {
                // Human player's turn -> cancel any pending CPU coroutine
                if (cpuMoveCoroutine != null)
                {
                    StopCoroutine(cpuMoveCoroutine);
                    cpuMoveCoroutine = null;
                }
                // wait for player input.
            }
        }

        // Centralized click handling
        private void ProcessPointerClick()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera == null)
            {
                Debug.LogWarning("No main camera found for click processing.");
                return;
            }

            ///TODO: check if the top Drawpile card as been clicked, if yes, draw a card
            // Draw a card for the current player
            if(playingTurnQueue == null || playingTurnQueue.IsQueueEmpty())
                return;
            var currentPlayer = playingTurnQueue.Peek();
            if (currentPlayer == null) return;
            var player = currentPlayer.GetComponent<CardPlayer>();

            if(player == null) return;

            if (!player.isBot == true)
            {

                var top_draw_card = Card_Stacks.Instance.GetDrawPile().Peek();
                if (top_draw_card != null)
                {
                    var top_face = top_draw_card.dataObj.GetComponent<CardFace>();
                    if (top_face.IsClicked())
                    {

                        TryDrawing(currentPlayer);
                        return; // Exit after processing draw action
                    }
                }

                ///TODO: check if a card in the player's hand has been clicked, if yes, discard that card
                var player_hand = Card_Stacks.Instance.GetPlayerHand();

                if (player_hand.head != null)
                {
                    NodeBehavior current = player_hand.head;
                    while (current != null)
                    {
                        if (current.dataObj != null)
                        {
                            var cardFace = current.dataObj.GetComponent<CardFace>();
                            if (cardFace != null && cardFace.IsClicked())
                            {
                                // Discard the clicked card
                                TryDiscarding(playingTurnQueue.Peek(), current);
                                return; // Exit after processing one card
                            }
                        }
                        current = current.nextNode;
                    }
                }
            }


            
        }

        private void TryDiscarding(NodeBehavior player, NodeBehavior card)
        {
            var stacks = Card_Stacks.Instance;
            if (stacks == null)
            {
                Debug.LogWarning("Card_Stacks.Instance is null in TryDiscarding.");
                return;
            }

            if (card == null)
            {
                Debug.LogWarning("TryDiscarding called with null card.");
                return;
            }

            // Ensure player's hand exists
            var playerHand = stacks.GetPlayerHand();
            if (playerHand == null)
            {
                Debug.Log("Failed to discard the card. Player hand is null.");
                return;
            }

            var player_turn = playingTurnQueue.Peek();

            if(player_turn != null)
            {
                if(player_turn.GetComponent<CardPlayer>().isBot == true)
                {
                    playerHand = stacks.GetCpuHand();
                    if (playerHand == null)
                    {
                        Debug.Log("Failed to discard the card. Player hand is null.");
                        return;
                    }
                    stacks.DiscardCard(card, false);
                }
                else
                {
                    stacks.DiscardCard(card, true);
                }
            }

            // Use DiscardCard with the clicked card
            
            Debug.Log($"{player.nodeName} discarded a card: {card.nodeName}");

            SwitchTurn();
        }

        public void SetPlayingTurnQueue()
        {
            numOfPlayers = players != null ? players.Length : 0;

            if (numOfPlayers <= 0)
            {
                Debug.LogWarning("No players found to set up the turn queue.");
                return;
            }

            playingTurnQueue = new QueueProperties();
            playingTurnQueue.Clear();

            for (int i = 0; i < numOfPlayers; i++)
            {
                NodeBehavior player = players[i].gameObject.GetComponent<NodeBehavior>();
                if (player == null)
                {
                    Debug.LogWarning($"Player at index {i} does not have a NodeBehavior component.");
                    continue;
                }
                player.nodeName = players[i].playerName;
                playingTurnQueue.Insert(player);
            }
            Debug.Log("Playing turn queue initialized.");
        }

        public void TryDrawing(NodeBehavior player)
        {
            var stacks = Card_Stacks.Instance;

            if (stacks == null)
            {
                Debug.LogWarning("Card_Stacks.Instance is null in TryDrawing.");
                return;
            }

            if (stacks.GetDrawPile().IsStackEmpty())
            {
                Debug.Log("Draw Pile is empty, cannot draw a card.");
                return;
            }

            var topCard = stacks.GetDrawPile().Peek();

            if (topCard != null)
            {
                var player_turn = playingTurnQueue.Peek();

                if (player_turn != null)
                {
                    if(player_turn.GetComponent<CardPlayer>().isBot == true)
                    {
                        stacks.DrawCard(stacks.GetDrawPile(), stacks.GetCpuHand(), false);
                    }
                    else
                    {
                        stacks.DrawCard(stacks.GetDrawPile(), stacks.GetPlayerHand(), true);
                    }
                }
                Debug.Log($"{player.nodeName} drew a card: {topCard.nodeName}");
            }
            else
            {
                Debug.Log("No card to draw.");
            }

            SwitchTurn();
        }

        public void SwitchTurn()
        {
            if (playingTurnQueue == null || playingTurnQueue.IsQueueEmpty())
            {
                Debug.LogWarning("Turn queue is empty or not initialized.");
                return;
            }

            // If less than two players nothing to rotate
            if (playingTurnQueue.queueSize < 2)
            {
                Debug.Log("Not enough players to rotate turns.");
                return;
            }

            // Rotate one step in O(1)
            var prev = playingTurnQueue.Peek();
            playingTurnQueue.RotateOne();

            var next = playingTurnQueue.Peek();
            if (prev != null && next != null)
            {
                Debug.Log($"Turn switched from {prev.nodeName} to {next.nodeName}");
            }
        }

        public NodeBehavior GetCurrentTurn()
        {
            if(!playingTurnQueue.IsQueueEmpty())
            {
                return playingTurnQueue.Peek();
            }

            return null;
        }

        public void GenerateMove(NodeBehavior cpuPlayer)
        {
            // Simple CPU logic: draw a card if hand has less than 5 cards, else discard the first card
            var stacks = Card_Stacks.Instance;
            if (stacks == null)
            {
                Debug.LogWarning("Card_Stacks.Instance is null in GenerateMove.");
                return;
            }
            var playerHand = stacks.GetCpuHand();
            // Count cards in hand
            int handCount = 0;
            NodeBehavior current = playerHand.head;
            while (current != null)
            {
                handCount++;
                current = current.nextNode;
            }
            if (handCount < 3)
            {
                TryDrawing(cpuPlayer);
                Debug.Log($"{cpuPlayer.nodeName} decided to draw a card.");
            }
            else
            {
                var chance = UnityEngine.Random.Range(1, 10);

                if(chance <= 3)
                {
                    // 30% chance to draw instead of discard
                    TryDrawing(cpuPlayer);
                    Debug.Log($"{cpuPlayer.nodeName} decided to draw a card.");
                    return;
                }
                // Discard the first card in hand
                if (playerHand.head != null)
                {
                    TryDiscarding(cpuPlayer, playerHand.head);
                    Debug.Log($"{cpuPlayer.nodeName} decided to discard a card.");
                }
            }
        }

        // Coroutine to delay CPU move by cpuTurnDelay seconds.
        // Cancels itself if the turn changes before timeout.
        private IEnumerator DelayedCpuMove(NodeBehavior cpuPlayer)
        {
            if (cpuPlayer == null)
            {
                cpuMoveCoroutine = null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < cpuMoveDelay)
            {
                // If the turn changed, cancel scheduled CPU action
                if (GetCurrentTurn() != cpuPlayer)
                {
                    cpuMoveCoroutine = null;
                    yield break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Still the same player's turn? execute move.
            if (GetCurrentTurn() == cpuPlayer)
            {
                GenerateMove(cpuPlayer);
            }

            cpuMoveCoroutine = null;
        }
    }
}