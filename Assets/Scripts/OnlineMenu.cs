using UnityEngine;

public class OnlineMenu : MonoBehaviour
{
    public GameObject mainMenuCanvas;
    public GameObject onlineMenuCanvas;
    public GameObject connectMenuCanvas;
    public GameObject hostMenuCanvas;
    
    public void ConnectButtonHandler()
    {
        AudioManager.instance.Play("menu_button");
        onlineMenuCanvas.SetActive(false);
        connectMenuCanvas.SetActive(true);
    }
    
    public void HostButtonHandler()
    {
        AudioManager.instance.Play("menu_button");
        onlineMenuCanvas.SetActive(false);
        hostMenuCanvas.SetActive(true);
        
        ConnectionManager.instance.HostButton();
    }
    
    public void BackButtonHandler()
    {
        AudioManager.instance.Play("menu_button");
        onlineMenuCanvas.SetActive(false);
        mainMenuCanvas.SetActive(true);
    }
    
}
