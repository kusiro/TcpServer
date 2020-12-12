using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImageStreamServer
{
    public class Client
    {
        public TcpClient client;
        public BinaryWriter writer;



        public Client(TcpClient aClient)
        {
            client = aClient;
            writer = new BinaryWriter(client.GetStream());
        }

        // 傳輸資料
        public bool SendImageData(byte[] aData)
        {
            if (!client.Connected)
                return false;
            writer.Write(aData.Length);
            writer.Write(aData);
            return true;
        }
    }


    class Program
    {
        TcpListener m_Server;
        bool m_ServerRunning;
        bool IsClient = false;
        Thread m_ListenThread;
        NetworkStream stream;
        int MachineNum;

        Thread m_SendingThread;
        List<Client> m_Clients = new List<Client>();

        static void Main(string[] args)
        {
            var prg = new Program();
            prg.Run();
        }

        void ListenThread()
        {
            MachineNum = 0;
            m_Server = new TcpListener(IPAddress.Any, 5566);
            m_Server.Start();
            m_ServerRunning = true;
            Socket mySocket = m_Server.AcceptSocket();
            // Get Client
            while (m_ServerRunning)
            {
                try
                {
                    var newClient = m_Server.AcceptTcpClient();
                    lock (m_Clients)
                    {
                        m_Clients.Add(new Client(newClient));
                        if(m_Clients.Count != 0)
                        {
                            IsClient = true;
                        }
                    }
                    while(m_Clients.Count != 0)
                    {
                        int bufferSize = newClient.ReceiveBufferSize;
                        byte[] ClientDataByte = new byte[bufferSize];
                        stream = newClient.GetStream();
                        stream.Read(ClientDataByte, 0, bufferSize);

                        MachineNum = Int32.Parse(Encoding.ASCII.GetString(ClientDataByte, 0, bufferSize));
                        Console.WriteLine("機台編號: " + MachineNum);

                        if (MachineNum != 0)
                        {
                            m_SendingThread = new Thread(new ThreadStart(SendThread));
                            m_SendingThread.Start();
                        }
                    }
                }
                catch (Exception e)
                {
                    //Console.Write(e.Message + "\n");
                }

                //Client[] clients;
                //lock (m_Clients)
                //{
                //    clients = m_Clients.ToArray();
                //}
                //foreach(var client in clients)
                //{
                //    int bufferSize = client.ReceiveBufferSize;
                //    byte[] ClientDataByte = new byte[bufferSize];
                //    stream = client.GetStream();
                //    stream.Read(ClientDataByte, 0, bufferSize);
                //}
            }
            lock (m_Clients)
            {
                foreach (var c in m_Clients)
                {
                    try
                    {
                        c.client.Close();
                    }
                    catch
                    {
                    }
                }
                m_Clients.Clear();
            }

            // Receieve Data
        }

        class FileItem
        {
            public string name;
            public byte[] data;
        }

        void SendThread()
        {
            DirectoryInfo folder;
            var files = new List<FileItem>();
            for(int i = 0; i < 2; i++)
            {
                if(i == 0)
                    folder = new DirectoryInfo("C:\\Users\\coffee\\Desktop\\TcpServer\\Machine Client\\M00" + MachineNum + "L");
                else
                    folder = new DirectoryInfo("C:\\Users\\coffee\\Desktop\\TcpServer\\Machine Client\\M00" + MachineNum + "R");

                var fileNames = folder.GetFiles("*.png");
                foreach (var fn in fileNames)
                {
                    files.Add(new FileItem
                    {
                        data = File.ReadAllBytes(fn.FullName),
                        name = fn.FullName
                    });
                }
            }

            while (true)
            {
                if (IsClient)
                {
                    int counter = 0;
                    foreach (var file in files)
                    {
                        string ehi = file.name.Split("\\")[7].Split(".")[0];
                        
                        Client[] clients;
                        lock (m_Clients)
                        {
                            clients = m_Clients.ToArray();
                        }
                        foreach (var client in clients)
                        {
                            bool success = false;
                            try
                            {
                                success = client.SendImageData(file.data);
                                Console.WriteLine("Sending File: " + ehi + ".png");
                                Thread.Sleep(200);
                                //Console.WriteLine(counter);
                                //    if(counter % 2 == 0)
                                //    {
                                //        success = client.SendImageData(Encoding.ASCII.GetBytes(ehi));
                                //        Console.WriteLine("Sending File: " + ehi + ".png");
                                //        Thread.Sleep(200);
                                //        //success = client.SendImageData(file.data);
                                //        //Thread.Sleep(200);
                                //    }
                                //    else
                                //    {
                                //        success = client.SendImageData(file.data);
                                //        Console.WriteLine("Sending File: " + ehi + ".png");
                                //        Thread.Sleep(200);
                                //    }
                            }
                            catch
                            {
                                //Console.WriteLine(counter);
                                success = false;
                                client.client.Close();
                            }
                            finally
                            {
                                if (!success)
                                {
                                    lock (m_Clients)
                                    {
                                        Console.WriteLine("Remove Client");
                                        m_Clients.Remove(client);
                                    }
                                }
                            }
                        }

                        counter++;
                    }
                    Console.WriteLine("\n\n");
                    m_SendingThread.Join();
                }
            }
        }
        private void Run()
        {
            m_ListenThread = new Thread(new ThreadStart(ListenThread));
            m_ListenThread.Start();
            

            Console.WriteLine("Server start");
            //m_ServerRunning = false;
            //m_Server.Stop();
            //m_ListenThread.Join();
            //m_SendingThread.Join();
        }
    }
}