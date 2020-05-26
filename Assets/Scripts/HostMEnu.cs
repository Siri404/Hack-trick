using UnityEngine;

public class HostMenu : MonoBehaviour
{
    public GameObject onlineMenuCanvas;
    public GameObject hostMenuCanvas;
    
    
    public void BackButtonHandler()
    {
        AudioManager.instance.Play("menu_button");
        hostMenuCanvas.SetActive(false);
        onlineMenuCanvas.SetActive(true);

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
    }
}
