using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class Server : MonoBehaviour
{
    public int port = 6321;

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
            
            StartListening();
        }
        catch (Exception e)
        {
            Debug.Log("Socket error: " + e.Message);
        }
    }

    private void Update()
    {
        if (!serverStarted)
        {
            return;
        }

        foreach (ServerClient client in clients)
        {
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
        ServerClient serverClient = new ServerClient(listener.EndAcceptTcpClient(ar));
        clients.Add(serverClient);
        
        Debug.Log("Somebody has connected!");
        
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

    //server send
    private void BroadCast(string data, List<ServerClient> clients)
    {
        foreach (ServerClient client in clients)
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
        Debug.Log(client.clientName + ": " + data);
    }
}

public class ServerClient
{
    public string clientName;
    public TcpClient tcp;

    public ServerClient(TcpClient tcp)
    {
        this.tcp = tcp;
    }
}