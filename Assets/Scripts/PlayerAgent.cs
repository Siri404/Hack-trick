using System.Collections.Generic;
using MLAgents;
using MLAgents.Sensors;
using UnityEngine;

public class PlayerAgent : Agent
{
    private float penalty = 1f;
    public Player Player { get; set; }
    public Player Opponent { get; set; }

    public override void CollectDiscreteActionMasks(DiscreteActionMasker actionMasker)
    {
        List<int> illegalActions = new List<int>();
        for (int i = 0; i < 6; i++)
        {
            if (!Player.CardsInHand.Contains(i))
            {
                illegalActions.Add(i);
            }
        }

        if (!illegalActions.Contains(DeckHandler.instance.lastPlayed))
        {
            illegalActions.Add(DeckHandler.instance.lastPlayed);
        }

        if (Player.CardsInHand.Count == 4)
        {
            illegalActions.Add(6);
        }

        if (Player.ForcedToPlay && !illegalActions.Contains(6))
        {
            bool canPlay;

            if (Player.CardsInHand.Count == 1 && Player.CardsInHand[0] == DeckHandler.instance.lastPlayed || 
                Player.CardsInHand.Count == 2 && Player.CardsInHand[0] == DeckHandler.instance.lastPlayed 
                                              && Player.CardsInHand[1] == DeckHandler.instance.lastPlayed)
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
        if (int.Parse(Player.Tokens.text) < 2)
        {
            actionMasker.SetMask(1, new []{1});
            actionMasker.SetMask(2, new []{1});
        }

        if (Opponent.CardsInHand.Count == 0 || Opponent.CardsInHand.Count == 4 || Opponent.Blocking)
        {
            actionMasker.SetMask(2, new []{1});
        }
    }
    
    public override void CollectObservations(VectorSensor sensor)
    {
        float[] board = new float[9];
        for (int i = 0; i < 9; i++)
        {
            if (BoardManager.instance.Slots[i].Color.Equals("none"))
            {
                board[i] = 0f;
            }
            else if(BoardManager.instance.Slots[i].Color.Equals(Opponent.Color))
            {
                board[i] = BoardManager.instance.Slots.Count;
            }
            else
            {
                board[i] = -1 * BoardManager.instance.Slots.Count;
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
        sensor.AddObservation(Player.CardsInHand.Count);
        //1 float
        sensor.AddObservation(Opponent.CardsInHand.Count);
        
        //9+1+1+1+1+1 = 14 values
        
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        if (Player.Color == "white" && GameSystem.instance.state != GameState.Playerturn)
        {
            return;
        }

        if (Player.Color == "red" && GameSystem.instance.state != GameState.Enemyturn)
        {
            return;
        }
        int blocking = Mathf.FloorToInt(vectorAction[1]);
        if (blocking == 1)
        {
            if (Player.Color == "white")
            {
                GameSystem.instance.BlockForPlayer();
            }
            else
            {
                GameSystem.instance.BlockForEnemy();
            }
            AddReward(penalty);
        }

        int forcingPlayerToPlay = Mathf.FloorToInt(vectorAction[2]);
        if (forcingPlayerToPlay == 1)
        {
            if (Player.Color == "white")
            {
                GameSystem.instance.ForceEnemyToPlay();
            }
            else
            {
                GameSystem.instance.ForcePlayerToPlay();
            }
            AddReward(penalty);
        }

        int move = Mathf.FloorToInt(vectorAction[0]);
        
        //check if chosen action is legal
        if (move == 6 && Player.CardsInHand.Count < 4 && !Player.ForcedToPlay)
        {
            if (Player.Color == "white")
            {
                DeckHandler.instance.DrawForPlayer1();
            }
            else
            {
                DeckHandler.instance.DrawForPlayer2();
            }   
        }
        else
        {
            int newTokensCaptured = int.Parse(Player.CapturedTokens.text);
            int card = (int) vectorAction[0];
            
            //check if chosen action is legal
            if (!Player.CardsInHand.Contains(card) || card == DeckHandler.instance.lastPlayed)
            {
                bool mustDraw = true;
                for (int i = 0; i < Player.CardsInHand.Count; i++)
                {
                    if (Player.CardsInHand[i] != DeckHandler.instance.lastPlayed)
                    {
                        card = Player.CardsInHand[i];
                        mustDraw = false;
                        break;
                    }
                }

                if (mustDraw)
                {
                    if (Player.Color == "white")
                    {
                        DeckHandler.instance.DrawForPlayer1();
                    }
                    else
                    {
                        DeckHandler.instance.DrawForPlayer2();
                    }
                    return;
                }
            }
            
            //remove & destroy played card
            if (Player.Color == "white")
            {
                DeckHandler.instance.RemoveFromPlayer1(card);
                UserInterfaceManager.instance.DestroyCardFromPlayer1CardHolder(card);
            }
            else
            {
                DeckHandler.instance.RemoveFromPlayer2(card);
                UserInterfaceManager.instance.DestroyCardFromPlayer2CardHolder();
            }

            //get the position on board for token placement
            int pos = DeckHandler.instance.lastPlayed + card - 1;
            
            //set last played card
            DeckHandler.instance.lastPlayed = card;
            DeckHandler.instance.playedCards.Add(card);
            UserInterfaceManager.instance.InstantiatePlayedCard(card);
            
            BoardManager.instance.PlaceToken(pos, Player.Color, Player.TokenType);
            newTokensCaptured = int.Parse(Player.CapturedTokens.text) - newTokensCaptured;
            AddReward(newTokensCaptured);
            if (Player.Color == "white")
            {
                if (GameSystem.instance.state == GameState.Playerturn)
                {
                    GameSystem.instance.state = GameState.Enemyturn;
                }
            }
            else
            {
                if (GameSystem.instance.state == GameState.Enemyturn)
                {
                    GameSystem.instance.state = GameState.Playerturn;
                }
            }
        }
    }

}
