using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

public enum GameState { Start, Playerturn, Enemyturn, Won, Lost}
public class GameSystem : MonoBehaviour
{
    public List<Transform> slots;
    private List<Slot> _slots = new List<Slot>(9); 
    public GameState state;
    public List<GameObject> tokens;

    public TMP_Text playerTokens;
    public TMP_Text playerTokensCaptured;
    public TMP_Text enemyTokens;
    public TMP_Text enemyTokensCaptured;
    public DeckHandler deckHandler;
    public Image lastCardImage;
    public ChatManager chatManager;
    
    private bool alreadyAsked;
    private readonly Random _random = new Random();
    private void Start()
    {
        state = GameState.Start;
        StartCoroutine(SetupGame());
    }

    IEnumerator SetupGame()
    {
        //initialize the 9 slots of the board
        for (int i = 0; i < 9; i++)
        {
            _slots.Add(new Slot());
            //AddToActionLog("da");
        }
        
        //set the text info
        playerTokens.text = "10";
        playerTokensCaptured.text = "0";
        enemyTokens.text = "10";
        enemyTokensCaptured.text = "0";
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
        //game over?
        if (state != GameState.Enemyturn) yield break;
        
        chatManager.SendToActionLog("Enemy turn");
        yield return new WaitForSeconds(3f);

        if (deckHandler.enemyHand.Count == 1 && deckHandler.enemyHand[0] == deckHandler.lastPlayed || 
            deckHandler.enemyHand.Count == 2 && deckHandler.enemyHand[0] == deckHandler.lastPlayed 
                                             && deckHandler.enemyHand[1] == deckHandler.lastPlayed)
        {
            chatManager.SendToActionLog("Enemy draws a card because he can't play the card in hand");
            deckHandler.DrawForEnemy();
            
        }
        //coin flip for draw / play card
        else if (deckHandler.enemyHand.Count < 4 && _random.Next(0,2) == 1 || deckHandler.enemyHand.Count == 0)
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
        string winner;
        if (_slots[pos].Color == color || _slots[pos].Color == "none")
        {
            //place new token
            _slots[pos].Tokens.Add( Instantiate(tokens[token], slots[pos * 3 + _slots[pos].Count]));
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
                }
                else
                {
                    chatManager.SendToActionLog("You Lost!");
                    state = GameState.Lost;
                }
            }
            
            CheckGameOver(); 
            return;
        }
        
        //slot is occupied by opponent -> disable the opponent's tokens from this slot
        for (int i = 0; i < _slots[pos].Count; i++)
        {
            _slots[pos].Tokens[i].SetActive(false);
        }
        _slots[pos].Tokens.Clear();

        //place new token
        int tokensTaken = _slots[pos].Count;
        _slots[pos].Count = 1;
        _slots[pos].Color = color;
        _slots[pos].Tokens.Add(Instantiate(tokens[token], slots[pos * 3]));
        
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
            if (_slots[line * 3].Color == "white" &&
                _slots[line * 3 + 1].Color == "white" && _slots[line * 3 + 2].Color == "white")
            {
                chatManager.SendToActionLog("You Won!");
                state = GameState.Won;
                return;
            }
            
            if (_slots[line * 3].Color == "red" &&
                _slots[line * 3 + 1].Color == "red" && _slots[line * 3 + 2].Color == "red")
            {
                chatManager.SendToActionLog("You Lost!");
                state = GameState.Lost;
                return;
            }
            
            if (_slots[line].Color == "white" && _slots[line + 3].Color == "white" && _slots[line + 6].Color == "white")
            {
                chatManager.SendToActionLog("You Won!");
                state = GameState.Won;
                return;
            }
            
            if (_slots[line].Color == "red" && _slots[line + 3].Color == "red" && _slots[line + 6].Color == "red")
            {
                chatManager.SendToActionLog("You Lost!");
                state = GameState.Lost;
                return;
            }
        }
        
        //check diagonals
        if (_slots[0].Color == "white" && _slots[4].Color == "white" && _slots[8].Color == "white")
        {
            chatManager.SendToActionLog("You Won!");
            state = GameState.Won;
            return;

        }
        
        if (_slots[0].Color == "red" && _slots[4].Color == "red" && _slots[8].Color == "red")
        {
            chatManager.SendToActionLog("You Lost!");
            state = GameState.Lost;
            return;
        }

        if (_slots[2].Color == "white" && _slots[4].Color == "white" && _slots[6].Color == "white")
        {
            chatManager.SendToActionLog("You Won!");
            state = GameState.Won;

        }
        
        if (_slots[2].Color == "red" && _slots[4].Color == "red" && _slots[6].Color == "red")
        {
            chatManager.SendToActionLog("You Lost!");
            state = GameState.Lost;
        }
        if (int.Parse(enemyTokens.text) == 0)
        {
            chatManager.SendToActionLog("You Won!");
            state = GameState.Won;
            return;
        }
            
        if (int.Parse(playerTokens.text) == 0)
        {
            chatManager.SendToActionLog("You Lost!");
            state = GameState.Lost;
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
            playerTokensCaptured.text = (int.Parse(playerTokensCaptured.text) - 1).ToString();
            alreadyAsked = true;
        }
        else
        {
            chatManager.SendToActionLog("Action requires one enemy token!");
        }
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