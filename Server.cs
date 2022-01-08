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
        public virtual Packet Reply(Packet packet)
        {
            return packet;
        }
    }
    public class Server
    {
        public static Handle handle = new();
        private static List<Packet> packets = new();
        private static TcpListener server = null;
        public static Packet[] GetPackets()
        {
            Packet[] tmp = packets.ToArray();
            packets.Clear();
            return tmp;
        }
        public static Task StartServer(int port = 12345)
        {
            Thread t = new Thread(delegate ()
            {
                Server myserver = new Server(IPAddress.Any, port);
            });
            t.Start();
            Debug.Info("Debris Server Started!");
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
        private static void HandleDeivce(Object obj)
        {
            TcpClient client = (TcpClient)obj;
            var stream = client.GetStream();
            byte[] bytes = new byte[ushort.MaxValue];
            int i;
            try
            {
                while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    List<byte> list = new List<byte>();
                    for (int x = 0; x < i; x++)
                    {
                        list.Add(bytes[x]);
                    }
                    Packet req = Engine.Deserialize(list.ToArray(), stream.Socket);
                    packets.Add(req);
                    Packet resp = handle.Reply(req);
                    Engine.SendPacket(stream, resp);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.ToString());
                client.Close();
            }
        }
    }
}