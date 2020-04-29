using System.Collections.Generic;
using MLAgents;
using MLAgents.Sensors;
using UnityEngine;
using UnityEngine.UI;

public class PlayerAgent : Agent
{
    public GameSystem gameSystem;
    private float penalty = 1f;
    public Player Player { get; set; }
    public Player Opponent { get; set; }

    public override void CollectDiscreteActionMasks(DiscreteActionMasker actionMasker)
    {
        List<int> illegalActions = new List<int>();
        for (int i = 0; i < 6; i++)
        {
            if (!Player.cardsInHand.Contains(i))
            {
                illegalActions.Add(i);
            }
        }

        if (!illegalActions.Contains(gameSystem.deckHandler.lastPlayed))
        {
            illegalActions.Add(gameSystem.deckHandler.lastPlayed);
        }

        if (Player.cardsInHand.Count == 4)
        {
            illegalActions.Add(6);
        }

        if (Player.ForcedToPlay && !illegalActions.Contains(6))
        {
            bool canPlay;

            if (Player.cardsInHand.Count == 1 && Player.cardsInHand[0] == gameSystem.deckHandler.lastPlayed || 
                Player.cardsInHand.Count == 2 && Player.cardsInHand[0] == gameSystem.deckHandler.lastPlayed 
                                              && Player.cardsInHand[1] == gameSystem.deckHandler.lastPlayed)
            {
                canPlay = false;
            }
            else
            {
                canPlay = true;
            }

            if (canPlay)
            {
                illegalActions.Add(6);
            }
        }

        actionMasker.SetMask(0, illegalActions);
        if (int.Parse(gameSystem.playerTokens.text) < 2)
        {
            actionMasker.SetMask(1, new []{1});
            actionMasker.SetMask(2, new []{1});
        }

        if (Opponent.cardsInHand.Count == 0 || Opponent.cardsInHand.Count == 4 || Opponent.Blocking)
        {
            actionMasker.SetMask(2, new []{1});
        }
    }
    
    public override void CollectObservations(VectorSensor sensor)
    {
        float[] board = new float[9];
        for (int i = 0; i < 9; i++)
        {
            if (gameSystem.Slots[i].Color.Equals("none"))
            {
                board[i] = 0f;
            }
            else if(gameSystem.Slots[i].Color.Equals(Opponent.Color))
            {
                board[i] = gameSystem.Slots.Count;
            }
            else
            {
                board[i] = -1 * gameSystem.Slots.Count;
            }
        }
        //9 floats
        sensor.AddObservation(board);
        //1 float
        sensor.AddObservation(float.Parse(Player.Tokens.text));
        //1 float
        sensor.AddObservation(float.Parse(Player.CapturedTokens.text));
        //1 float
        sensor.AddObservation(float.Parse(Opponent.Tokens.text));
        //1 float
        sensor.AddObservation(Player.cardsInHand.Count);
        //1 float
        sensor.AddObservation(Opponent.cardsInHand.Count);
        
        //9+1+1+1+1+1 = 14 values
        
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        if (Player.Color == "white" && gameSystem.state != GameState.Playerturn)
        {
            return;
        }

        if (Player.Color == "red" && gameSystem.state != GameState.Enemyturn)
        {
            return;
        }
        int blocking = Mathf.FloorToInt(vectorAction[1]);
        if (blocking == 1)
        {
            if (Player.Color == "white")
            {
                gameSystem.BlockForPlayer();
            }
            else
            {
                gameSystem.BlockForEnemy();
            }
            AddReward(penalty);
        }

        int forcingPlayerToPlay = Mathf.FloorToInt(vectorAction[2]);
        if (forcingPlayerToPlay == 1)
        {
            if (Player.Color == "white")
            {
                gameSystem.ForceEnemyToPlay();
            }
            else
            {
                gameSystem.ForcePlayerToPlay();
            }
            AddReward(penalty);
        }

        int move = Mathf.FloorToInt(vectorAction[0]);
        
        //check if chosen action is legal
        if (move == 6 && Player.cardsInHand.Count < 4 && !Player.ForcedToPlay)
        {
            if (Player.Color == "white")
            {
                gameSystem.deckHandler.DrawForPlayer1();
            }
            else
            {
                gameSystem.deckHandler.DrawForPlayer2();
            }   
        }
        else
        {
            int newTokensCaptured = int.Parse(Player.CapturedTokens.text);
            int card = (int) vectorAction[0];
            
            //check if chosen action is legal
            if (!Player.cardsInHand.Contains(card) || card == gameSystem.deckHandler.lastPlayed)
            {
                bool mustDraw = true;
                for (int i = 0; i < Player.cardsInHand.Count; i++)
                {
                    if (Player.cardsInHand[i] != gameSystem.deckHandler.lastPlayed)
                    {
                        card = Player.cardsInHand[i];
                        mustDraw = false;
                        break;
                    }
                }

                if (mustDraw)
                {
                    if (Player.Color == "white")
                    {
                        gameSystem.deckHandler.DrawForPlayer1();
                    }
                    else
                    {
                        gameSystem.deckHandler.DrawForPlayer2();
                    }
                    return;
                }
            }
            
            //remove & destroy played card
            if (Player.Color == "white")
            {
                gameSystem.deckHandler.RemoveFromPlayer1(card);
                gameSystem.DestroyCardFromPlayerHolder(card);
            }
            else
            {
                gameSystem.deckHandler.RemoveFromPlayer2(card);
                gameSystem.DestroyCardFromEnemyHolder();
            }

            //get the position on board for token placement
            int pos = gameSystem.deckHandler.lastPlayed + card - 1;
            
            //set last played card
            gameSystem.deckHandler.lastPlayed = card;
            gameSystem.deckHandler.playedCards.Add(card);
            gameSystem.deckHandler.InstantiatePlayedCard(card);
            gameSystem.lastCardImage.sprite = gameSystem.deckHandler.cards[card].GetComponent<Image>().sprite;
            
            gameSystem.PlaceToken(pos, Player.Color, Player.TokenType);
            newTokensCaptured = int.Parse(Player.CapturedTokens.text) - newTokensCaptured;
            AddReward(newTokensCaptured);
            if (Player.Color == "white")
            {
                if (gameSystem.state == GameState.Playerturn)
                {
                    gameSystem.state = GameState.Enemyturn;
                }
            }
            else
            {
                if (gameSystem.state == GameState.Enemyturn)
                {
                    gameSystem.state = GameState.Playerturn;
                }
            }
        }
    }

}
