using System.Collections.Generic;
using System.Net.Sockets;

namespace Debris
{
    public class Client
    {
        public static Packet[] GetPackets()
        {
            Packet[] tmp = packets.ToArray();
            packets.Clear();
            return tmp;
        }
        private static TcpClient client = null;
        private static List<Packet> packets = new();
        public static void Connect(string ip = "127.0.0.1", int port = 12345)
        {
            client = new TcpClient(ip, port);
        }
        public static Packet SendPacket(Packet packet)
        {
            if (client == null)
            {
                Connect();
            }
            NetworkStream stream = client.GetStream();
            List<byte> temp = new();
            List<byte> data = new();
            byte[] req = Engine.Serialize(packet, stream.Socket);
            stream.Write(req);
            byte[] bytes = new byte[ushort.MaxValue];
            bool done = false;
            int length = 0;
            int id = -1;
            while (!done)
            {
                int i = stream.Read(bytes, 0, bytes.Length);
                if (i != 0)
                {
                    for (int c = 0; c < i; c++)
                    {
                        temp.Add(bytes[c]);
                        data.Add(bytes[c]);
                        if (id == -1)
                        {
                            if (length == 0)
                            {
                                if (bytes[c] <= 127)
                                {
                                    length = VarintBitConverter.ToInt32(data.ToArray());
                                    data.Clear();
                                }
                            }
                            else
                            {
                                if (bytes[c] <= 127)
                                {
                                    id = VarintBitConverter.ToInt32(data.ToArray());
                                    data.Clear();
                                }
                            }
                        }
                    }
                    if (temp.Count >= (length + VarintBitConverter.GetVarintBytes(length).Length))
                    {
                        done = true;
                    }
                }
            }
            Packet resp = Engine.Deserialize(temp.ToArray(), stream.Socket);
            packets.Add(resp);
            return resp;
        }
    }
}