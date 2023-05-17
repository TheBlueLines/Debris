using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Debris
{
	public class Client
	{
		public Handle handle = new();
		private TcpClient client;
		private List<byte> list = new();
		public Client(string ip = "127.0.0.1", int port = 12345)
		{
			client = new TcpClient(ip, port);
			Task task = new(GetPacket);
			task.Start();
		}
		public bool EnableEncryption(byte[] key)
		{
			return Encryption.keys.TryAdd(client.GetStream().Socket, key);
		}
		public void Disconnect()
		{
			client.Close();
			client.Dispose();
		}
		public void SendPacket(Packet packet)
		{
			NetworkStream stream = client.GetStream();
			byte[] req = Engine.Serialize(packet, stream.Socket);
			stream.Write(req);
		}
		private void DoWork(NetworkStream stream)
		{
			while (list.Count > 0)
			{
				int? nzx = Helper.CheckLength(list.ToArray(), stream.Socket);
				if (nzx != null && nzx.Value > 0 && list.Count >= nzx)
				{
					Packet resp = Engine.Deserialize(list.GetRange(0, nzx.Value).ToArray(), stream.Socket);
					list.RemoveRange(0, nzx.Value);
					Packet req = handle.Message(resp, stream);
					if (req != null)
					{
						SendPacket(req);
					}
				}
				else
				{
					break;
				}
			}
		}
		private void GetPacket()
		{
			NetworkStream stream = client.GetStream();
			byte[] bytes = new byte[ushort.MaxValue];
			int i;
			while (true)
			{
				try
				{
					if (stream.DataAvailable)
					{
						i = stream.Read(bytes, 0, bytes.Length);
						if (i > 0)
						{
							list.AddRange(bytes[..i]);
							DoWork(stream);
						}
					}
				}
				catch (Exception e)
				{
					Debug.Error("Exception: " + e);
					Disconnect();
				}
			}
		}
	}
}