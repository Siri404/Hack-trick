using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameOver : MonoBehaviour
{
    public TMP_Text text;
    public GameSystem gameSystem;
    public void GameOverDialogue()
    {
        if (gameSystem.state == GameState.Won)
        {
            text.text = "You Won!";
        }
        else if (gameSystem.state == GameState.Lost)
        {
            text.text = "You Lost!";
        }
        else return;
        
        gameObject.SetActive(true);
    }
}
