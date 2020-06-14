using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RulesMenu : MonoBehaviour
{
    public GameObject rulesMenuCanvas;
    public GameObject mainMenuCanvas;
    
    public void BackButtonHandler()
    {
        AudioManager.instance.Play("menu_button");
        rulesMenuCanvas.SetActive(false);
        mainMenuCanvas.SetActive(true);
    }
}
