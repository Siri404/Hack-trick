using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Random = System.Random;

public enum GameState { Start, Playerturn, Enemyturn, Won, Lost}
public class GameSystem : MonoBehaviour
{
    public static GameSystem instance { set; get; }
    
    public GameState state;

    public Player player1;
    public Player player2;
    
    public GameOver gameOver;

    private bool playerAskedSum;
    private readonly Random _random = new Random();

    public PlayerAgent playerAgent2;
    public PlayerAgentHard playerAgentHard2;

    public Client client;
    private Server server;
    public static bool isMultiplayer = true;
    public bool waitingForServer = true;
    public List<int> playerActionVector = new List<int> {0, 0, 0, 0, 0};


    public void ResetGame()
    {
        if (isMultiplayer)
        {
            if (client.isHost)
            {
                client.Send("restart");
            }
            else
            {
                waitingForServer = true;
            }
        }
        //destroy cards for player and enemy
        UserInterfaceManager.instance.DestroyCardsFromHolders();

        //reset all slots
        BoardManager.instance.ResetSlots();
        
        ChatManager.instance.SendToActionLog("Game reset!");
        state = GameState.Start;
        StartCoroutine(SetupGame());
    }
    private void Start()
    {
        //only one GameSystem instance should exist
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;

        server = FindObjectOfType<Server>();
        client = FindObjectOfType<Client>();

        if (client == null)
        {
            isMultiplayer = false;
        }

        if (isMultiplayer && !client.isHost)
        {
            player2 = new Player( UserInterfaceManager.instance.whitePlayerTokens, 
                UserInterfaceManager.instance.whitePlayerTokensCaptured, "white", 1 );
            player1 = new Player(UserInterfaceManager.instance.redPlayerTokens, 
                UserInterfaceManager.instance.redPlayerTokensCaptured, "red", 0 );
        }
        else
        {
            player1 = new Player(UserInterfaceManager.instance.whitePlayerTokens, 
                UserInterfaceManager.instance.whitePlayerTokensCaptured, "white", 1 );
            player2 = new Player(UserInterfaceManager.instance.redPlayerTokens, 
                UserInterfaceManager.instance.redPlayerTokensCaptured, "red", 0 );
            
        }
        playerAgent2.Player = player2;
        playerAgentHard2.Player = player2;
        playerAgent2.Opponent = player1;
        playerAgentHard2.Opponent = player1;

        state = GameState.Start;
        StartCoroutine(SetupGame());
    }

    public IEnumerator SetupGame()
    {
        //set the text info
        UserInterfaceManager.instance.ResetTextInfo();
        
        //reset players action state
        player1.ForcedToPlay = false;
        player2.ForcedToPlay = false;
        player1.Blocking = false;
        player2.Blocking = false;
        playerAskedSum = false;

        yield return new WaitForSeconds(2f);
        if (isMultiplayer)
        {
            //multi player setup
            if (client.isHost)
            {
                //coin flip to decide starting player
                if (_random.Next(0, 2) == 1)
                {
                    state = GameState.Playerturn;
                    DeckHandler.instance.ResetDeck();
                    SendGameSetup();
                    StartCoroutine(PlayerTurn());
                }
                else
                {
                    state = GameState.Enemyturn;
                    DeckHandler.instance.ResetDeck();
                    SendGameSetup();
                    StartCoroutine(EnemyTurn());
                }
            }
            else
            {
                //wait for server to tell client to call ReceiveGameSetup
                yield return new WaitUntil(() => !waitingForServer);
                
                ChatManager.instance.SendToActionLog("Enemy card total is: " + player2.CardsInHand.Sum());
                if (player2.CardsInHand.Count == 4)
                {
                    state = GameState.Playerturn;
                    StartCoroutine(PlayerTurn());
                }
                else
                {
                    state = GameState.Enemyturn;
                    StartCoroutine(EnemyTurn());
                }
            }
        }
        //single player setup
        else
        {
            //coin flip to decide starting player
            if (_random.Next(0, 2) == 1)
            {
                state = GameState.Playerturn;
                DeckHandler.instance.ResetDeck();
                StartCoroutine(PlayerTurn());
            }
            else
            {
                state = GameState.Enemyturn;
                DeckHandler.instance.ResetDeck();
                StartCoroutine(EnemyTurn());
            }
        }
        
    }

    IEnumerator PlayerTurn()
    {
        BoardManager.instance.CheckGameOver();
        playerAskedSum = false;
        playerActionVector[3] = 0;
        while (state == GameState.Playerturn)
        {
            ChatManager.instance.SendToActionLog("Waiting for player");

            yield return new WaitUntil(() => state != GameState.Playerturn);
        }
        
        if (isMultiplayer)
        {
            waitingForServer = true;
        }
        //game not over -> sendPlayerMove
        if (state == GameState.Enemyturn)
        {
            if (isMultiplayer)
            {
                SendPlayerMove();
            }
            BoardManager.instance.CheckGameOver();
            
            //if still game not over -> enemy turn
            if (state == GameState.Enemyturn)
            {
                StartCoroutine(EnemyTurn());
            }
        }
        
    }
    
    IEnumerator EnemyTurn()
    {
        ChatManager.instance.SendToActionLog("Enemy turn");
        if (!isMultiplayer)
        {
            //reset forcedToPlay after player turn is over
            player1.ForcedToPlay = false;
            player2.Blocking = false;
            
            yield return new WaitForSeconds(3f);
            //take a random action / request decision from agent
            //playerAgent2.RequestDecision();
            playerAgentHard2.RequestDecision();
            //EnemyRandomAction();
            yield return new WaitUntil(() => state != GameState.Enemyturn);
            
            //reset forcedToPlay after enemy turn is over
            player2.ForcedToPlay = false;
            player1.Blocking = false;
            StartCoroutine(PlayerTurn());
        }
        //else, game waits for server to call ExecuteEnemyActionVector
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

        if (player2.CardsInHand.Count == 1 && player2.CardsInHand[0] == DeckHandler.instance.lastPlayed || 
            player2.CardsInHand.Count == 2 && player2.CardsInHand[0] == DeckHandler.instance.lastPlayed 
                                           && player2.CardsInHand[1] == DeckHandler.instance.lastPlayed)
        {
            if (player2.ForcedToPlay)
            {
                ChatManager.instance.SendToActionLog("Enemy draws a card because he can't play the card(s) in hand");
            }
            else
            {
                ChatManager.instance.SendToActionLog("Enemy draws a card");
            }

            DeckHandler.instance.DrawForPlayer2();
            
        }
        
        //coin flip for draw / play card
        else if (player2.CardsInHand.Count < 4 && _random.Next(0,2) == 1 && player2.ForcedToPlay == false 
                 || player2.CardsInHand.Count == 0)
        {
            ChatManager.instance.SendToActionLog("Enemy draws a card");
            DeckHandler.instance.DrawForPlayer2();
        }
        else
        {
            ChatManager.instance.SendToActionLog("Enemy plays a card");
            //play random card from hand
            int card = DeckHandler.instance.lastPlayed;
            while (card == DeckHandler.instance.lastPlayed)
            {
                card = player2.CardsInHand[_random.Next(0, player2.CardsInHand.Count)];
            }

            //remove played card from enemy
            DeckHandler.instance.RemoveFromPlayer2(card);
            UserInterfaceManager.instance.DestroyCardFromPlayer2CardHolder();
            
            ChatManager.instance.SendToActionLog("Enemy played a " + card);

            
            // get the position on board for token placement
            int pos = DeckHandler.instance.lastPlayed + card - 1;
            
            // set last played card
            DeckHandler.instance.lastPlayed = card;
            DeckHandler.instance.playedCards.Add(card);
            UserInterfaceManager.instance.InstantiatePlayedCard(card);

            BoardManager.instance.PlaceToken(pos, player2.Color, 0);
            if (state == GameState.Enemyturn)
            {
                state = GameState.Playerturn;
            }
        }
        
    }

    public void AskSum()
    {
        AudioManager.instance.Play("menu_button");
        if(state != GameState.Playerturn) return;
        if (int.Parse(player1.CapturedTokens.text) > 0)
        {
            ChatManager.instance.SendToActionLog("Enemy card total is: " + player2.CardsInHand.Sum());
            
            //use only one token per turn
            if (playerAskedSum) return;
            playerAskedSum = true;
            playerActionVector[3] = 1;
            player1.CapturedTokens.text = (int.Parse(player1.CapturedTokens.text) - 1).ToString();
        }
        else
        {
            ChatManager.instance.SendToActionLog("Action requires one enemy token!");
        }
    }

    public void EnemyAskedSum()
    {
        player2.CapturedTokens.text = (int.Parse(player2.CapturedTokens.text) - 1).ToString();
        ChatManager.instance.SendToActionLog("Enemy asked your card sum!");
    }

    public void ForceEnemyToPlay()
    {
        AudioManager.instance.Play("menu_button");
        if (state != GameState.Playerturn) return;
        if (player2.Blocking)
        {
            ChatManager.instance.SendToActionLog("Enemy is blocking!");
            return;
        }
        if (player2.ForcedToPlay)
        {
            player2.ForcedToPlay = false;
            player1.Tokens.text = (int.Parse(player1.Tokens.text) + 1).ToString();
            playerActionVector[2] = 0;
            ChatManager.instance.SendToActionLog("Enemy no longer forced to play next turn");
            return;
        }
        if (player2.CardsInHand.Count == 0)
        {
            ChatManager.instance.SendToActionLog("Enemy does not have any cards, can't be forced to play next turn!");
            return;
        }

        if (int.Parse(player1.Tokens.text) < 2)
        {
            ChatManager.instance.SendToActionLog("You can't sacrifice your last token!");
            return;
        }
        
        player2.ForcedToPlay = true;
        player1.Tokens.text = (int.Parse(player1.Tokens.text) - 1).ToString();
        playerActionVector[2] = 1;
        ChatManager.instance.SendToActionLog("Enemy forced to play next turn");
    }

    public void ForcePlayerToPlay()
    {
        if (state != GameState.Enemyturn || player1.Blocking || player1.CardsInHand.Count == 0 || 
            int.Parse(player2.Tokens.text) < 2) return;

        player1.ForcedToPlay = true;
        player2.Tokens.text = (int.Parse(player2.Tokens.text) - 1).ToString();
        ChatManager.instance.SendToActionLog("Player forced to play next turn");
    }

    public void BlockForPlayer()
    {
        AudioManager.instance.Play("menu_button");
        if(state != GameState.Playerturn) return;
        if (player1.Blocking)
        {
            player1.Blocking = false;
            player1.Tokens.text = (int.Parse(player1.Tokens.text) + 1).ToString();
            playerActionVector[1] = 0;
            ChatManager.instance.SendToActionLog("You are no longer blocking");
        }
        else if (int.Parse(player1.Tokens.text) > 2)
        {
            player1.Blocking = true;
            player1.Tokens.text = (int.Parse(player1.Tokens.text) - 1).ToString();
            playerActionVector[1] = 1;
            ChatManager.instance.SendToActionLog("You are now blocking");
        }
        else
        {
            ChatManager.instance.SendToActionLog("You can't sacrifice your last token!");
        }
    }

    public void BlockForEnemy()
    {
        if (state != GameState.Enemyturn  || int.Parse(player2.Tokens.text) < 2) return;
        
        player2.Blocking = true;
        player2.Tokens.text = (int.Parse(player2.Tokens.text) - 1).ToString();
        ChatManager.instance.SendToActionLog("Enemy blocking next turn!");
    }

    public void ReceiveGameSetup(string data)
    {
        Debug.Log("receiving setup");
        string[] splitData = data.Split('|');
        
        List<int> deck = new List<int>();
        foreach (string card in splitData[1].Split(','))
        {
            deck.Add(Int32.Parse(card));
        }
        DeckHandler.instance.SetDeck(deck);
        
        List<int> player1Hand = new List<int>();
        foreach (string card in splitData[3].Split(','))
        {
            player1Hand.Add(Int32.Parse(card));
            UserInterfaceManager.instance.InstantiateCardForPlayer1(Int32.Parse(card));
        }

        player1.CardsInHand = player1Hand;
        
        List<int> player2Hand = new List<int>();
        foreach (string card in splitData[2].Split(','))
        {
            player2Hand.Add(Int32.Parse(card));
            UserInterfaceManager.instance.InstantiateCardForPlayer2();
        }

        player2.CardsInHand = player2Hand;
        
        DeckHandler.instance.lastPlayed = Int32.Parse(splitData[4]);
        DeckHandler.instance.playedCards.Add(DeckHandler.instance.lastPlayed);
        UserInterfaceManager.instance.InstantiatePlayedCard(DeckHandler.instance.lastPlayed);

        waitingForServer = false;
    }

    public void SendGameSetup()
    {
        string deckString = "";
        foreach (int i in DeckHandler.instance.GetDeck())
        {
            deckString += i + ",";
        }
        deckString = deckString.Remove(deckString.Length - 1);

        string player1String = "";
        foreach (int i in player1.CardsInHand)
        {
            player1String += i + ",";
        }
        player1String = player1String.Remove(player1String.Length - 1);
        
        string player2String = "";
        foreach (int i in player2.CardsInHand)
        {
            player2String += i + ",";
        }
        player2String = player2String.Remove(player2String.Length - 1);
        
        string data = "setup|" + deckString + "|" + player1String + "|" + player2String + "|" + DeckHandler.instance.lastPlayed;
        client.Send(data);
    }

    public void SendPlayerMove()
    {
        string data;
        if (client.isHost)
        {
            data = "hMove|";

        }
        else
        {
            data = "gMove|";
        }
        foreach (int i in playerActionVector)
        {
            data += i + ",";
        }
        data = data.Remove(data.Length - 1);
        
        client.Send(data);
    }

    public void ExecuteEnemyActionVector(List<int> actionVector)
    {
        //reset forcedToPlay after player turn is over
        player1.ForcedToPlay = false;
        playerActionVector[2] = 0;
        player2.Blocking = false;
        
        if (actionVector[1] == 1)
        {
            BlockForEnemy();
        }

        if (actionVector[2] == 1)
        {
            ForcePlayerToPlay();
        }

        if (actionVector[3] == 1)
        {
            EnemyAskedSum();
        }
        
        if (actionVector[0] == 1)
        {
            DeckHandler.instance.DrawForPlayer2(actionVector[4]);
        }
        else
        {
            int card = actionVector[4];
            
            //remove & destroy played card
            DeckHandler.instance.RemoveFromPlayer2(card);
            UserInterfaceManager.instance.DestroyCardFromPlayer2CardHolder();

            //get the position on board for token placement
            int pos = DeckHandler.instance.lastPlayed + card - 1;
            
            //set last played card
            DeckHandler.instance.lastPlayed = card;
            DeckHandler.instance.playedCards.Add(card);
            UserInterfaceManager.instance.InstantiatePlayedCard(card);
            ChatManager.instance.SendToActionLog("Enemy played a " + card);
            BoardManager.instance.PlaceToken(pos, player2.Color, player2.TokenType);
        }
        state = GameState.Playerturn;
        waitingForServer = false;
        
        //reset forcedToPlay here to avoid bug by player spamming the button right before his turn
        player2.ForcedToPlay = false;
        player1.Blocking = false;
        playerActionVector[1] = 0;
        
        StartCoroutine(PlayerTurn());
    }
}

public class Player
{
    public bool ForcedToPlay;
    public bool Blocking;
    public List<int> CardsInHand { set; get; } = new List<int>(4);
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