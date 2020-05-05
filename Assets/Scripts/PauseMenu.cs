using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class PauseMenu : MonoBehaviour
{
    public static bool GameIsPaused = false;
    public GameObject pauseMenuPanel;
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
        Time.timeScale = 1f;
        GameIsPaused = false;
    }

    void Pause()
    {
        pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f;
        GameIsPaused = true;
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
        Application.Quit();
    }

    public void QuitToMainMenu()
    {
        Server server = FindObjectOfType<Server>();
        if (server != null)
        {
            Destroy(server.gameObject);
        }

        Client client = FindObjectOfType<Client>();
        if (client != null)
        {
            Destroy(client.gameObject);
        }

        SceneManager.LoadScene("Menu");
    }
}
