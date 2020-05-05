using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;


public class ConnectionManager : MonoBehaviour
{
    public TMP_InputField hostInput;
    public TMP_InputField nameInput;
    public static ConnectionManager instance { set; get; }
    public GameObject serverPrefab;
    public GameObject clientPrefab;

    public void Start()
    {
        if (instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }
    }

    public void ConnectToServerButton()
    {
        string hostAddress = hostInput.text;
        if (hostAddress == "")
        {
            hostAddress = "127.0.0.1";
        }

        if (hostAddress.Equals("bobo", StringComparison.InvariantCultureIgnoreCase))
        {
            hostAddress = "188.24.118.216";
        }
        
        if (hostAddress.Equals("vali", StringComparison.InvariantCultureIgnoreCase))
        {
            hostAddress = "109.101.213.156";
        }
        
        if (hostAddress.Equals("iulian", StringComparison.InvariantCultureIgnoreCase))
        {
            hostAddress = "109.96.38.202";
        }
        
        

        try
        {
            Client client = Instantiate(clientPrefab).GetComponent<Client>();
            client.clientName = nameInput.text;
            if (client.clientName == "")
            {
                client.clientName = "Client";
            }
            client.ConnectToServer(hostAddress, Server.port);
            
            //start game
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }
    }

    public void HostButton()
    {
        try
        {
            Server server = Instantiate(serverPrefab).GetComponent<Server>();
            server.Init();
            
            Client client = Instantiate(clientPrefab).GetComponent<Client>();
            client.clientName = nameInput.text;
            client.isHost = true;
            if (client.clientName == "")
            {
                client.clientName = "Host";
            }
            client.ConnectToServer("127.0.0.1", Server.port);
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }
    }

    public void StartMultiplayerGame()
    {
        SceneManager.LoadScene("Game");
    }
    
}
