using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

public class BoardManager : MonoBehaviour
{
    public static BoardManager instance { set; get; }
    public List<Slot> Slots { get; } = new List<Slot>(9);
    
    //used for easy win check
    private readonly List<int> _slotConverter = new List<int>(9);
    private readonly Random _random = new Random();

    public void Start()
    {
        //only one BoardManager instance should exist
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;

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
    }

    public void ResetSlots()
    {
        for (int i = 0; i < 9; i++)
        {
            Slots[i].ResetSlot();
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

        PlayPlaceTokenSound();
        //slot is friendly or unoccupied
        if (Slots[pos].Color == color || Slots[pos].Color == "none")
        {
            //place new token
            Slots[pos].Tokens.Add(UserInterfaceManager.instance.InstantiateToken(token, pos, Slots[pos].Count));
            Slots[pos].Count += 1;
            Slots[pos].Color = color;
            
            //update info on screen
            if (color == "white")
            {
                UserInterfaceManager.instance.UseWhitePlayerToken();
            }
            else
            {
                UserInterfaceManager.instance.UseRedPlayerToken();
            }
            return;
        }
        
        //slot is occupied by opponent -> disable the opponent's tokens from this slot
        for (int i = 0; i < Slots[pos].Count; i++)
        {
            Destroy(Slots[pos].Tokens[i]);
        }
        Slots[pos].Tokens.Clear();

        //place new token
        int tokensTaken = Slots[pos].Count;
        Slots[pos].Count = 1;
        Slots[pos].Color = color;
        Slots[pos].Tokens.Add(UserInterfaceManager.instance.InstantiateToken(token, pos, 0));
        
        //update info on screen
        if (color == "white")
        {
            UserInterfaceManager.instance.UseWhitePlayerToken();
            UserInterfaceManager.instance.UseRedPlayerCapturedToken(tokensTaken);
        }
        else
        {
            UserInterfaceManager.instance.UseRedPlayerToken();
            UserInterfaceManager.instance.UseRedPlayerCapturedToken(tokensTaken);
        }
    }
    
    public void CheckGameOver()
    {

        //check for lines & columns
        for (int line = 0; line < 3; line++)
        {
            if (Slots[_slotConverter[line * 3]].Color == GameSystem.instance.player1.Color && 
                Slots[_slotConverter[line * 3 + 1]].Color == GameSystem.instance.player1.Color && 
                Slots[_slotConverter[line * 3 + 2]].Color == GameSystem.instance.player1.Color)
            {
                ChatManager.instance.SendToActionLog("You Won!");
                GameSystem.instance.state = GameState.Won;
                GameSystem.instance.gameOver.GameOverDialogue();
                return;
            }
            
            if (Slots[_slotConverter[line * 3]].Color == GameSystem.instance.player2.Color 
                && Slots[_slotConverter[line * 3 + 1]].Color == GameSystem.instance.player2.Color 
                && Slots[_slotConverter[line * 3 + 2]].Color == GameSystem.instance.player2.Color)
            {
                ChatManager.instance.SendToActionLog("You Lost!");
                GameSystem.instance.state = GameState.Lost;
                GameSystem.instance.gameOver.GameOverDialogue();
                return;
            }
            
            if (Slots[_slotConverter[line]].Color == GameSystem.instance.player1.Color 
                && Slots[_slotConverter[line + 3]].Color == GameSystem.instance.player1.Color 
                && Slots[_slotConverter[line + 6]].Color == GameSystem.instance.player1.Color)
            {
                ChatManager.instance.SendToActionLog("You Won!");
                GameSystem.instance.state = GameState.Won;
                GameSystem.instance.gameOver.GameOverDialogue();
                return;
            }
            
            if (Slots[_slotConverter[line]].Color == GameSystem.instance.player2.Color 
                && Slots[_slotConverter[line + 3]].Color == GameSystem.instance.player2.Color
                && Slots[_slotConverter[line + 6]].Color == GameSystem.instance.player2.Color)
            {
                ChatManager.instance.SendToActionLog("You Lost!");
                GameSystem.instance.state = GameState.Lost;
                GameSystem.instance.gameOver.GameOverDialogue();
                return;
            }
        }
        
        //check diagonals
        if (Slots[_slotConverter[0]].Color == GameSystem.instance.player1.Color
            && Slots[_slotConverter[4]].Color == GameSystem.instance.player1.Color
            && Slots[_slotConverter[8]].Color == GameSystem.instance.player1.Color)
        {
            ChatManager.instance.SendToActionLog("You Won!");
            GameSystem.instance.state = GameState.Won;
            GameSystem.instance.gameOver.GameOverDialogue();
            return;

        }
        
        if (Slots[_slotConverter[0]].Color == GameSystem.instance.player2.Color 
            && Slots[_slotConverter[4]].Color == GameSystem.instance.player2.Color
            && Slots[_slotConverter[8]].Color == GameSystem.instance.player2.Color)
        {
            ChatManager.instance.SendToActionLog("You Lost!");
            GameSystem.instance.state = GameState.Lost;
            GameSystem.instance.gameOver.GameOverDialogue();
            return;
        }

        if (Slots[_slotConverter[2]].Color == GameSystem.instance.player1.Color 
            && Slots[_slotConverter[4]].Color == GameSystem.instance.player1.Color 
            && Slots[_slotConverter[6]].Color == GameSystem.instance.player1.Color)
        {
            ChatManager.instance.SendToActionLog("You Won!");
            GameSystem.instance.state = GameState.Won;
            GameSystem.instance.gameOver.GameOverDialogue();
            return;
        }
        
        if (Slots[_slotConverter[2]].Color == GameSystem.instance.player2.Color 
            && Slots[_slotConverter[4]].Color == GameSystem.instance.player2.Color
            && Slots[_slotConverter[6]].Color == GameSystem.instance.player2.Color)
        {
            ChatManager.instance.SendToActionLog("You Lost!");
            GameSystem.instance.state = GameState.Lost;
            GameSystem.instance.gameOver.GameOverDialogue();
            return;
        }
        
        //check tokens
        if (int.Parse(GameSystem.instance.player2.Tokens.text) == 0)
        {
            ChatManager.instance.SendToActionLog("You Won!");
            GameSystem.instance.state = GameState.Won;
            GameSystem.instance.gameOver.GameOverDialogue();
            return;
        }
            
        if (int.Parse(GameSystem.instance.player1.Tokens.text) == 0)
        {
            ChatManager.instance.SendToActionLog("You Lost!");
            GameSystem.instance.state = GameState.Lost;
            GameSystem.instance.gameOver.GameOverDialogue();
        }
        
        //check for stacks of 3 tokens
        for (int i = 0; i < 9; i++)
        {
            if (Slots[i].Count == 3)
            {
                if (Slots[i].Color == GameSystem.instance.player1.Color)
                {
                    ChatManager.instance.SendToActionLog("You Won!");
                    GameSystem.instance.state = GameState.Won;
                    GameSystem.instance.gameOver.GameOverDialogue();
                }
                else
                {
                    ChatManager.instance.SendToActionLog("You Lost!");
                    GameSystem.instance.state = GameState.Lost;
                    GameSystem.instance.gameOver.GameOverDialogue();
                }
            }
        }
    }
    
    public void PlayPlaceTokenSound()
    {
        int i = _random.Next(1, 6);
        AudioManager.instance.Play("place_token" + i);
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