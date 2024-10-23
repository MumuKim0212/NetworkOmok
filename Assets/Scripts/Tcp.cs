using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Net;
using System.Net.Sockets;
using System.Threading;

public class Tcp : MonoBehaviour
{
    Socket socketServer = null;
    Socket socketClient = null;

    Queue qSend;
    Queue qReceive;

    bool bServer = false;
    bool bConnect = false;

    bool bThread = false;
    Thread thread = null;

    void Start()
    {
        qSend = new Queue();
        qReceive = new Queue();
    }

    public int Send(byte[] data, int size)
    {
        if (qSend == null)
        {
            return 0;
        }

        return qSend.Add(data, size);
    }

    public int Receive(ref byte[] data, int size)
    {
        if (qReceive == null)
        {
            return 0;
        }

        return qReceive.Pop(ref data, size);
    }

    public bool IsServer()
    {
        return bServer;
    }

    public bool IsConnect()
    {
        return bConnect;
    }

    public bool StartServer(int port, int backlog)
    {
        socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socketServer.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        socketServer.Bind(new IPEndPoint(IPAddress.Any, port));
        socketServer.Listen(backlog);

        bServer = true;

        Debug.Log("서버 시작");

        return StartThread();
    }

    public void StopServer()
    {
        bThread = false;
        if (thread != null)
        {
            thread.Join();
            thread = null;
        }

        Disconnect();

        if (socketServer != null)
        {
            socketServer.Close();
            socketServer = null;
        }

        bServer = false;
    }

    public bool Connect(string address, int port)
    {
        try
        {
            socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socketClient.Connect(address, port);
            if (StartThread())
            {
                bConnect = true;
                Debug.Log("클라이언트 접속 성공");
                return true;
            }
        }
        catch (SocketException ex)
        {
            Debug.LogError($"연결 실패: {ex.Message}");
            Disconnect();
        }
        return false;
    }

    public void Disconnect()
    {
        bConnect = false;

        if (socketClient != null)
        {
            socketClient.Shutdown(SocketShutdown.Both);
            socketClient.Close();
            socketClient = null;
        }
    }

    bool StartThread()
    {
        bThread = true;
        thread = new Thread(new ThreadStart(NetworkUpdate));
        thread.Start();

        return true;
    }

    public void NetworkUpdate()
    {
        while (bThread)
        {
            WaitClient();

            if (socketClient != null && bConnect == true)
            {
                UpdateSend();
                UpdateReceive();
            }

            Thread.Sleep(5);
        }
    }

    void WaitClient()
    {
        if (socketServer != null && socketServer.Poll(0, SelectMode.SelectRead))
        {
            socketClient = socketServer.Accept();
            bConnect = true;

            Debug.Log("클라이언트 접속 성공");
        }
    }

    void UpdateSend()
    {
        if (socketClient.Poll(0, SelectMode.SelectWrite))
        {
            byte[] data = new byte[1024];

            int iSize = qSend.Pop(ref data, data.Length);
            while (iSize > 0)
            {
                socketClient.Send(data, iSize, SocketFlags.None);
                iSize = qSend.Pop(ref data, data.Length);
            }
        }
    }

    void UpdateReceive()
    {
        while (socketClient.Poll(0, SelectMode.SelectRead))
        {
            byte[] data = new byte[1024];

            int iSize = socketClient.Receive(data, data.Length, SocketFlags.None);
            if (iSize == 0)
            {
                Disconnect();
            }
            else if (iSize > 0)
            {
                qReceive.Add(data, iSize);
            }
        }
    }
    private void CloseSocket()
    {
        if (socketServer != null)
        {
            socketServer.Shutdown(SocketShutdown.Both);
            socketServer.Close();
            socketServer = null;
        }

        if (socketClient != null)
        {
            socketClient.Shutdown(SocketShutdown.Both);
            socketClient.Close();
            socketClient = null;
        }
    }
    private void OnDestroy()
    {

        CloseSocket();
    }
    private void OnApplicationQuit()
    {
        CloseSocket();
    }
}