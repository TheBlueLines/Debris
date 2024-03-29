﻿using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using TTMC.Tools;

namespace TTMC.Debris
{
	public class Server
	{
		public Handle handle = new();
		internal bool run = true;
		public Server(int port = 12345, Handle handle = null)
		{
			if (handle != null)
			{
				this.handle = handle;
			}
			Thread t = new Thread(delegate ()
			{
				Listener listener = new Listener(IPAddress.Any, port, this);
			});
			t.Start();
		}
		public void StopServer()
		{
			run = false;
		}
	}
	internal class Listener
	{
		private Server server;
		private List<byte> list = new();
		internal TcpListener listener = null;
		internal Listener(IPAddress ip, int port, Server server)
		{
			this.server = server;
			listener = new TcpListener(ip, port);
			listener.Start();
			StartListener();
		}
		private void StartListener()
		{
			try
			{
				while (server.run)
				{
					if (listener.Pending())
					{
						TcpClient client = listener.AcceptTcpClient();
						Thread t = new Thread(new ParameterizedThreadStart(HandleDeivce));
						t.Start(client);
					}
				}
			}
			catch (SocketException e)
			{
				Debug.Error("SocketException: " + e);
			}
			listener.Stop();
		}
		private void HandleDeivce(object obj)
		{
			TcpClient client = obj as TcpClient;
			var stream = client.GetStream();
			byte[] bytes = new byte[ushort.MaxValue];
			try
			{
				while (server.run)
				{
					if (stream.DataAvailable)
					{
						int i = stream.Read(bytes, 0, bytes.Length);
						if (i != 0)
						{
							list.AddRange(bytes[..i]);
							DoWork(stream);
						}
					}
				}
				listener.Stop();
			}
			catch { }
		}
		private void DoWork(NetworkStream stream)
		{
			try
			{
				while (list.Count > 0)
				{
					int? nzx = Helper.CheckLength(list.ToArray(), stream.Socket);
					if (nzx != null && nzx > 0 && list.Count >= nzx)
					{
						List<byte> ttmc = new();
						ttmc.AddRange(list.ToArray()[..(int)nzx]);
						list.RemoveRange(0, (int)nzx);
						Packet req = Packet.Deserialize(ttmc.ToArray(), stream.Socket);
						Packet resp = server.handle.Message(req, stream);
						if (resp != null)
						{
							Packet.SendPacket(stream, resp);
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