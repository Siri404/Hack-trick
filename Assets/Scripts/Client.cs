using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using UnityEngine;

public class Client : MonoBehaviour
{
    public string clientName;
    public bool isHost = false;

    private List<GameClient> playersInRoom = new List<GameClient>();
    private bool socketReady = false;
    private TcpClient socket;
    private NetworkStream stream;
    private StreamWriter writer;
    private StreamReader reader;

    public void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public bool ConnectToServer(string host, int port)
    {
        if (socketReady) return false;

        try
        {
            socket = new TcpClient(host, port);
            stream = socket.GetStream();
            writer = new StreamWriter(stream);
            reader = new StreamReader(stream);

            socketReady = true;
        }
        catch (Exception e)
        {
            Debug.Log("Socket error: " + e.Message);
        }

        return socketReady;
    }
    
    //client read
    private void OnIncomingData(string data)
    {
        Debug.Log("Client: " + data);
        string[] splitData = data.Split('|');

        switch (splitData[0])
        {
            case "SHello":
                for (int i = 1; i < splitData.Length - 1; i++)
                {
                    UserConnected(splitData[i], false);
                }
                Send("CHello|" + clientName + "|" + ((isHost)?1:0));
                break;
            case "SInfo":
                UserConnected(splitData[1], false);
                break;
            case "setup":
                GameSystem.instance.ReceiveGameSetup(data);
                break;
            case "move":
                List<int> actionVector = new List<int>();
                foreach (string action in splitData[1].Split(','))
                {
                    actionVector.Add(Int32.Parse(action));
                }
                GameSystem.instance.ExecuteEnemyActionVector(actionVector);
                break;
            case "restart":
                GameSystem.instance.ResetGame();
                break;
            case "disconnect":
                ChatManager.instance.SendToActionLog(splitData[1]);
                break;
        }
    }
    
    //client send
    public void Send(string data)
    {
        if (!socketReady) return;
        
        writer.WriteLine(data);
        writer.Flush();
    }

    private void Update()
    {
        if (socketReady)
        {
            if (stream.DataAvailable)
            {
                string data = reader.ReadLine();
                if (data != null)
                {
                    OnIncomingData(data);
                }
            }
        }
    }

    private void UserConnected(string name, bool isHost)
    {
        GameClient client = new GameClient();
        client.name = name;
        playersInRoom.Add(client);
        if (playersInRoom.Count == 2)
        {
            ConnectionManager.instance.StartMultiplayerGame();
        }
    }

    private void CloseSocket()
    {
        if (!socketReady)
        {
            return;
        }
        
        writer.Close();
        reader.Close();
        socket.Close();

        socketReady = false;
    }

    private void OnApplicationQuit()
    {
        CloseSocket();
    }

    private void OnDisable()
    {
        CloseSocket();
    }
}

public class GameClient
{
    public string name;
    public bool isHost;
    
}