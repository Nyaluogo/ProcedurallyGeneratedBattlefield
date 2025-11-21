using System;
using System.Collections.Generic;
using UnityEngine;
using static GraphTheory.CardRules;
using static GraphTheory.GraphMaster;

namespace GraphTheory
{
    public class Card_Stacks : MonoBehaviour
    {
        public static Card_Stacks Instance { get; private set; }
        Card_Stacks() { Instance = this; }

        private float moveTimer = 0f;

        public CardFaceProperties[] cardFaces;
        public GameObject cardPrefab;
        public Material defaultBackfaceMat;
        public Transform disposePosition;
        public Transform drawPosition;
        public Transform playerPosition;
        public Transform cpuPosition;

        [SerializeField] StackProperties Deck;
        [SerializeField] LinkedListProperties playerCards;
        [SerializeField] LinkedListProperties cpuCards;
        [SerializeField] StackProperties DiscardPile;
        [SerializeField] StackProperties DrawPile;

        private Dictionary<int, Vector3> layoutPositionCache = new Dictionary<int, Vector3>();

        private Dictionary<NodeBehavior, CardMoveData> activeCardMoves = new Dictionary<NodeBehavior, CardMoveData>();
        private const float CARD_MOVE_DURATION = 0.3f; // Duration of card movement animation in seconds

        private struct CardMoveData
        {
            public Vector3 startPos;
            public Vector3 targetPos;
            public Quaternion startRot;
            public Quaternion targetRot;
            public Transform targetParent;
            public float moveStartTime;
            public bool resetLocalPos;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            SetInitialReferences();
        }

        // Update is called once per frame
        void Update()
        {
            SetUpdateReferences();
        }

        void SetInitialReferences()
        {
            LoadDeck();
            if (!Deck.IsStackEmpty())
            {
                InitialiseDrawPile();
            }

            if (!DrawPile.IsStackEmpty())
            {
                ShuffleDeck(DrawPile);
            }

            DealInitialHands(4);

            PlaceStartingCard();

            SetDrawstackDiscression();
            SetDiscardstackDiscression();
            SetPlayerHandDiscression();
        }

        private void PlaceStartingCard()
        {
            if (DrawPile == null || DrawPile.top <= 0)
            {
                Debug.LogWarning("Draw pile is empty or not properly initialized.");
                return;
            }
            NodeBehavior startingCard = DrawPile.Pop();
            if (startingCard != null)
            {
                DiscardPile.Push(startingCard);

                var card_face = startingCard.dataObj.GetComponent<CardFace>();
                if (card_face != null)
                {
                    card_face.SetFaceVisibility(true);
                    card_face.SetInteractivity(false);
                    card_face.PrintFace();
                }

                // Place the first discard card at the disposePosition and center it
                MoveCardTo(startingCard, disposePosition, true);
                SetDiscardstackDiscression();
            }
        }

        void SetUpdateReferences()
        {
            moveTimer += Time.deltaTime;

            if (moveTimer >= 1f)
            {
                CheckInput();
                moveTimer = 0f;
            }
        }

        public void LoadDeck()
        {
            if (cardPrefab == null)
            {
                Debug.LogError("Card prefab is not assigned.");
                return;
            }
            if (cardFaces.Length > 0)
            {
                Deck = new StackProperties()
                {
                    stack = new NodeBehavior[cardFaces.Length]
                };
                DiscardPile = new StackProperties()
                {
                    stack = new NodeBehavior[cardFaces.Length]
                };
                DrawPile = new StackProperties()
                {
                    stack = new NodeBehavior[cardFaces.Length]
                };

                foreach (var cardFace in cardFaces)
                {
                    GameObject new_card = Instantiate(cardPrefab, drawPosition.position, Quaternion.identity);
                    if (new_card == null)
                    {
                        Debug.LogError("Failed to instantiate card prefab.");
                        continue;
                    }
                    new_card.transform.SetParent(drawPosition);
                    new_card.transform.position = Vector3.zero;
                    new_card.transform.localPosition = Vector3.zero;

                    if (new_card.GetComponent<CardFace>() == null)
                    {
                        Debug.LogError("Card prefab does not have a CardFace component.");
                        continue;
                    }

                    var card_face = new_card.GetComponent<CardFace>();
                    card_face.SetInteractivity(false);
                    card_face.SetFaceMaterial(cardFace.cardImage);
                    card_face.PrintBack();

                    NodeBehavior cardVertex = new_card.GetComponent<NodeBehavior>();
                    if (cardVertex == null)
                    {
                        Debug.LogError("Card prefab does not have a NodeBehavior component.");
                        continue;
                    }

                    cardVertex.nodeName = cardFace.cardName;
                    cardVertex.dataObj = new_card;
                    cardVertex.position = drawPosition.position;
                    Deck.Push(cardVertex);
                    cardVertex.DisplayInfo();
                    Debug.Log(cardVertex.name + " loaded to Deck");
                }
            }
        }
        /// <summary>
        /// toa kwa box . eka kwa meza
        /// </summary>
        public void InitialiseDrawPile()
        {
            if (Deck.stack.Length > 0)
            {
                while (!Deck.IsStackEmpty())
                {
                    NodeBehavior card = Deck.Pop();
                    DrawPile.Push(card);
                }
                var topCard = DrawPile.Peek();
                if (topCard != null)
                {
                    var cardFace = topCard.GetComponent<CardFace>();
                    if (cardFace != null)
                    {
                        cardFace.SetInteractivity(true);
                    }
                }

                // parent all draw pile cards to drawPosition and stack them visually
                if (drawPosition != null)
                {
                    for (int i = 0; i < DrawPile.stack.Length; i++)
                    {
                        var c = DrawPile.stack[i];
                        if (c != null && c.dataObj != null)
                        {
                            c.dataObj.transform.SetParent(drawPosition, false);
                            // small offset so the pile is visible
                            c.dataObj.transform.localPosition = new Vector3(0f, 0f, -i * 0.002f);
                        }
                    }
                }
            }
        }

        private void CheckInput()
        {

        }

        public void DrawCard(StackProperties drawPile, LinkedListProperties playerHand, bool isPlayer = true)
        {
            if (drawPile == null || drawPile.top <= 0)
            {
                Debug.LogWarning("Draw pile is empty or not properly initialized.");
                return;
            }
            NodeBehavior drawnCard = drawPile.Pop();
            if (drawnCard != null)
            {
                drawnCard.previousNode = null;
                drawnCard.nextNode = null;

                // parent the drawn card to the appropriate hand container but do not reset local position
                if (isPlayer)
                {
                    MoveCardTo(drawnCard, playerPosition, false);
                }
                else
                {
                    MoveCardTo(drawnCard, cpuPosition, false);
                }

                if (drawnCard.dataObj != null)
                {
                    var card_face = drawnCard.dataObj.GetComponent<CardFace>();
                    if (card_face != null)
                    {
                        card_face.SetFaceVisibility(true);
                        card_face.SetInteractivity(true);
                        card_face.PrintFace();
                    }
                }

                // add vertex to linked list for the hand (append at end)
                if (playerHand.head == null)
                {
                    playerHand.head = drawnCard;
                    playerHand.tail = drawnCard;
                }
                else
                {
                    // append to end
                    if (playerHand.tail != null)
                    {
                        playerHand.tail.nextNode = drawnCard;
                        drawnCard.previousNode = playerHand.tail;
                        playerHand.tail = drawnCard;
                    }
                    else
                    {
                        // fallback, attach after head
                        playerHand.head.nextNode = drawnCard;
                        drawnCard.previousNode = playerHand.head;
                        playerHand.tail = drawnCard;
                        Debug.LogWarning("Player hand tail is null while head is not. fallback executed");
                    }
                }

                playerHand.dataBodyPrefab = drawnCard.dataObj;

                // Re-layout hands and stacks so the drawn card appears at the end and cards are spread out
                if (isPlayer)
                {
                    LayoutHand(playerHand, playerPosition, 3.5f, true);
                }
                else
                {
                    LayoutHand(playerHand, cpuPosition, 1.2f, false);
                }

                SetDrawstackDiscression();
                SetPlayerHandDiscression();
                drawnCard.DisplayInfo();
            }
        }

        public void DiscardCard(NodeBehavior card, bool isPlayer = true)
        {
            // Early validation
            if (card == null)
            {
                Debug.LogWarning("Attempted to discard null card.");
                return;
            }

            var playerHand = isPlayer ? playerCards : cpuCards;
            if (playerHand == null)
            {
                Debug.LogWarning("Player hand or discard pile is not properly initialized.");
                return;
            }


            // Find the exact node in the hand (single traversal, avoids calling external RemoveVertex which may corrupt links)
            NodeBehavior current = playerHand.head;
            NodeBehavior target = null;
            while (current != null)
            {
                if (current == card)
                {
                    target = current;
                    break;
                }
                current = current.nextNode;
            }

            if (target == null)
            {
                Debug.LogWarning($"Card '{card.nodeName}' not found in player's hand.");
                return;
            }

            // Detach target from linked list (safe, O(1) pointer updates)
            if (target.previousNode != null)
            {
                target.previousNode.nextNode = target.nextNode;
            }
            else
            {
                // removing head
                playerHand.head = target.nextNode;
            }

            if (target.nextNode != null)
            {
                target.nextNode.previousNode = target.previousNode;
            }
            else
            {
                // removing tail
                playerHand.tail = target.previousNode;
            }

            // Fully isolate the node
            target.previousNode = null;
            target.nextNode = null;

            // Move the GameObject under dispose position to avoid transform parent issues (do it after detaching)
            MoveCardTo(target, disposePosition, true);

            // Push to discard pile
            DiscardPile.Push(target);

            // Single layout pass for the affected hand
            if (isPlayer)
            {
                LayoutHand(playerCards, playerPosition, 3.5f, true);
            }
            else
            {
                LayoutHand(cpuCards, cpuPosition, 1.2f, false);
            }

            // Update discard visuals
            SetDiscardstackDiscression();
        }

        void DealInitialHands(int handSize)
        {
            playerCards = new LinkedListProperties();
            cpuCards = new LinkedListProperties();

            for (int i = 0; i < handSize; i++)
            {
                DrawCard(DrawPile, playerCards);
                DrawCard(DrawPile, cpuCards, false);
            }
            SetPlayerHandDiscression();
            // initial layout
            LayoutHand(playerCards, playerPosition, 3f, true);
            LayoutHand(cpuCards, cpuPosition, 1.2f, false);
        }

        /// <summary>
        /// Move a card's GameObject under a parent transform.
        /// If resetLocalPos is true, localPosition is set to Vector3.zero (centered on parent).
        /// If false, keep the world position while parenting (so caller can later layout).
        /// </summary>
        public void MoveCardTo(NodeBehavior cardToMove, Transform parent, bool resetLocalPos = true)
        {
            if (cardToMove == null)
            {
                Debug.LogWarning("Card to move is null.");
                return;
            }

            if (cardToMove.dataObj == null)
            {
                Debug.LogWarning("Card dataObj is null, cannot move.");
                return;
            }

            if (parent == null)
            {
                Debug.LogWarning("Parent transform is null.");
                return;
            }
            
            
            /*
            // Calculate target position and rotation
            Vector3 targetPos = resetLocalPos ? parent.position : cardToMove.dataObj.transform.position;
            Quaternion targetRot = resetLocalPos ? parent.rotation : cardToMove.dataObj.transform.rotation;


            // Setup animation data
            var moveData = new CardMoveData
            {
                startPos = cardToMove.dataObj.transform.position,
                targetPos = targetPos,
                startRot = cardToMove.dataObj.transform.rotation,
                targetRot = targetRot,
                targetParent = parent,
                moveStartTime = Time.time,
                resetLocalPos = resetLocalPos
            };

            // Add or update the move animation
            activeCardMoves[cardToMove] = moveData;*/

            // Parent with correct worldPositionStays behavior:
            // - If resetLocalPos is true: set parent without preserving world pos, then zero local pos so card sits at parent's origin.
            // - If resetLocalPos is false: set parent but preserve world position so card doesn't jump; LayoutHand will re-parent & reposition later.
            

            if (resetLocalPos)
            {
                cardToMove.dataObj.transform.SetParent(parent);
                cardToMove.dataObj.transform.localPosition = Vector3.zero;
                cardToMove.dataObj.transform.localRotation = Quaternion.identity;
            }
            else
            {
                cardToMove.dataObj.transform.SetParent(parent); // keep world position
            }

            // update node position to match transform world position
            cardToMove.position = cardToMove.dataObj.transform.position;
        }

        private void UpdateCardAnimations()
        {
            // Process all active card movements
            var completedMoves = new List<NodeBehavior>();

            foreach (var kvp in activeCardMoves)
            {
                var card = kvp.Key;
                var moveData = kvp.Value;

                float elapsedTime = Time.time - moveData.moveStartTime;
                float t = Mathf.Clamp01(elapsedTime / CARD_MOVE_DURATION);

                // Smooth out the animation using easing
                t = Mathf.SmoothStep(0f, 1f, t);

                if (card != null && card.dataObj != null)
                {
                    // Animate position and rotation
                    card.dataObj.transform.position = Vector3.Lerp(moveData.startPos, moveData.targetPos, t);
                    card.dataObj.transform.rotation = Quaternion.Lerp(moveData.startRot, moveData.targetRot, t);

                    // If animation is complete
                    if (t >= 1f)
                    {
                        // Set final parent and position
                        card.dataObj.transform.SetParent(moveData.targetParent);
                        if (moveData.resetLocalPos)
                        {
                            card.dataObj.transform.localPosition = Vector3.zero;
                            card.dataObj.transform.localRotation = Quaternion.identity;
                        }

                        // Update node position to match transform world position
                        card.position = card.dataObj.transform.position;

                        completedMoves.Add(card);
                    }
                }
                else
                {
                    completedMoves.Add(card);
                }
            }

            // Remove completed animations
            foreach (var card in completedMoves)
            {
                activeCardMoves.Remove(card);
            }
        }

        public void SetDrawstackDiscression()
        {

            if (!DrawPile.IsStackEmpty())
            {
                var topCard = DrawPile.Peek();
                // visually stack draw pile at drawPosition
                for (int i = 0; i < DrawPile.stack.Length; i++)
                {
                    var card = DrawPile.stack[i];
                    if (card != null && card.dataObj != null)
                    {
                        var cardFace = card.GetComponent<CardFace>();
                        // parent all draw pile cards to the draw container
                        card.dataObj.transform.SetParent(drawPosition, false);
                        // small stacked z offset so pile shows thickness (older cards behind)
                        card.dataObj.transform.localPosition = new Vector3(0f, 0f, -i * 0.002f);

                        if (cardFace != null)
                        {
                            if (card == topCard)
                            {
                                cardFace.SetInteractivity(true);
                            }
                            else
                            {
                                cardFace.SetInteractivity(false);
                            }
                            cardFace.PrintBack();
                        }
                    }
                }
            }
        }

        public void SetDiscardstackDiscression()
        {
            if (!DiscardPile.IsStackEmpty())
            {
                var topCard = DiscardPile.Peek();
                // put all discard cards under disposePosition so they stack visually, top card shown clearly
                for (int i = 0; i < DiscardPile.stack.Length; i++)
                {
                    var card = DiscardPile.stack[i];
                    if (card != null && card.dataObj != null)
                    {
                        var cardFace = card.GetComponent<CardFace>();
                        // parent to dispose container
                        card.dataObj.transform.SetParent(disposePosition, false);

                        // Adjust positioning for better visual stacking
                        // Move cards slightly up and back as they go deeper in the stack
                        float depthOffset = i * 0.0025f; // Z offset for depth
                        float heightOffset = -i * 0.1f;  // Y offset for slight downward stacking
                        card.dataObj.transform.localPosition = new Vector3(0f, heightOffset, -depthOffset);

                        // Ensure proper rotation
                        card.dataObj.transform.localRotation = Quaternion.identity;

                        if (cardFace != null)
                        {
                            cardFace.SetFaceVisibility(card == topCard); // Always show front face
                            cardFace.SetInteractivity(card == topCard); // Only top card interactive
                            cardFace.PrintFace(); // Explicitly show front face
                        }
                    }
                }
            }
        }

        public void SetPlayerHandDiscression()
        {
            // set interactivity & face for every card in player's hand
            NodeBehavior current = playerCards.head;
            while (current != null)
            {
                if (current.dataObj != null)
                {
                    var cardFace = current.dataObj.GetComponent<CardFace>();
                    if (cardFace != null)
                    {
                        cardFace.SetInteractivity(true);
                        cardFace.PrintFace();
                    }
                }
                current = current.nextNode;
            }
        }

        public bool CheckForMatch()
        {
            return false;
        }

        public void ShuffleDeck(StackProperties stack)
        {
            if (stack == null || stack.top <= 0)
            {
                Debug.LogWarning("Stack is empty or not properly initialized.");
                return;
            }

            // Convert the stack to a list for easier shuffling.
            List<NodeBehavior> cardList = new List<NodeBehavior>();
            while (!stack.IsStackEmpty())
            {
                cardList.Add(stack.Pop());
            }

            // Shuffle the list using Fisher-Yates algorithm.
            int n = cardList.Count;
            System.Random rng = new System.Random();
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                NodeBehavior value = cardList[k];
                cardList[k] = cardList[n];
                cardList[n] = value;
            }

            // Push the shuffled cards back onto the stack.
            foreach (NodeBehavior card in cardList)
            {
                stack.Push(card);
            }
        }

        public StackProperties GetDrawPile()
        {
            return DrawPile;
        }

        public LinkedListProperties GetPlayerHand()
        {
            return playerCards;
        }

        public StackProperties GetDiscardPile()
        {
            return DiscardPile;
        }

        // ----- Helper layout utilities -----

        // Count nodes in a linked list
        int CountNodes(LinkedListProperties list)
        {
            int count = 0;
            if (list == null) return 0;
            NodeBehavior cur = list.head;
            while (cur != null)
            {
                count++;
                cur = cur.nextNode;
            }
            return count;
        }

        // Layout a hand (player or cpu). Centers the spread on the parent transform.
        // spacing: distance between cards on X axis.
        // faceUp: if true, cards are shown face up; otherwise show back.
        void LayoutHand(LinkedListProperties hand, Transform parent, float spacing = 2.5f, bool faceUp = true)
        {
            if (hand == null || parent == null) return;

            int count = CountNodes(hand);
            int cacheKey = count * 1000 + (faceUp ? 1 : 0);

            // Try get cached layout positions
            if (!layoutPositionCache.ContainsKey(cacheKey))
            {
                float totalWidth = (count <= 1) ? 0f : (count - 1) * spacing;
                float startX = -totalWidth / 2f;

                // Cache positions for this configuration
                var positions = new List<Vector3>();
                for (int i = 0; i < count; i++)
                {
                    positions.Add(new Vector3(startX + i * spacing, 0f, -i * 0.001f));
                }
                layoutPositionCache[cacheKey] = positions[0]; // Store first position
            }

            // Apply cached positions
            NodeBehavior current = hand.head;
            int idx = 0;
            Vector3 basePos = layoutPositionCache[cacheKey];

            while (current != null && current.dataObj != null)
            {
                current.dataObj.transform.SetParent(parent, false);
                current.dataObj.transform.localPosition = basePos + new Vector3(idx * spacing, 0f, -idx * 0.001f);
                current.dataObj.transform.localRotation = Quaternion.identity;
                current.position = current.dataObj.transform.position;

                var cf = current.dataObj.GetComponent<CardFace>();
                if (cf != null)
                {
                    cf.SetInteractivity(true);
                    cf.SetFaceVisibility(true);
                    cf.PrintFace();
                }

                current = current.nextNode;
                idx++;
            }
        }

        public LinkedListProperties GetCpuHand()
        {
            return cpuCards;
        }
    }
}