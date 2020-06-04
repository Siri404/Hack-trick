using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

public class DeckHandler : MonoBehaviour
{
    public static DeckHandler instance { get; set; }
    
    private List<int> deck = new List<int>(6);
    public Player player1;
    public Player player2;
    private int cardsInDeck = 18;
    private Random generator = new Random();
    
    public List<int> playedCards = new List<int>(18);
    public int lastPlayed = -1;

    //deck starts with 3 copies of each card
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
        
        for (int i = 0; i < 6; i++)
        {
            deck.Add(3);
        }

        player1 = GameSystem.instance.player1;
        player2 = GameSystem.instance.player2;
    }

    public void SetDeck(List<int> deck)
    {
        this.deck = deck;
        int sum = 0;
        foreach (int i in deck)
        {
            sum += i;
        }

        cardsInDeck = sum;
    }

    public List<int> GetDeck()
    {
        return deck;
    }
    
    public void ResetDeck()
    {
        playedCards.Clear();
        player1.CardsInHand.Clear();
        player2.CardsInHand.Clear();
        deck.Clear();
        for (int i = 0; i < 6; i++)
        {
            deck.Add(3);
        }

        cardsInDeck = 18;
        lastPlayed = 0;
        GameSetup();
    }
    
    //shuffle the deck, cards in hand and last played are not returned to the shuffled deck
    private void shuffle_deck()
    {
        ChatManager.instance.SendToActionLog("Shuffling deck!");
        
        //reset played cards
        playedCards.Clear();
        UserInterfaceManager.instance.DestroyFromPlayedCardHolder();
        
        for (int i = 0; i < 6; i++)
        {
            deck[i] = 3;
        }

        cardsInDeck = 18;

        //remove cards that player 1 holds
        for (int i = 0; i < player1.CardsInHand.Count; i++)
        {
            deck[player1.CardsInHand[i]]--;
            cardsInDeck--;
        }

        //remove cards that player 2 holds
        for (int i = 0; i < player2.CardsInHand.Count; i++)
        {
            deck[player2.CardsInHand[i]]--;
            cardsInDeck--;
        }

        if (lastPlayed >= 0)
        {
            deck[lastPlayed]--;
            playedCards.Add(lastPlayed);
            UserInterfaceManager.instance.InstantiatePlayedCard(lastPlayed);
            cardsInDeck--;
        }
    }

    //draw a specific card for player2
    public void DrawForPlayer2(int card)
    {
        if (cardsInDeck == 0)
        {
            shuffle_deck();
        }
        
        deck[card]--;
        cardsInDeck--;
        player2.CardsInHand.Add(card);
        PlayDrawCardSound();
        
        //ui draw
        UserInterfaceManager.instance.InstantiateCardForPlayer2(card);
        
        ChatManager.instance.SendToActionLog("Enemy draws a card");
        GameSystem.instance.state = GameState.Playerturn;
    }
    
    //draw random card for player 2
    public void DrawForPlayer2()
    {
        if(GameSystem.instance.state != GameState.Enemyturn) return;
        
        if (player2.CardsInHand.Count == 4)
        {
            //can't have more than 4 cards in hand
            return;
        }

        if (cardsInDeck == 0)
        {
            shuffle_deck();
        }

        //pick a random card
        int card = generator.Next(0, 6);
        while (deck[card] == 0)
        {
            card = generator.Next(0, 6);
        }

        deck[card]--;
        cardsInDeck--;
        player2.CardsInHand.Add(card);
        PlayDrawCardSound();
        
        //ui draw
        UserInterfaceManager.instance.InstantiateCardForPlayer2(card);
        
        ChatManager.instance.SendToActionLog("Enemy draws a card");
        GameSystem.instance.state = GameState.Playerturn;
    }

    //draw a card for the player 1 (human player)
    public void DrawForPlayer1()
    {
        if (GameSystem.instance.state != GameState.Playerturn) return;
        if (player1.ForcedToPlay && !(player1.CardsInHand.Count == 1 && player1.CardsInHand[0] == lastPlayed ||
                                               player1.CardsInHand.Count == 2 && player1.CardsInHand[0] == lastPlayed &&
                                               player1.CardsInHand[1] == lastPlayed))
        {
            ChatManager.instance.SendToActionLog("You are forced to play a card this turn!");
            return;
        }

        if (player1.CardsInHand.Count == 4)
        {
            //can't have more than 4 cards in hand
            return;
        }

        if (cardsInDeck == 0)
        {
            shuffle_deck();
        }

        //pick a random card
        int card = generator.Next(0, 6);
        while (deck[card] == 0)
        {
            card = generator.Next(0, 6);
        }
        
        deck[card]--;
        cardsInDeck--;
        player1.CardsInHand.Add(card);
        PlayDrawCardSound();
        
        //ui draw
        UserInterfaceManager.instance.InstantiateCardForPlayer1(card);
        
        GameSystem.instance.playerActionVector[0] = 1;
        GameSystem.instance.playerActionVector[4] = card;
        ChatManager.instance.SendToActionLog("Player draws a card");
        
        //record move for imitation learning
        GameSystem.instance.heuristicActionVector[0] = 6;
        
        GameSystem.instance.state = GameState.Enemyturn;
    }

    public void RemoveFromPlayer1(int card)
    {
        player1.CardsInHand.Remove(card);
    }

    public void RemoveFromPlayer2(int card)
    {
        player2.CardsInHand.Remove(card);
    }

    //setup for start of game
    public void GameSetup()
    {
        //the starting player has 3 cards, the other has 4
        int player1CardCount, player2CardCount;
        if (GameSystem.instance.state == GameState.Playerturn)
        {
            player1CardCount = 3;
            player2CardCount = 4;
        }
        else
        {
            player1CardCount = 4;
            player2CardCount = 3;
        }
        
        //draw for player 2
        int card;
        for (int i = 0; i < player2CardCount; i++)
        {
            card = generator.Next(0, 6);
            while (deck[card] == 0)
            {
                card = generator.Next(0, 6);
            }

            deck[card]--;
            cardsInDeck--;
            player2.CardsInHand.Add(card);
            
            //ui draw
            UserInterfaceManager.instance.InstantiateCardForPlayer2(card);
        }

        //draw for player 1
        for (int i = 0; i < player1CardCount; i++)
        {
            card = generator.Next(0, 6);
            while (deck[card] == 0)
            {
                card = generator.Next(0, 6);
            }

            deck[card]--;
            cardsInDeck--;
            player1.CardsInHand.Add(card);
            
            //ui draw
            UserInterfaceManager.instance.InstantiateCardForPlayer1(card);
        }
        
        //draw play card
        card = generator.Next(0, 6);
        while (deck[card] == 0)
        {
            card = generator.Next(0, 6);
        }

        lastPlayed = card;
        playedCards.Add(card);
        UserInterfaceManager.instance.InstantiatePlayedCard(card);
        deck[card]--;
        cardsInDeck--;
        ChatManager.instance.SendToActionLog("Enemy card total is: " + player2.CardsInHand.Sum());
    }

    public void PlayDrawCardSound()
    {
        int i = generator.Next(1, 4);
        FindObjectOfType<AudioManager>().Play("Draw" + i);
    }
}
