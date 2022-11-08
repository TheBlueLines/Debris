using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Debris
{
    public class Handle
    {
        public Socket clientSocket = null;
        public Socket serverSocket = null;
        public virtual Packet Request(Packet packet, NetworkStream stream)
        {
            return packet;
        }
        public virtual void Response(Packet packet, NetworkStream stream)
        {

        }
    }
    public class Server
    {
        private static TcpListener server = null;
        public static Task StartServer(int port = 12345, bool alert = true)
        {
            Thread t = new Thread(delegate ()
            {
                Server myserver = new Server(IPAddress.Any, port);
            });
            t.Start();
            if (alert)
            {
                Debug.Info("Debris Server Started!");
            }
            return Task.CompletedTask;
        }
        private Server(IPAddress ip, int port)
        {
            server = new TcpListener(ip, port);
            server.Start();
            StartListener();
        }
        private static void StartListener()
        {
            try
            {
                while (true)
                {
                    TcpClient client = server.AcceptTcpClient();
                    Thread t = new Thread(new ParameterizedThreadStart(HandleDeivce));
                    t.Start(client);
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
                server.Stop();
            }
        }
        private static List<byte> list = new List<byte>();
        private static void HandleDeivce(object obj)
        {
            TcpClient client = (TcpClient)obj;
            var stream = client.GetStream();
            Engine.handle.serverSocket = stream.Socket;
            byte[] bytes = new byte[ushort.MaxValue];
            int i;
            try
            {
                while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    for (int x = 0; x < i; x++)
                    {
                        list.Add(bytes[x]);
                    }
                    DoWork(stream);
                }
            }
            catch { }
        }
        private static void DoWork(NetworkStream stream)
        {
            try
            {
                while (list.Count > 0)
                {
                    int? nzx = Helper.CheckLength(list.ToArray(), stream.Socket);
                    if (nzx != null && nzx > 0)
                    {
                        List<byte> ttmc = new();
                        ttmc.AddRange(list.ToArray()[..(int)nzx]);
                        list.RemoveRange(0, (int)nzx);
                        Packet req = Engine.Deserialize(ttmc.ToArray(), stream.Socket);
                        Packet resp = Engine.handle.Request(req, stream);
                        if (resp != null)
                        {
                            Engine.SendPacket(stream, resp);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch
            {
                list.Clear();
            }
        }
    }
}