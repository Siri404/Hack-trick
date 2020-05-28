using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemDropHandler : MonoBehaviour, IDropHandler
{
    
    public void OnDrop(PointerEventData eventData)
    {
        //can play a card only during player turn
        if (GameSystem.instance.state == GameState.Playerturn)
        {
            //get the number of played card & remove it from hand
            int card = Int32.Parse(ItemDragHandler.ObjectBeingDragged.name[5].ToString());
            if (card == DeckHandler.instance.lastPlayed)
            {
                ChatManager.instance.SendToActionLog("Can't play same card!");
                return;
            }
            DeckHandler.instance.RemoveFromPlayer1(card);
            GameSystem.instance.playerActionVector[0] = 0;
            GameSystem.instance.playerActionVector[4] = card;
            
            //get the position on board for token placement
            int pos = DeckHandler.instance.lastPlayed + card - 1;
            
            //set last played card
            DeckHandler.instance.lastPlayed = card;
            DeckHandler.instance.playedCards.Add(card);
            UserInterfaceManager.instance.InstantiatePlayedCard(card);
            
            ChatManager.instance.SendToActionLog("You played a " + card);
            
            //record move for imitation learning
            GameSystem.instance.heuristicActionVector[0] = card;
        
            //disable played card and it's parent slot
            Destroy(ItemDragHandler.ObjectBeingDragged.transform.parent.gameObject);
            Destroy(ItemDragHandler.ObjectBeingDragged);

            //place token on board & set enemy turn
            BoardManager.instance.PlaceToken(pos, GameSystem.instance.player1.Color, GameSystem.instance.player1.TokenType);

            if (GameSystem.instance.state == GameState.Playerturn)
            {
                GameSystem.instance.state = GameState.Enemyturn;
            }
        }
        
    }
}
