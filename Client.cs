using UnityEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TMPro;
using System.Threading;
using System.Collections.Concurrent;
using System.IO;

public class Client : MonoBehaviour
{
    Thread m_MainThread; //連線執行緒
    Thread m_SendingThread; //連線執行緒
    bool NetworkRunning;

    Socket serverSocket; //伺服器端socket
    IPAddress ip; //主機ip
    IPEndPoint ipEnd;
    byte[] sendData; //傳送的資料，必須為位元組

    public GameObject ehiL_01;
    public GameObject ehiR_01;
    public GameObject keyL_01;
    public GameObject keyR_01;
    // public GameObject ehiL_02;
    // public GameObject ehiR_02;
    // public GameObject keyL_02;
    // public GameObject keyR_02;
    public GameObject ehiTextL;
    public GameObject ehiTextR;
    ConcurrentQueue<byte[]> ehiDataL = new ConcurrentQueue<byte[]>();
    ConcurrentQueue<byte[]> ehiDataR = new ConcurrentQueue<byte[]>();
    ConcurrentQueue<byte[]> keyDataL = new ConcurrentQueue<byte[]>();
    ConcurrentQueue<byte[]> keyDataR = new ConcurrentQueue<byte[]>();
    Texture2D ehiTexL = null;
    Texture2D ehiTexR = null;
    Texture2D keyTexL = null;
    Texture2D keyTexR = null;
    
    int ImgNum = 0;
    byte[] dataEHIL;
    byte[] dataEHIR;
    byte[] dataKEYL;
    byte[] dataKEYR;
    string textL = "none";
    string textR = "none";
    
    private void OnEnable()
    {
        //定義伺服器的IP和埠，埠與伺服器對應
        ip = IPAddress.Parse("127.0.0.1");
        ipEnd = new IPEndPoint(ip, 5566);


        //開啟一個執行緒連線，必須的，否則主執行緒卡死
        m_MainThread = new Thread(SocketConnet);
        m_MainThread.Start();
        NetworkRunning = true;

    }

    private void OnDisable()
    {
        NetworkRunning = false;
        if(m_MainThread != null)
        {
            if(!m_MainThread.Join(100))
            {
                m_MainThread.Abort();
            }
        }
    }

    private void SocketConnet()
    {
        // if (serverSocket != null)
        //     serverSocket.Close();

        serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        serverSocket.Connect(ipEnd);
        //SocketSend("2");
        serverSocket.Dispose();

        var client = new TcpClient();
        client.Connect(ipEnd);

        using(var stream = client.GetStream())
        {
            BinaryReader reader = new BinaryReader(stream);
            try
            {
                int counter = 1;
                while(NetworkRunning && client.Connected && stream.CanRead)
                {
                    int length = reader.ReadInt32();
                    byte[] data = reader.ReadBytes(length);
                    print(counter);
                    if(counter == 1)
                    {
                        ehiDataL.Enqueue(data);
                    }
                    if (counter == 2)
                    {
                        textL = Encoding.ASCII.GetString(data, 0, length);
                    }
                    else if(counter == 3)
                    {
                        keyDataL.Enqueue(data);
                    }
                    else if(counter == 5)
                    {
                        ehiDataR.Enqueue(data);
                    }
                    if (counter == 6)
                    {
                        textR = Encoding.ASCII.GetString(data, 0, length);
                    }
                    else if(counter == 7)
                    {
                        keyDataR.Enqueue(data);
                    }
                    counter++;
                }
            }
            catch
            {

            }
        }
    }
    
    public void SocketSend(string sendStr)
    {
        //清空傳送快取
        sendData = new byte[1024];
        //資料型別轉換
        sendData = Encoding.ASCII.GetBytes(sendStr);
        //傳送
        serverSocket.Send(sendData);
    }

    void Update()
    {
        if(ehiDataL.Count > 0)
        {
            if(ehiTexL == null)
                ehiTexL = new Texture2D(1, 1);

            ehiDataL.TryDequeue(out dataEHIL);
            ehiTexL.LoadImage(dataEHIL);
            ehiTexL.Apply();
            ehiL_01.GetComponent<Renderer>().material.mainTexture = ehiTexL;
        }
        if (ehiDataR.Count > 0)
        {
            if (ehiTexR == null)
                ehiTexR = new Texture2D(1, 1);

            ehiDataR.TryDequeue(out dataEHIR);
            ehiTexR.LoadImage(dataEHIR);
            ehiTexR.Apply();
            ehiR_01.GetComponent<Renderer>().material.mainTexture = ehiTexR;
        }
        if (keyDataL.Count > 0)
        {
            if (keyTexL == null)
                keyTexL = new Texture2D(1, 1);

            keyDataL.TryDequeue(out dataKEYL);
            keyTexL.LoadImage(dataKEYL);
            keyTexL.Apply();
            keyL_01.GetComponent<Renderer>().material.mainTexture = keyTexL;
        }
        if (keyDataR.Count > 0)
        {
            if (keyTexR == null)
                keyTexR = new Texture2D(1, 1);

            keyDataR.TryDequeue(out dataKEYR);
            keyTexR.LoadImage(dataKEYR);
            keyTexR.Apply();
            keyR_01.GetComponent<Renderer>().material.mainTexture = keyTexR;
        }
        if(textL != "none")
        {
            ehiTextL.GetComponent<TextMeshPro>().text = textL;
        }
        if (textR != "none")
        {
            ehiTextR.GetComponent<TextMeshPro>().text = textR;
        }
        // if(dataqueueKEY.Count > 0){
        //     if(texKEY == null)
        //         texKEY = new Texture2D(1, 1);
            
        //     dataqueueKEY.TryDequeue(out dataKEY);
        //     texKEY.LoadImage(dataKEY);
        //     texKEY.Apply();
        //     KEY.GetComponent<Renderer>().material.mainTexture = texKEY;
        // }
    }
}
