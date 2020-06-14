using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UserInterfaceManager : MonoBehaviour
{
    public static UserInterfaceManager instance { set; get; }
    
    public TMP_Text whitePlayerTokens;
    public TMP_Text whitePlayerTokensCaptured;
    public TMP_Text redPlayerTokens;
    public TMP_Text redPlayerTokensCaptured;
    
    public Image lastCardImage;
    
    public Transform player1CardHolder;
    public Transform player2CardHolder;
    public Transform playedCardsHolder;
    public GameObject cardSlot;
    public List<Transform> slotTransforms;
    public List<GameObject> cards;
    public List<GameObject> tokens;

    public void DestroyCardsFromHolders()
    {
        //destroy player cards
        Transform[] children = player1CardHolder.GetComponentsInChildren<Transform>();
        for (int i = 1; i < children.Length; i++)
        {
            Destroy(children[i].gameObject);
        }
        
        //destroy enemy cards
        children = player2CardHolder.GetComponentsInChildren<Transform>();
        for (int i = 1; i < children.Length; i++)
        {
            Destroy(children[i].gameObject);
        }

        //destroy played cards
        children = playedCardsHolder.GetComponentsInChildren<Transform>();
        for (int i = 1; i < children.Length; i++)
        {
            Destroy(children[i].gameObject);
        }
    }
    
    public void DestroyCardFromPlayer1CardHolder(int card)
    {
        Transform[] transforms = player1CardHolder.GetComponentsInChildren<Transform>();
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

    public void DestroyCardFromPlayer2CardHolder()
    {
        Transform[] transforms = player2CardHolder.GetComponentsInChildren<Transform>();
        if (transforms.Length > 1)
        {
            Destroy(transforms[1].gameObject);
        }
        else
        {
            Console.Write("Enemy has no cards -> can't destroy card from enemy holder!");
        }
    }

    public void DestroyFromPlayedCardHolder()
    {
        foreach (Transform child in playedCardsHolder) {
            Destroy(child.gameObject);
        }
    }

    public void Start()
    {
        //only one UserInterfaceManager instance should exist
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        ResetTextInfo();
        instance = this;
        
    }

    public void ResetTextInfo()
    {
        whitePlayerTokens.text = "10";
        whitePlayerTokensCaptured.text = "0";
        redPlayerTokens.text = "10";
        redPlayerTokensCaptured.text = "0";
    }

    public void InstantiatePlayedCard(int card)
    {
        Instantiate(cards[card], Instantiate(cardSlot, playedCardsHolder).transform);
        lastCardImage.sprite = cards[card].GetComponent<Image>().sprite;
    }

    public void InstantiateCardForPlayer1(int card)
    {
        Instantiate(cards[card], Instantiate(cardSlot, player1CardHolder).transform);
    }

    public void InstantiateCardForPlayer2()
    {
        Instantiate(cards[6], Instantiate(cardSlot, player2CardHolder).transform);
    }

    public GameObject InstantiateToken(int token, int pos, int tokensOnSlot)
    {
        return Instantiate(tokens[token], slotTransforms[pos * 3 + tokensOnSlot]);
    }

    public void UseWhitePlayerToken()
    {
        whitePlayerTokens.text = (int.Parse(whitePlayerTokens.text) - 1).ToString();
    }

    public void UseWhitePlayerCapturedToken(int tokensTaken)
    { 
        whitePlayerTokensCaptured.text = (Int32.Parse(whitePlayerTokensCaptured.text) + tokensTaken).ToString();
    }

    public void UseRedPlayerToken()
    {
        redPlayerTokens.text = (int.Parse(redPlayerTokens.text) - 1).ToString();
    }

    public void UseRedPlayerCapturedToken(int tokensTaken)
    {
        redPlayerTokensCaptured.text = (int.Parse(redPlayerTokensCaptured.text) + tokensTaken).ToString();
    }
    
}
