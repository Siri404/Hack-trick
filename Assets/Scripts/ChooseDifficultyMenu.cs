using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChooseDifficultyMenu : MonoBehaviour
{
    public GameObject chooseDifficultyCanvas;
    public GameObject mainMenuCanvas;
    
    public void BackButtonHandler()
    {
        AudioManager.instance.Play("menu_button");
        chooseDifficultyCanvas.SetActive(false);
        mainMenuCanvas.SetActive(true);
    }
    
    public void StartEasyGame(){
        AudioManager.instance.Play("menu_button");
        DifficultyManager.instance.difficulty = Difficulty.Easy;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void StartHardGame()
    {
        AudioManager.instance.Play("menu_button");
        DifficultyManager.instance.difficulty = Difficulty.Hard;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
}
