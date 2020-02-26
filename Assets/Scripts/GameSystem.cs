using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = System.Random;

public enum GameState { Start, Playerturn, Enemyturn, Won, Lost}
public class GameSystem : MonoBehaviour
{
    [FormerlySerializedAs("slots")] public List<Transform> slotTransforms;
    private readonly List<Slot> _slots = new List<Slot>(9);
    
    //used for easy win check
    private readonly List<int> _slotConverter = new List<int>(9);
    public GameState state;
    public List<GameObject> tokens;

    public TMP_Text playerTokens;
    public TMP_Text playerTokensCaptured;
    public TMP_Text enemyTokens;
    public TMP_Text enemyTokensCaptured;
    public DeckHandler deckHandler;
    public Image lastCardImage;
    public ChatManager chatManager;
    public GameOver gameOver;
    public GameObject EnemyCardHolder;
    
    private bool alreadyAsked;
    public bool playerForcedToPlay;
    public bool enemyForcedToPlay;
    private bool playerBlocking;
    private bool enemyBlocking;
    private readonly Random _random = new Random();

    private void Start()
    {
        //initialize the 9 slots of the board and the slot converter
        for (int i = 0; i < 9; i++)
        {
            _slots.Add(new Slot());
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
        playerForcedToPlay = false;
        enemyForcedToPlay = false;
        playerBlocking = false;
        enemyBlocking = false;
        alreadyAsked = false;
        
        yield return new WaitForSeconds(2f);
        
        //coin flip to decide starting player
        if (_random.Next(0, 2) == 1)
        {
            state = GameState.Playerturn;
            deckHandler.GameSetup();
            StartCoroutine(PlayerTurn());
        }
        else
        {
            state = GameState.Enemyturn;
            deckHandler.GameSetup();
            StartCoroutine(EnemyTurn());
        }
    }

    IEnumerator PlayerTurn()
    {
        alreadyAsked = false;
        while (state == GameState.Playerturn)
        {
            chatManager.SendToActionLog("Waiting for player");
            yield return new WaitUntil(() => state == GameState.Enemyturn);
        }
        StartCoroutine(EnemyTurn());
    }

    IEnumerator EnemyTurn()
    {
        //reset forcedToPlay after player turn is over
        playerForcedToPlay = false;
        enemyBlocking = false;
        
        //game over?
        if (state != GameState.Enemyturn) yield break;
        
        chatManager.SendToActionLog("Enemy turn");
        yield return new WaitForSeconds(3f);

        if (_random.Next(0, 7) == 0)
        {
            BlockForEnemy();
        }

        if (_random.Next(0, 6) == 0)
        {
            ForcePlayerToPlay();
        }

        if (deckHandler.enemyHand.Count == 1 && deckHandler.enemyHand[0] == deckHandler.lastPlayed || 
            deckHandler.enemyHand.Count == 2 && deckHandler.enemyHand[0] == deckHandler.lastPlayed 
                                             && deckHandler.enemyHand[1] == deckHandler.lastPlayed)
        {
            if (enemyForcedToPlay)
            {
                chatManager.SendToActionLog("Enemy draws a card because he can't play the card(s) in hand");
            }
            else
            {
                chatManager.SendToActionLog("Enemy draws a card");
            }
            deckHandler.DrawForEnemy();
            
        }
        
        //coin flip for draw / play card
        else if (deckHandler.enemyHand.Count < 4 && _random.Next(0,2) == 1 && enemyForcedToPlay == false || deckHandler.enemyHand.Count == 0)
        {
            chatManager.SendToActionLog("Enemy draws a card");
            deckHandler.DrawForEnemy();
        }
        else
        {
            chatManager.SendToActionLog("Enemy plays a card");
            //play random card from hand
            int card = deckHandler.lastPlayed;
            while (card == deckHandler.lastPlayed)
            {
                card = deckHandler.enemyHand[_random.Next(0, deckHandler.enemyHand.Count)];
            }
            deckHandler.RemoveFromEnemy(card);
            Transform[] transforms = EnemyCardHolder.GetComponentsInChildren<Transform>();
            Destroy(transforms[1].gameObject);
            
            //get the position on board for token placement
            int pos = deckHandler.lastPlayed + card - 1;
            
            //set last played card
            deckHandler.lastPlayed = card;
            lastCardImage.sprite = deckHandler.cards[card].GetComponent<Image>().sprite;
            
            PlaceToken(pos, "red", 0);
        }
        
        //game over?
        if (state != GameState.Enemyturn) yield break;
        
        yield return new WaitForSeconds(2f);
        
        //reset forcedToPlay here to avoid bug by player spamming the button right before his turn
        enemyForcedToPlay = false;
        playerBlocking = false;
        state = GameState.Playerturn;
        StartCoroutine(PlayerTurn());

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
        if (_slots[pos].Color == color || _slots[pos].Color == "none")
        {
            //place new token
            _slots[pos].Tokens.Add( Instantiate(tokens[token], slotTransforms[pos * 3 + _slots[pos].Count]));
            _slots[pos].Count += 1;
            _slots[pos].Color = color;
            
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
            if (_slots[pos].Count == 3)
            {
                if (_slots[pos].Color == "white")
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
            }
            
            CheckGameOver(); 
            return;
        }
        
        //slot is occupied by opponent -> disable the opponent's tokens from this slot
        for (int i = 0; i < _slots[pos].Count; i++)
        {
            Destroy(_slots[pos].Tokens[i]);
            //_slots[pos].Tokens[i].SetActive(false);
        }
        _slots[pos].Tokens.Clear();

        //place new token
        int tokensTaken = _slots[pos].Count;
        _slots[pos].Count = 1;
        _slots[pos].Color = color;
        _slots[pos].Tokens.Add(Instantiate(tokens[token], slotTransforms[pos * 3]));
        
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
            if (_slots[_slotConverter[line * 3]].Color == "white" && 
                _slots[_slotConverter[line * 3 + 1]].Color == "white" && 
                _slots[_slotConverter[line * 3 + 2]].Color == "white")
            {
                chatManager.SendToActionLog("You Won!");
                state = GameState.Won;
                gameOver.GameOverDialogue();
                return;
            }
            
            if (_slots[_slotConverter[line * 3]].Color == "red" && _slots[_slotConverter[line * 3 + 1]].Color == "red" 
                                                                && _slots[_slotConverter[line * 3 + 2]].Color == "red")
            {
                chatManager.SendToActionLog("You Lost!");
                state = GameState.Lost;
                gameOver.GameOverDialogue();
                return;
            }
            
            if (_slots[_slotConverter[line]].Color == "white" && _slots[_slotConverter[line + 3]].Color == "white" 
                                                              && _slots[_slotConverter[line + 6]].Color == "white")
            {
                chatManager.SendToActionLog("You Won!");
                state = GameState.Won;
                gameOver.GameOverDialogue();
                return;
            }
            
            if (_slots[_slotConverter[line]].Color == "red" && _slots[_slotConverter[line + 3]].Color == "red"
                                                            && _slots[_slotConverter[line + 6]].Color == "red")
            {
                chatManager.SendToActionLog("You Lost!");
                state = GameState.Lost;
                gameOver.GameOverDialogue();
                return;
            }
        }
        
        //check diagonals
        if (_slots[_slotConverter[0]].Color == "white" && _slots[_slotConverter[4]].Color == "white" 
                                                       && _slots[_slotConverter[8]].Color == "white")
        {
            chatManager.SendToActionLog("You Won!");
            state = GameState.Won;
            gameOver.GameOverDialogue();
            return;

        }
        
        if (_slots[_slotConverter[0]].Color == "red" && _slots[_slotConverter[4]].Color == "red" 
                                                     && _slots[_slotConverter[8]].Color == "red")
        {
            chatManager.SendToActionLog("You Lost!");
            state = GameState.Lost;
            gameOver.GameOverDialogue();
            return;
        }

        if (_slots[_slotConverter[2]].Color == "white" && _slots[_slotConverter[4]].Color == "white" 
                                                       && _slots[_slotConverter[6]].Color == "white")
        {
            chatManager.SendToActionLog("You Won!");
            state = GameState.Won;
            gameOver.GameOverDialogue();

        }
        
        if (_slots[_slotConverter[2]].Color == "red" && _slots[_slotConverter[4]].Color == "red" 
                                                     && _slots[_slotConverter[6]].Color == "red")
        {
            chatManager.SendToActionLog("You Lost!");
            state = GameState.Lost;
            gameOver.GameOverDialogue();
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
        if (int.Parse(playerTokensCaptured.text) > 0)
        {
            chatManager.SendToActionLog("Enemy card total is: " + deckHandler.enemyHand.Sum());
            
            //use only one token per turn
            if (alreadyAsked) return;
            alreadyAsked = true;
            playerTokensCaptured.text = (int.Parse(playerTokensCaptured.text) - 1).ToString();
        }
        else
        {
            chatManager.SendToActionLog("Action requires one enemy token!");
        }
    }

    public void ForceEnemyToPlay()
    {
        if (state != GameState.Playerturn) return;
        if (enemyBlocking)
        {
            chatManager.SendToActionLog("Enemy is blocking!");
            return;
        }
        if (enemyForcedToPlay)
        {
            enemyForcedToPlay = false;
            playerTokens.text = (int.Parse(playerTokens.text) + 1).ToString();
            chatManager.SendToActionLog("Enemy no longer forced to play next turn");
            return;
        }
        if (deckHandler.enemyHand.Count == 0)
        {
            chatManager.SendToActionLog("Enemy does not have any cards, can't be forced to play next turn!");
            return;
        }

        if (int.Parse(playerTokens.text) < 2) return;
        
        enemyForcedToPlay = true;
        playerTokens.text = (int.Parse(playerTokens.text) - 1).ToString();
        chatManager.SendToActionLog("Enemy forced to play next turn");
    }

    public void ForcePlayerToPlay()
    {
        if (state != GameState.Enemyturn || playerBlocking || deckHandler.playerHand.Count == 0 || 
            int.Parse(enemyTokens.text) < 2) return;

        playerForcedToPlay = true;
        enemyTokens.text = (int.Parse(enemyTokens.text) - 1).ToString();
        chatManager.SendToActionLog("Player forced to play next turn");
    }

    public void BlockForPlayer()
    {
        if(state != GameState.Playerturn) return;
        if (playerBlocking)
        {
            playerBlocking = false;
            playerTokens.text = (int.Parse(playerTokens.text) + 1).ToString();
            chatManager.SendToActionLog("You are no longer blocking");
        }
        else if (int.Parse(playerTokens.text) > 2)
        {
            playerBlocking = true;
            playerTokens.text = (int.Parse(playerTokens.text) - 1).ToString();
            chatManager.SendToActionLog("You are now blocking");
        }
        else
        {
            chatManager.SendToActionLog("You can't sacrifice your last token!");
        }
    }

    public void BlockForEnemy()
    {
        if (state != GameState.Enemyturn  || int.Parse(enemyTokens.text) < 2) return;
        
        enemyBlocking = true;
        enemyTokens.text = (int.Parse(enemyTokens.text) - 1).ToString();
        chatManager.SendToActionLog("Enemy blocking next turn!");
    }
    
}



//A class that holds the information about the 9 slots on the board
class Slot
{
    public string Color { get; set; }
    public List<GameObject> Tokens { get; }
    public int Count { get; set; }

    public Slot()
    {
        Color = "none";
        Count = 0;
        Tokens = new List<GameObject>(3);
    }
}