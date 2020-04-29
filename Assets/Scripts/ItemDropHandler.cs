using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemDropHandler : MonoBehaviour, IDropHandler
{
    public DeckHandler deckHandler;
    public GameSystem gameSystem;
    public ChatManager chatManager;
    public void OnDrop(PointerEventData eventData)
    {
        //can play a card only during player turn
        if (gameSystem.state == GameState.Playerturn)
        {
            //get the number of played card & remove it from hand
            int card = Int32.Parse(ItemDragHandler.ObjectBeingDragged.name[5].ToString());
            if (card == deckHandler.lastPlayed)
            {
                chatManager.SendToActionLog("Can't play same card!");
                return;
            }
            deckHandler.RemoveFromPlayer1(card);
            
            //get the position on board for token placement
            int pos = deckHandler.lastPlayed + card - 1;
            
            //set last played card
            deckHandler.lastPlayed = card;
            deckHandler.playedCards.Add(card);
            deckHandler.InstantiatePlayedCard(card);
            GetComponent<Image>().sprite = ItemDragHandler.ObjectBeingDragged.GetComponent<Image>().sprite;
        
            //disable played card and it's parent slot
            Destroy(ItemDragHandler.ObjectBeingDragged.transform.parent.gameObject);
            Destroy(ItemDragHandler.ObjectBeingDragged);

            //place token on board & set enemy turn
            gameSystem.PlaceToken(pos, "white", 1);

            if (gameSystem.state == GameState.Playerturn)
            {
                gameSystem.state = GameState.Enemyturn;
            }
        }
        
    }
}
