using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject mainMenuCanvas;
    public GameObject onlineMenuCanvas;
    public GameObject chooseDifficultyCanvas;
    public GameObject gameRulesCanvas;
    public void PlayButtonHandler(){
        AudioManager.instance.Play("menu_button");
        mainMenuCanvas.SetActive(false);
        chooseDifficultyCanvas.SetActive(true);
    }

    public void OnlineButtonHandler()
    {
        AudioManager.instance.Play("menu_button");
        mainMenuCanvas.SetActive(false);
        onlineMenuCanvas.SetActive(true);
        
    }

    public void GameRulesButtonHandler()
    {
        AudioManager.instance.Play("menu_button");
        mainMenuCanvas.SetActive(false);
        gameRulesCanvas.SetActive(true);
    }

    public void QuitGame()
    {
        AudioManager.instance.Play("menu_button");
        Debug.Log("quit");
        Application.Quit();
    }
}
