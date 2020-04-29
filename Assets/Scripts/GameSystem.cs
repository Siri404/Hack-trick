using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = System.Random;

public enum GameState { Start, Playerturn, Enemyturn, Won, Lost}
public class GameSystem : MonoBehaviour
{
    [FormerlySerializedAs("slots")] public List<Transform> slotTransforms;
    public List<Slot> Slots { get; } = new List<Slot>(9);

    //used for easy win check
    private readonly List<int> _slotConverter = new List<int>(9);
    public GameState state;
    public List<GameObject> tokens;

    public Player player1;
    public Player player2;
    public TMP_Text playerTokens;
    public TMP_Text playerTokensCaptured;
    public TMP_Text enemyTokens;
    public TMP_Text enemyTokensCaptured;
    public DeckHandler deckHandler;
    public Image lastCardImage;
    public ChatManager chatManager;
    public GameOver gameOver;
    public GameObject EnemyCardHolder;
    public GameObject PlayerCardHolder;
    public GameObject PlayedCardsPanel;
    
    private bool alreadyAsked;
    private readonly Random _random = new Random();

    public PlayerAgent playerAgent2;
    

    
    public void ResetGame()
    {
        //destroy cards for player and enemy
        Transform[] children = PlayerCardHolder.GetComponentsInChildren<Transform>();
        for (int i = 1; i < children.Length; i++)
        {
            Destroy(children[i].gameObject);
        }
        
        children = EnemyCardHolder.GetComponentsInChildren<Transform>();
        for (int i = 1; i < children.Length; i++)
        {
            Destroy(children[i].gameObject);
        }

        children = PlayedCardsPanel.GetComponentsInChildren<Transform>();
        for (int i = 1; i < children.Length; i++)
        {
            Destroy(children[i].gameObject);
        }

        //reset all slots
        for (int i = 0; i < 9; i++)
        {
            Slots[i].ResetSlot();
        }
        
        chatManager.SendToActionLog("Game reset!");
        state = GameState.Start;
        StartCoroutine(SetupGame());
    }
    private void Start()
    {
        player1 = new Player(playerTokens, playerTokensCaptured, "white", 1 );
        player2 = new Player(enemyTokens, enemyTokensCaptured, "red", 0 );
        playerAgent2.Player = player2;
        playerAgent2.Opponent = player1;
        deckHandler.player1 = player1;
        deckHandler.player2 = player2;
        //initialize the 9 slots of the board and the slot converter
        for (int i = 0; i < 9; i++)
        {
            Slots.Add(new Slot());
        }
        _slotConverter.Add(7);
        _slotConverter.Add(0);
        _slotConverter.Add(5);
        _slotConverter.Add(2);
        _slotConverter.Add(4);
        _slotConverter.Add(6);
        _slotConverter.Add(3);
        _slotConverter.Add(8);
        _slotConverter.Add(1);
        
        state = GameState.Start;
        StartCoroutine(SetupGame());
    }

    public IEnumerator SetupGame()
    {
        //set the text info
        playerTokens.text = "10";
        playerTokensCaptured.text = "0";
        enemyTokens.text = "10";
        enemyTokensCaptured.text = "0";
        player1.ForcedToPlay = false;
        player2.ForcedToPlay = false;
        player1.Blocking = false;
        player2.Blocking = false;
        alreadyAsked = false;
        
        yield return new WaitForSeconds(2f);
        
        //coin flip to decide starting player
        if (_random.Next(0, 2) == 1)
        {
            state = GameState.Playerturn;
            deckHandler.ResetDeck();
            StartCoroutine(PlayerTurn());
        }
        else
        {
            state = GameState.Enemyturn;
            deckHandler.ResetDeck();
            StartCoroutine(EnemyTurn());
        }
    }

    IEnumerator PlayerTurn()
    {
        alreadyAsked = false;
        while (state == GameState.Playerturn)
        {
            chatManager.SendToActionLog("Waiting for player");

            yield return new WaitUntil(() => state != GameState.Playerturn);
        }

        player1.ForcedToPlay = false;
        player2.Blocking = false;
        
        //game over?
        //if state is start -> game was reset, must not start EnemyTurn again
        if (state == GameState.Enemyturn)
        {
            StartCoroutine(EnemyTurn());
        }
    }
    
    IEnumerator EnemyTurn()
    {
        //reset forcedToPlay after player turn is over
        player1.ForcedToPlay = false;
        player2.Blocking = false;
        
        //game over?
        if (state != GameState.Enemyturn) yield break;
        
        chatManager.SendToActionLog("Enemy turn");
        yield return new WaitForSeconds(3f);
        
        //randomAction / request decision
        playerAgent2.RequestDecision();
        //EnemyRandomAction();
        
        yield return new WaitUntil(() => state != GameState.Enemyturn);
        
        //game over?
        if (state == GameState.Playerturn)
        {
            //reset forcedToPlay here to avoid bug by player spamming the button right before his turn
            player2.ForcedToPlay = false;
            player1.Blocking = false;
        
            StartCoroutine(PlayerTurn());
        }
    }

    public void EnemyRandomAction()
    {
        
        if (_random.Next(0, 7) == 0)
        {
            BlockForEnemy();
        }

        if (_random.Next(0, 6) == 0)
        {
            ForcePlayerToPlay();
        }

        if (player2.cardsInHand.Count == 1 && player2.cardsInHand[0] == deckHandler.lastPlayed || 
            player2.cardsInHand.Count == 2 && player2.cardsInHand[0] == deckHandler.lastPlayed 
                                           && player2.cardsInHand[1] == deckHandler.lastPlayed)
        {
            if (player2.ForcedToPlay)
            {
                chatManager.SendToActionLog("Enemy draws a card because he can't play the card(s) in hand");
            }
            else
            {
                chatManager.SendToActionLog("Enemy draws a card");
            }

            deckHandler.DrawForPlayer2();
            
        }
        
        //coin flip for draw / play card
        else if (player2.cardsInHand.Count < 4 && _random.Next(0,2) == 1 && player2.ForcedToPlay == false 
                 || player2.cardsInHand.Count == 0)
        {
            chatManager.SendToActionLog("Enemy draws a card");
            deckHandler.DrawForPlayer2();
        }
        else
        {
            chatManager.SendToActionLog("Enemy plays a card");
            //play random card from hand
            int card = deckHandler.lastPlayed;
            while (card == deckHandler.lastPlayed)
            {
                card = player2.cardsInHand[_random.Next(0, player2.cardsInHand.Count)];
            }

            deckHandler.RemoveFromPlayer2(card);
            Transform[] transforms = EnemyCardHolder.GetComponentsInChildren<Transform>();
            if (transforms.Length > 1)
            {
                Destroy(transforms[1].gameObject);
            }
            else
            {
                Console.Write("Failed destroy!");
            }
            // get the position on board for token placement
            int pos = deckHandler.lastPlayed + card - 1;
            
            // set last played card
            deckHandler.lastPlayed = card;
            deckHandler.playedCards.Add(card);
            deckHandler.InstantiatePlayedCard(card);
            lastCardImage.sprite = deckHandler.cards[card].GetComponent<Image>().sprite;
            
            PlaceToken(pos, player2.Color, 0);
            if (state == GameState.Enemyturn)
            {
                state = GameState.Playerturn;
            }
        }
        
    }

    //place token of given color on board
    public void PlaceToken(int pos, string color, int token)
    {
        if (pos < 0 || pos > 8 || token < 0 || token > 3)
        {
            Debug.Log("invalid pos");
            return;
        }
        
        //slot is friendly or unoccupied
        if (Slots[pos].Color == color || Slots[pos].Color == "none")
        {
            //place new token
            Slots[pos].Tokens.Add( Instantiate(tokens[token], slotTransforms[pos * 3 + Slots[pos].Count]));
            Slots[pos].Count += 1;
            Slots[pos].Color = color;
            
            //update info on screen
            if (color == "white")
            {
                playerTokens.text = (int.Parse(playerTokens.text) - 1).ToString();
            }
            else
            {
                enemyTokens.text = (int.Parse(enemyTokens.text) - 1).ToString();
            }

            //check if the new token is the third on the same slot => game over
            if (Slots[pos].Count == 3)
            {
                if (Slots[pos].Color == "white")
                {
                    chatManager.SendToActionLog("You Won!");
                    state = GameState.Won;
                    gameOver.GameOverDialogue();
                }
                else
                {
                    chatManager.SendToActionLog("You Lost!");
                    state = GameState.Lost;
                    gameOver.GameOverDialogue();
                }

                return;
            }
            
            CheckGameOver(); 
            return;
        }
        
        //slot is occupied by opponent -> disable the opponent's tokens from this slot
        for (int i = 0; i < Slots[pos].Count; i++)
        {
            Destroy(Slots[pos].Tokens[i]);
            //_slots[pos].Tokens[i].SetActive(false);
        }
        Slots[pos].Tokens.Clear();

        //place new token
        int tokensTaken = Slots[pos].Count;
        Slots[pos].Count = 1;
        Slots[pos].Color = color;
        Slots[pos].Tokens.Add(Instantiate(tokens[token], slotTransforms[pos * 3]));
        
        //update info on screen
        if (color == "white")
        {
            playerTokens.text = (int.Parse(playerTokens.text) - 1).ToString();
            playerTokensCaptured.text = (Int32.Parse(playerTokensCaptured.text) + tokensTaken).ToString();
        }
        else
        {
            enemyTokens.text = (int.Parse(enemyTokens.text) - 1).ToString();
            enemyTokensCaptured.text = (int.Parse(enemyTokensCaptured.text) + tokensTaken).ToString();
        }
        CheckGameOver();
    }

    private void CheckGameOver()
    {

        //check for lines & columns
        for (int line = 0; line < 3; line++)
        {
            if (Slots[_slotConverter[line * 3]].Color == "white" && 
                Slots[_slotConverter[line * 3 + 1]].Color == "white" && 
                Slots[_slotConverter[line * 3 + 2]].Color == "white")
            {
                chatManager.SendToActionLog("You Won!");
                state = GameState.Won;
                gameOver.GameOverDialogue();
                return;
            }
            
            if (Slots[_slotConverter[line * 3]].Color == "red" && Slots[_slotConverter[line * 3 + 1]].Color == "red" 
                                                                && Slots[_slotConverter[line * 3 + 2]].Color == "red")
            {
                chatManager.SendToActionLog("You Lost!");
                state = GameState.Lost;
                gameOver.GameOverDialogue();
                return;
            }
            
            if (Slots[_slotConverter[line]].Color == "white" && Slots[_slotConverter[line + 3]].Color == "white" 
                                                              && Slots[_slotConverter[line + 6]].Color == "white")
            {
                chatManager.SendToActionLog("You Won!");
                state = GameState.Won;
                gameOver.GameOverDialogue();
                return;
            }
            
            if (Slots[_slotConverter[line]].Color == "red" && Slots[_slotConverter[line + 3]].Color == "red"
                                                            && Slots[_slotConverter[line + 6]].Color == "red")
            {
                chatManager.SendToActionLog("You Lost!");
                state = GameState.Lost;
                gameOver.GameOverDialogue();
                return;
            }
        }
        
        //check diagonals
        if (Slots[_slotConverter[0]].Color == "white" && Slots[_slotConverter[4]].Color == "white" 
                                                       && Slots[_slotConverter[8]].Color == "white")
        {
            chatManager.SendToActionLog("You Won!");
            state = GameState.Won;
            gameOver.GameOverDialogue();
            return;

        }
        
        if (Slots[_slotConverter[0]].Color == "red" && Slots[_slotConverter[4]].Color == "red" 
                                                     && Slots[_slotConverter[8]].Color == "red")
        {
            chatManager.SendToActionLog("You Lost!");
            state = GameState.Lost;
            gameOver.GameOverDialogue();
            return;
        }

        if (Slots[_slotConverter[2]].Color == "white" && Slots[_slotConverter[4]].Color == "white" 
                                                       && Slots[_slotConverter[6]].Color == "white")
        {
            chatManager.SendToActionLog("You Won!");
            state = GameState.Won;
            gameOver.GameOverDialogue();
            return;
        }
        
        if (Slots[_slotConverter[2]].Color == "red" && Slots[_slotConverter[4]].Color == "red" 
                                                     && Slots[_slotConverter[6]].Color == "red")
        {
            chatManager.SendToActionLog("You Lost!");
            state = GameState.Lost;
            gameOver.GameOverDialogue();
            return;
        }
        if (int.Parse(enemyTokens.text) == 0)
        {
            chatManager.SendToActionLog("You Won!");
            state = GameState.Won;
            gameOver.GameOverDialogue();
            return;
        }
            
        if (int.Parse(playerTokens.text) == 0)
        {
            chatManager.SendToActionLog("You Lost!");
            state = GameState.Lost;
            gameOver.GameOverDialogue();
        }
    }

    public void AskSum()
    {
        if(state != GameState.Playerturn) return;
        if (int.Parse(player1.CapturedTokens.text) > 0)
        {
            chatManager.SendToActionLog("Enemy card total is: " + player2.cardsInHand.Sum());
            
            //use only one token per turn
            if (alreadyAsked) return;
            alreadyAsked = true;
            player1.CapturedTokens.text = (int.Parse(player1.CapturedTokens.text) - 1).ToString();
        }
        else
        {
            chatManager.SendToActionLog("Action requires one enemy token!");
        }
    }

    public void ForceEnemyToPlay()
    {
        if (state != GameState.Playerturn) return;
        if (player2.Blocking)
        {
            chatManager.SendToActionLog("Enemy is blocking!");
            return;
        }
        if (player2.ForcedToPlay)
        {
            player2.ForcedToPlay = false;
            player1.Tokens.text = (int.Parse(player1.Tokens.text) + 1).ToString();
            chatManager.SendToActionLog("Enemy no longer forced to play next turn");
            return;
        }
        if (player2.cardsInHand.Count == 0)
        {
            chatManager.SendToActionLog("Enemy does not have any cards, can't be forced to play next turn!");
            return;
        }

        if (int.Parse(player1.Tokens.text) < 2)
        {
            chatManager.SendToActionLog("You can't sacrifice your last token!");
            return;
        }
        
        player2.ForcedToPlay = true;
        player1.Tokens.text = (int.Parse(player1.Tokens.text) - 1).ToString();
        chatManager.SendToActionLog("Enemy forced to play next turn");
    }

    public void ForcePlayerToPlay()
    {
        if (state != GameState.Enemyturn || player1.Blocking || player1.cardsInHand.Count == 0 || 
            int.Parse(player2.Tokens.text) < 2) return;

        player1.ForcedToPlay = true;
        player2.Tokens.text = (int.Parse(player2.Tokens.text) - 1).ToString();
        chatManager.SendToActionLog("Player forced to play next turn");
    }

    public void BlockForPlayer()
    {
        if(state != GameState.Playerturn) return;
        if (player1.Blocking)
        {
            player1.Blocking = false;
            player1.Tokens.text = (int.Parse(player1.Tokens.text) + 1).ToString();
            chatManager.SendToActionLog("You are no longer blocking");
        }
        else if (int.Parse(playerTokens.text) > 2)
        {
            player1.Blocking = true;
            player1.Tokens.text = (int.Parse(player1.Tokens.text) - 1).ToString();
            chatManager.SendToActionLog("You are now blocking");
        }
        else
        {
            chatManager.SendToActionLog("You can't sacrifice your last token!");
        }
    }

    public void BlockForEnemy()
    {
        if (state != GameState.Enemyturn  || int.Parse(player2.Tokens.text) < 2) return;
        
        player2.Blocking = true;
        player2.Tokens.text = (int.Parse(player2.Tokens.text) - 1).ToString();
        chatManager.SendToActionLog("Enemy blocking next turn!");
    }

    public void DestroyCardFromPlayerHolder(int card)
    {
        Transform[] transforms = PlayerCardHolder.GetComponentsInChildren<Transform>();
        int i = 0;
        while (i < transforms.Length)
        {
            if (transforms[i].GetComponentInChildren<Image>().name.Contains("Card_" + card))
            {
                Destroy(transforms[i].parent.gameObject);
                return;
            }

            i++;
        }
    }

    public void DestroyCardFromEnemyHolder()
    {
        Transform[] transforms = EnemyCardHolder.GetComponentsInChildren<Transform>();
        if (transforms.Length > 1)
        {
            Destroy(transforms[1].gameObject);
        }
        else
        {
            Console.Write("Failed destroy!");
        }
    }
}



//A class that holds the information about the 9 slots on the board
public class Slot
{
    public string Color { get; set; }
    public List<GameObject> Tokens { get; set; }
    public int Count { get; set; }

    public Slot()
    {
        Color = "none";
        Count = 0;
        Tokens = new List<GameObject>(3);
    }

    public void ResetSlot()
    {
        Color = "none";
        Count = 0;
        foreach (GameObject token in Tokens)
        {
            token.SetActive(false);
            Object.Destroy(token);
        }
        Tokens = new List<GameObject>(3);
    }
    
}

public class Player
{
    public bool ForcedToPlay;
    public bool Blocking;
    public List<int> cardsInHand = new List<int>(4);
    public string Color;
    public int TokenType;
    public TMP_Text Tokens;
    public TMP_Text CapturedTokens;

    public Player(TMP_Text tokens, TMP_Text capturedTokens, string color, int tokenType)
    {
        Tokens = tokens;
        CapturedTokens = capturedTokens;
        Color = color;
        TokenType = tokenType;
    }
}