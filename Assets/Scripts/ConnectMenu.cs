using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConnectMenu : MonoBehaviour
{
    public GameObject onlineMenuCanvas;
    public GameObject connectMenuCanvas;
    
    
    public void BackButtonHandler()
    {
        AudioManager.instance.Play("menu_button");
        connectMenuCanvas.SetActive(false);
        onlineMenuCanvas.SetActive(true);
    }
}
