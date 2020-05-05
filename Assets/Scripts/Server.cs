using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class Server : MonoBehaviour
{
    public static int port = 6321;

    private List<ServerClient> clients;
    private List<ServerClient> disconnectList;

    private TcpListener server;
    private bool serverStarted;

    public void Init()
    {
        DontDestroyOnLoad(gameObject);
        clients = new List<ServerClient>();
        disconnectList = new List<ServerClient>();

        try
        {
            server = new TcpListener(IPAddress.Any, port);
            server.Start();
            serverStarted = true;
            
            StartListening();
        }
        catch (Exception e)
        {
            Debug.Log("Socket error: " + e.Message);
        }
    }

    private void OnDestroy()
    {
        server.Stop();
    }

    private void Update()
    {
        if (!serverStarted)
        {
            return;
        }

        for (var index = 0; index < clients.Count; index++)
        {
            ServerClient client = clients[index];
            //is client still connected?
            if (!IsConnected(client.tcp))
            {
                client.tcp.Close();
                disconnectList.Add(client);
            }
            else
            {
                NetworkStream stream = client.tcp.GetStream();
                if (stream.DataAvailable)
                {
                    StreamReader streamReader = new StreamReader(stream, true);
                    string data = streamReader.ReadLine();

                    if (data != null)
                    {
                        OnIncomingData(client, data);
                    }
                }
            }
        }

        for (int i = 0; i < disconnectList.Count - 1; i++)
        {
            //Tell our player somebody has disconnected
            BroadCast("disconnect|" + disconnectList[i].clientName + " has disconnected!", clients);
            
            clients.Remove(disconnectList[i]);
            disconnectList.RemoveAt(i);
        }
    }

    private void StartListening()
    {
        server.BeginAcceptTcpClient(AcceptTcpClient, server);
    }

    private void AcceptTcpClient(IAsyncResult ar)
    {
        TcpListener listener = (TcpListener) ar.AsyncState;
        
        string allUsers = "";
        foreach (ServerClient client in clients)
        {
            allUsers += client.clientName + "|";
        }
        
        ServerClient serverClient = new ServerClient(listener.EndAcceptTcpClient(ar));
        clients.Add(serverClient);
        BroadCast("SHello|" + allUsers, serverClient);
        
        StartListening();
    }

    private bool IsConnected(TcpClient client)
    {
        try
        {
            if (client?.Client == null || !client.Client.Connected) return false;
            if (client.Client.Poll(0, SelectMode.SelectRead))
            {
                return client.Client.Receive(new byte[1], SocketFlags.Peek) != 0;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    //server send one
    private void BroadCast(string data, ServerClient client)
    {
        List<ServerClient> list = new List<ServerClient> { client };
        BroadCast(data, list);
    }
    
    //server send all
    private void BroadCast(string data, List<ServerClient> serverClients)
    {
        foreach (ServerClient client in serverClients)
        {
            try
            {
                StreamWriter streamWriter = new StreamWriter(client.tcp.GetStream());
                streamWriter.WriteLine(data);
                streamWriter.Flush();
            }
            catch(Exception e)
            {
                Debug.Log("Write error: " + e.Message);
            }
        }
    }
    
    //server send
    private void OnIncomingData(ServerClient client, string data)
    {
        Debug.Log("Server: " + data);
        string[] splitData = data.Split('|');

        switch (splitData[0])
        {
            case "CHello":
                //received client name and status
                client.clientName = splitData[1];
                if (splitData[2] == "1")
                {
                    client.isHost = true;
                }
                BroadCast("SInfo|" + client.clientName, clients);
                break;
            case "setup":
                //send setup to guest
                BroadCast(data, clients[1]);
                break;
            case "gMove":
                //send to host the move made by guest
                BroadCast("move|" + splitData[1], clients[0]);
                break;
            case "hMove":
                //send to guest the move made by host
                BroadCast("move|" + splitData[1], clients[1]);
                break;
            case "restart":
                //tell guest to restart game
                BroadCast("restart", clients[1]);
                break;
        }
    }
}

public class ServerClient
{
    public string clientName;
    public TcpClient tcp;
    public bool isHost = false;

    public ServerClient(TcpClient tcp)
    {
        this.tcp = tcp;
    }
}