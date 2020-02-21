using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum GameState { Start, Playerturn, Enemyturn, Won, Lost}
public class game_system : MonoBehaviour
{
    public List<Transform> slots;
    private List<Slot> _slots = new List<Slot>(9); 
    public GameState state;
    public List<GameObject> tokens;

    public TMP_Text playerTokens;
    public TMP_Text playerTokensCaptured;
    public TMP_Text enemyTokens;
    public TMP_Text enemyTokensCaptured;
    private void Start()
    {
        state = GameState.Start;
        StartCoroutine(SetupGame());
    }

    IEnumerator SetupGame()
    {
        for (int i = 0; i < 9; i++)
        {
            _slots.Add(new Slot());
        }
        state = GameState.Playerturn;
        playerTokens.text = "10";
        playerTokensCaptured.text = "0";
        enemyTokens.text = "10";
        enemyTokensCaptured.text = "0";
        yield return new WaitForSeconds(2f);
        Debug.Log("Starting player turn");
        StartCoroutine(PlayerTurn());
    }

    IEnumerator PlayerTurn()
    {
        Debug.Log("player turn");
        int tokensTaken = PlaceToken(5, "white", 1);
        playerTokensCaptured.text = (Int32.Parse(playerTokensCaptured.text) + tokensTaken).ToString();
        playerTokens.text = (Int32.Parse(playerTokens.text) - 1).ToString();
        yield return new WaitForSeconds(2f);
        Debug.Log("Starting enemy turn");

        StartCoroutine(EnemyTurn());
    }

    IEnumerator EnemyTurn()
    {
        Debug.Log("enemy turn");

        int tokensTaken = PlaceToken(5, "red", 0);
        enemyTokensCaptured.text = (Int32.Parse(enemyTokensCaptured.text) + tokensTaken).ToString();
        enemyTokens.text = (Int32.Parse(enemyTokens.text) - 1).ToString();
        yield return new WaitForSeconds(2f);
    }

    int PlaceToken(int pos, string color, int token)
    {
        if (pos < 0 || pos > 8 || token < 0 || token > 3)
        {
            Debug.Log("invalid pos");
            return -1;
        }
        if (_slots[pos].Color == color || _slots[pos].Color == "none")
        {
            _slots[pos].Tokens.Add( Instantiate(tokens[token], slots[pos * 3 + _slots[pos].Count]));
            _slots[pos].Count += 1;
            _slots[pos].Color = color;
            return 0;
        }

        for (int i = 0; i < _slots[pos].Count; i++)
        {
            _slots[pos].Tokens[i].SetActive(false);
        }
        _slots[pos].Tokens.Clear();

        int tokensTaken = _slots[pos].Count;
        _slots[pos].Count = 1;
        _slots[pos].Color = color;
        _slots[pos].Tokens.Add(Instantiate(tokens[token], slots[pos * 3]));
        return tokensTaken;

    }
}

class Slot
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
}