using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = System.Random;

public class DeckHandler : MonoBehaviour
{
    private List<int> deck = new List<int>(6);
    public Player player1;
    public Player player2;
    private int cardsInDeck = 18;
    private Random generator = new Random();
    
    public List<int> playedCards = new List<int>(18);
    public int lastPlayed = -1;
    public Image lastCardImage;

    public List<GameObject> cards;
    [FormerlySerializedAs("cardHolder")] 
    public Transform player1CardHolder;
    [FormerlySerializedAs("enemyCardHolder")] 
    public Transform player2CardHolder;
    public Transform playedCardsHolder;
    public GameObject panel;
    public GameSystem gameSystem;
    public ChatManager chatManager;

    //deck starts with 3 copies of each card
    private void Start()
    {
        for (int i = 0; i < 6; i++)
        {
            deck.Add(3);
        }

        player1 = gameSystem.player1;
        player2 = gameSystem.player2;
    }

    public void ResetDeck()
    {
        playedCards.Clear();
        player1.cardsInHand.Clear();
        player2.cardsInHand.Clear();
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
        chatManager.SendToActionLog("Shuffling deck!");
        playedCards.Clear();
        foreach (Transform child in playedCardsHolder) {
            Destroy(child.gameObject);
        }
        for (int i = 0; i < 6; i++)
        {
            deck[i] = 3;
        }

        cardsInDeck = 18;

        //remove cards that player 1 holds
        for (int i = 0; i < player1.cardsInHand.Count; i++)
        {
            deck[player1.cardsInHand[i]]--;
            cardsInDeck--;
        }

        //remove cards that player 2 holds
        for (int i = 0; i < player2.cardsInHand.Count; i++)
        {
            deck[player2.cardsInHand[i]]--;
            cardsInDeck--;
        }

        if (lastPlayed >= 0)
        {
            deck[lastPlayed]--;
            playedCards.Add(lastPlayed);
            InstantiatePlayedCard(lastPlayed);
            cardsInDeck--;
        }
    }

    //draw for player 2 (no human error check)
    public void DrawForPlayer2()
    {
        if(gameSystem.state != GameState.Enemyturn) return;
        
        if (player2.cardsInHand.Count == 4)
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
        player2.cardsInHand.Add(card);
        Instantiate(cards[6], Instantiate(panel, player2CardHolder).transform);
        gameSystem.state = GameState.Playerturn;
    }

    //draw a card for the player 1 (human player)
    public void DrawForPlayer1()
    {
        if (gameSystem.state != GameState.Playerturn) return;
        if (player1.ForcedToPlay && !(player1.cardsInHand.Count == 1 && player1.cardsInHand[0] == lastPlayed ||
                                               player1.cardsInHand.Count == 2 && player1.cardsInHand[0] == lastPlayed &&
                                               player1.cardsInHand[1] == lastPlayed))
        {
            chatManager.SendToActionLog("You are forced to play a card this turn!");
            return;
        }

        if (player1.cardsInHand.Count == 4)
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
        player1.cardsInHand.Add(card);
        //ui draw
        Instantiate(cards[card], Instantiate(panel, player1CardHolder).transform);
        gameSystem.state = GameState.Enemyturn;
    }

    public void RemoveFromPlayer1(int card)
    {
        player1.cardsInHand.Remove(card);
    }

    public void RemoveFromPlayer2(int card)
    {
        player2.cardsInHand.Remove(card);
    }

    //setup for start of game
    public void GameSetup()
    {
        //the starting player has 3 cards, the other has 4
        int player1CardCount, player2CardCount;
        if (gameSystem.state == GameState.Playerturn)
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
            player2.cardsInHand.Add(card);
            Instantiate(cards[6], Instantiate(panel, player2CardHolder).transform);
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
            player1.cardsInHand.Add(card);
            //ui draw
            Instantiate(cards[card], Instantiate(panel, player1CardHolder).transform);
        }
        
        //draw play card
        card = generator.Next(0, 6);
        while (deck[card] == 0)
        {
            card = generator.Next(0, 6);
        }

        lastPlayed = card;
        playedCards.Add(card);
        InstantiatePlayedCard(card);
        deck[card]--;
        cardsInDeck--;
        lastCardImage.sprite = cards[card].GetComponent<Image>().sprite;
        
        chatManager.SendToActionLog("Enemy card total is: " + player2.cardsInHand.Sum());

    }
    
    public void InstantiatePlayedCard(int card)
    {
        Instantiate(cards[card], Instantiate(panel, playedCardsHolder).transform);
    }
}
