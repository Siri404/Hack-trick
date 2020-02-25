using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

public class DeckHandler : MonoBehaviour
{
    private List<int> deck = new List<int>(6);
    public List<int> playerHand = new List<int>(4);
    public List<int> enemyHand = new List<int>(4);
    private int cardsInDeck = 18;
    private Random generator = new Random();
    
    public int lastPlayed = -1;
    public Image lastCardImage;

    public List<GameObject> cards;
    public Transform cardHolder;
    public Transform enemyCardHolder;
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
    }
    
    //shuffle the deck, cards in hand and last played are not returned to the shuffled deck
    private void shuffle_deck()
    {
        for (int i = 0; i < 6; i++)
        {
            deck[i] = 3;
        }

        for (int i = 0; i < playerHand.Count; i++)
        {
            deck[playerHand[i]]--;
        }

        for (int i = 0; i < enemyHand.Count; i++)
        {
            deck[enemyHand[i]]--;
        }

        if (lastPlayed >= 0)
        {
            deck[lastPlayed]--;
        }
    }

    public void DrawForEnemy()
    {
        if(gameSystem.state != GameState.Enemyturn) return;
        
        if (enemyHand.Count == 4)
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
        enemyHand.Add(card);
        Instantiate(cards[6], Instantiate(panel, enemyCardHolder).transform);
    }

    //draw a card for the player whose turn is currently ongoing
    public void DrawForPlayer()
    {
        if (gameSystem.state != GameState.Playerturn) return;
        if (gameSystem.playerForcedToPlay && !(playerHand.Count == 1 && playerHand[0] == lastPlayed ||
                                               playerHand.Count == 2 && playerHand[0] == lastPlayed &&
                                               playerHand[1] == lastPlayed))
        {
            chatManager.SendToActionLog("You are forced to play a card this turn!");
            return;
        }

        if (playerHand.Count == 4)
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
        playerHand.Add(card);
        //ui draw
        Instantiate(cards[card], Instantiate(panel, cardHolder).transform);
        gameSystem.state = GameState.Enemyturn;
    }

    public void RemoveFromPlayer(int card)
    {
        playerHand.Remove(card);
    }

    public void RemoveFromEnemy(int card)
    {
        enemyHand.Remove(card);
    }

    //setup for start of game
    public void GameSetup()
    {
        //the starting player has 3 cards, the other has 4
        int playerCardCount, enemyCardCount;
        if (gameSystem.state == GameState.Playerturn)
        {
            playerCardCount = 3;
            enemyCardCount = 4;
        }
        else
        {
            playerCardCount = 4;
            enemyCardCount = 3;
        }
        
        //draw for opponent
        int card;
        for (int i = 0; i < enemyCardCount; i++)
        {
            card = generator.Next(0, 6);
            while (deck[card] == 0)
            {
                card = generator.Next(0, 6);
            }

            deck[card]--;
            cardsInDeck--;
            enemyHand.Add(card);
            Instantiate(cards[6], Instantiate(panel, enemyCardHolder).transform);
        }

        //draw for player
        for (int i = 0; i < playerCardCount; i++)
        {
            card = generator.Next(0, 6);
            while (deck[card] == 0)
            {
                card = generator.Next(0, 6);
            }

            deck[card]--;
            cardsInDeck--;
            playerHand.Add(card);
            //ui draw
            Instantiate(cards[card], Instantiate(panel, cardHolder).transform);
        }
        
        //draw play card
        card = generator.Next(0, 6);
        while (deck[card] == 0)
        {
            card = generator.Next(0, 6);
        }

        lastPlayed = card;
        deck[card]--;
        lastCardImage.sprite = cards[card].GetComponent<Image>().sprite;
        
        chatManager.SendToActionLog("Enemy card total is: " + enemyHand.Sum());

    }
}
