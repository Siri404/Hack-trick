using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void PlayGame(){
        AudioManager.instance.Play("menu_button");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void QuitGame()
    {
        AudioManager.instance.Play("menu_button");
        Debug.Log("quit");
        Application.Quit();
    }
}
