using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject mainMenuCanvas;
    public GameObject onlineMenuCanvas;
    public void StartSingleplayerGame(){
        AudioManager.instance.Play("menu_button");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void OnlineButtonHandler()
    {
        AudioManager.instance.Play("menu_button");
        mainMenuCanvas.SetActive(false);
        onlineMenuCanvas.SetActive(true);
        
    }

    public void QuitGame()
    {
        AudioManager.instance.Play("menu_button");
        Debug.Log("quit");
        Application.Quit();
    }
}
