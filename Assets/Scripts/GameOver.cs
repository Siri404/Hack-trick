using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameOver : MonoBehaviour
{
    public TMP_Text text;
    
    public void GameOverDialogue()
    {
        if (GameSystem.instance.state == GameState.Won)
        {
            text.text = "You Won!";
        }
        else if (GameSystem.instance.state == GameState.Lost)
        {
            text.text = "You Lost!";
        }
        else return;
        
        gameObject.SetActive(true);
    }
}
