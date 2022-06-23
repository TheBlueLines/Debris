using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Debris
{
    public class Client
    {
        private static TcpClient client = null;
        public static void Connect(string ip = "127.0.0.1", int port = 12345)
        {
            client = new TcpClient(ip, port);
            Task task = new Task(() => GetPacket());
            task.Start();
        }
        public static void Disconnect()
        {
            client.Close();
            client.Dispose();
        }
        public static void SendPacket(Packet packet)
        {
            if (client == null)
            {
                Connect();
            }
            NetworkStream stream = client.GetStream();
            byte[] req = Engine.Serialize(packet, stream.Socket);
            stream.Write(req);
        }
        public static void SendRequest()
        {
            if (client == null)
            {
                Connect();
            }
            NetworkStream stream = client.GetStream();
            byte[] req = { 1, 0 };
            stream.Write(req);
        }
        private static List<byte> list = new List<byte>();
        private static void DoWork(NetworkStream stream)
        {
            while (list.Count > 0)
            {
                int? nzx = Helper.CheckLength(list.ToArray(), stream.Socket);
                if (nzx != null && nzx > 0)
                {
                    List<byte> ttmc = new();
                    ttmc.AddRange(list.ToArray()[..(int)nzx]);
                    list.RemoveRange(0, (int)nzx);
                    Packet resp = Engine.Deserialize(ttmc.ToArray(), stream.Socket);
                    Engine.handle.Response(resp);
                }
                else
                {
                    break;
                }
            }
        }
        public static void GetPacket()
        {
            NetworkStream stream = client.GetStream();
            byte[] bytes = new byte[ushort.MaxValue];
            int i;
            while (true)
            {
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
                catch (Exception e)
                {
                    Console.WriteLine("Exception: {0}", e.ToString());
                    client.Close();
                }
            }
        }
    }
}