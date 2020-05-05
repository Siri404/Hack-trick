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
    public static GameSystem instance { set; get; }
    
    [FormerlySerializedAs("slots")] public List<Transform> slotTransforms;
    public List<Slot> Slots { get; } = new List<Slot>(9);

    //used for easy win check
    private readonly List<int> _slotConverter = new List<int>(9);
    public GameState state;
    public List<GameObject> tokens;
    //private AudioManager audioManager = AudioManager.instance;
    //private DeckHandler deckHandler = DeckHandler.instance;
    //private ChatManager chatManager = ChatManager.instance;


    public Player player1;
    public Player player2;
    public TMP_Text playerTokens;
    public TMP_Text playerTokensCaptured;
    public TMP_Text enemyTokens;
    public TMP_Text enemyTokensCaptured;
    public Image lastCardImage;
    public GameOver gameOver;
    public GameObject EnemyCardHolder;
    public GameObject PlayerCardHolder;
    public GameObject PlayedCardsPanel;
    
    private bool alreadyAsked;
    private readonly Random _random = new Random();

    public PlayerAgent playerAgent2;

    public Client client;
    private Server server;
    public static bool isMultiplayer = true;
    public bool waitingForServer = true;
    public List<int> playerActionVector = new List<int> {0, 0, 0, 0, 0};


    public void ResetGame()
    {
        if (client.isHost)
        {
            client.Send("restart");
        }
        else
        {
            waitingForServer = true;
        }
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
        
        ChatManager.instance.SendToActionLog("Game reset!");
        state = GameState.Start;
        StartCoroutine(SetupGame());
    }
    private void Start()
    {
        if (instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }

        server = FindObjectOfType<Server>();
        client = FindObjectOfType<Client>();

        if (client == null)
        {
            isMultiplayer = false;
        }

        if (isMultiplayer && !client.isHost)
        {
            player2 = new Player(playerTokens, playerTokensCaptured, "white", 1 );
            player1 = new Player(enemyTokens, enemyTokensCaptured, "red", 0 );
        }
        else
        {
            player1 = new Player(playerTokens, playerTokensCaptured, "white", 1 );
            player2 = new Player(enemyTokens, enemyTokensCaptured, "red", 0 );
            
        }
        playerAgent2.Player = player2;
        playerAgent2.Opponent = player1;
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
        CheckGameOver();
        alreadyAsked = false;
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
            CheckGameOver();
            
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
            //randomAction / request decision
            playerAgent2.RequestDecision();
            //EnemyRandomAction();
            yield return new WaitUntil(() => state != GameState.Enemyturn);
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

            DeckHandler.instance.RemoveFromPlayer2(card);
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
            int pos = DeckHandler.instance.lastPlayed + card - 1;
            
            // set last played card
            DeckHandler.instance.lastPlayed = card;
            DeckHandler.instance.playedCards.Add(card);
            DeckHandler.instance.InstantiatePlayedCard(card);
            lastCardImage.sprite = DeckHandler.instance.cards[card].GetComponent<Image>().sprite;
            
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

        PlayPlaceTokenSound();
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
    }

    private void CheckGameOver()
    {

        //check for lines & columns
        for (int line = 0; line < 3; line++)
        {
            if (Slots[_slotConverter[line * 3]].Color == player1.Color && 
                Slots[_slotConverter[line * 3 + 1]].Color == player1.Color && 
                Slots[_slotConverter[line * 3 + 2]].Color == player1.Color)
            {
                ChatManager.instance.SendToActionLog("You Won!");
                state = GameState.Won;
                gameOver.GameOverDialogue();
                return;
            }
            
            if (Slots[_slotConverter[line * 3]].Color == player2.Color && Slots[_slotConverter[line * 3 + 1]].Color == player2.Color 
                                                                       && Slots[_slotConverter[line * 3 + 2]].Color == player2.Color)
            {
                ChatManager.instance.SendToActionLog("You Lost!");
                state = GameState.Lost;
                gameOver.GameOverDialogue();
                return;
            }
            
            if (Slots[_slotConverter[line]].Color == player1.Color && Slots[_slotConverter[line + 3]].Color == player1.Color 
                                                                   && Slots[_slotConverter[line + 6]].Color == player1.Color)
            {
                ChatManager.instance.SendToActionLog("You Won!");
                state = GameState.Won;
                gameOver.GameOverDialogue();
                return;
            }
            
            if (Slots[_slotConverter[line]].Color == player2.Color && Slots[_slotConverter[line + 3]].Color == player2.Color
                                                                   && Slots[_slotConverter[line + 6]].Color == player2.Color)
            {
                ChatManager.instance.SendToActionLog("You Lost!");
                state = GameState.Lost;
                gameOver.GameOverDialogue();
                return;
            }
        }
        
        //check diagonals
        if (Slots[_slotConverter[0]].Color == player1.Color && Slots[_slotConverter[4]].Color == player1.Color
                                                            && Slots[_slotConverter[8]].Color == player1.Color)
        {
            ChatManager.instance.SendToActionLog("You Won!");
            state = GameState.Won;
            gameOver.GameOverDialogue();
            return;

        }
        
        if (Slots[_slotConverter[0]].Color == player2.Color && Slots[_slotConverter[4]].Color == player2.Color
                                                            && Slots[_slotConverter[8]].Color == player2.Color)
        {
            ChatManager.instance.SendToActionLog("You Lost!");
            state = GameState.Lost;
            gameOver.GameOverDialogue();
            return;
        }

        if (Slots[_slotConverter[2]].Color == player1.Color && Slots[_slotConverter[4]].Color == player1.Color 
                                                            && Slots[_slotConverter[6]].Color == player1.Color)
        {
            ChatManager.instance.SendToActionLog("You Won!");
            state = GameState.Won;
            gameOver.GameOverDialogue();
            return;
        }
        
        if (Slots[_slotConverter[2]].Color == player2.Color && Slots[_slotConverter[4]].Color == player2.Color
                                                            && Slots[_slotConverter[6]].Color == player2.Color)
        {
            ChatManager.instance.SendToActionLog("You Lost!");
            state = GameState.Lost;
            gameOver.GameOverDialogue();
            return;
        }
        
        //check tokens
        if (int.Parse(player2.Tokens.text) == 0)
        {
            ChatManager.instance.SendToActionLog("You Won!");
            state = GameState.Won;
            gameOver.GameOverDialogue();
            return;
        }
            
        if (int.Parse(player1.Tokens.text) == 0)
        {
            ChatManager.instance.SendToActionLog("You Lost!");
            state = GameState.Lost;
            gameOver.GameOverDialogue();
        }
        
        //check for stacks of 3 tokens
        for (int i = 0; i < 9; i++)
        {
            if (Slots[i].Count == 3)
            {
                if (Slots[i].Color == player1.Color)
                {
                    ChatManager.instance.SendToActionLog("You Won!");
                    state = GameState.Won;
                    gameOver.GameOverDialogue();
                }
                else
                {
                    ChatManager.instance.SendToActionLog("You Lost!");
                    state = GameState.Lost;
                    gameOver.GameOverDialogue();
                }
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
            if (alreadyAsked) return;
            alreadyAsked = true;
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
        else if (int.Parse(playerTokens.text) > 2)
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

    public void PlayPlaceTokenSound()
    {
        int i = _random.Next(1, 6);
        AudioManager.instance.Play("place_token" + i);
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
            DeckHandler.instance.InstantiateCardForPlayer(Int32.Parse(card));
        }

        player1.CardsInHand = player1Hand;
        
        List<int> player2Hand = new List<int>();
        foreach (string card in splitData[2].Split(','))
        {
            player2Hand.Add(Int32.Parse(card));
            DeckHandler.instance.InstantiateCardForEnemy();
        }

        player2.CardsInHand = player2Hand;
        
        DeckHandler.instance.lastPlayed = Int32.Parse(splitData[4]);
        DeckHandler.instance.playedCards.Add(DeckHandler.instance.lastPlayed);
        DeckHandler.instance.InstantiatePlayedCard(DeckHandler.instance.lastPlayed);
        lastCardImage.sprite = DeckHandler.instance.cards[DeckHandler.instance.lastPlayed].GetComponent<Image>().sprite;
        
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
            DestroyCardFromEnemyHolder();

            //get the position on board for token placement
            int pos = DeckHandler.instance.lastPlayed + card - 1;
            
            //set last played card
            DeckHandler.instance.lastPlayed = card;
            DeckHandler.instance.playedCards.Add(card);
            DeckHandler.instance.InstantiatePlayedCard(card);
            lastCardImage.sprite = DeckHandler.instance.cards[card].GetComponent<Image>().sprite;
            
            PlaceToken(pos, player2.Color, player2.TokenType);
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