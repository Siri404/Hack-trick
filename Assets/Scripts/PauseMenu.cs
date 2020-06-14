using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static bool GameIsPaused = false;
    public GameObject pauseMenuPanel;
    public GameObject gameRulesPanel;
    public GameObject gameOverPanel;
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !gameOverPanel.activeSelf)
        {
            if (GameIsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
        pauseMenuPanel.SetActive(false);
        gameRulesPanel.SetActive(false);
        Time.timeScale = 1f;
        GameIsPaused = false;
    }

    void Pause()
    {
        pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f;
        GameIsPaused = true;
    }
    
    public void GameRules()
    {
        pauseMenuPanel.SetActive(false);
        gameRulesPanel.SetActive(true);
    }
    
    public void GameRulesBack()
    {
        pauseMenuPanel.SetActive(true);
        gameRulesPanel.SetActive(false);
    }

    public void RestartGame()
    {
        pauseMenuPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        Time.timeScale = 1f;
        if (GameSystem.isMultiplayer && !GameSystem.instance.client.isHost)
        {
            ChatManager.instance.SendToActionLog("Only host can restart the game!");
            return;
        }

        if (GameSystem.instance.state != GameState.Start)
        {
            GameSystem.instance.ResetGame();
        }
    }

    public void QuitGame()
    {
        Client client = FindObjectOfType<Client>();
        if (client != null)
        {
            client.CloseSocket();
            Destroy(client.gameObject);
        }
        
        Server server = FindObjectOfType<Server>();
        if (server != null)
        {
            Destroy(server.gameObject);
        }

        Application.Quit();
    }

    public void QuitToMainMenu()
    {
        Client client = FindObjectOfType<Client>();
        if (client != null)
        {
            Destroy(client.gameObject);
        }
        
        Server server = FindObjectOfType<Server>();
        if (server != null)
        {
            Destroy(server.gameObject);
        }

        SceneManager.LoadScene("Menu");
    }
}
