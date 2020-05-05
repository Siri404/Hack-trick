using UnityEngine;

public class PlayedCardsManager : MonoBehaviour
{
    public static bool PanelIsOpen = false;
    [SerializeField]
    public GameObject PlayedCardsPanel;

    public void HandlePanel()
    {
        if (PanelIsOpen)
        {
            ClosePanel();
        }
        else
        {
            OpenPanel();
        }
    }

    void ClosePanel()
    {
        PlayedCardsPanel.SetActive(false);
        Time.timeScale = 1f;
        PanelIsOpen = false;
    }

    void OpenPanel()
    {
        PlayedCardsPanel.SetActive(true);
        Time.timeScale = 0f;
        PanelIsOpen = true;
    }
    
}
