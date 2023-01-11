using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Debris
{
    public class Server
    {
        public Handle handle = new();
        private Listener listener = null;
        public Server(int port = 12345)
        {
            Thread t = new Thread(delegate ()
            {
                listener = new Listener(IPAddress.Any, port, handle);
            });
            t.Start();
        }
        public void StopServer()
        {
            listener.listener.Stop();
        }
    }
    public class Listener
    {
        private Handle handle = new();
        private List<byte> list = new();
        internal TcpListener listener = null;
        internal Listener(IPAddress ip, int port, Handle hndl)
        {
            handle = hndl;
            listener = new TcpListener(ip, port);
            listener.Start();
            StartListener();
        }
        private void StartListener()
        {
            try
            {
                while (true)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    Thread t = new Thread(new ParameterizedThreadStart(HandleDeivce));
                    t.Start(client);
                }
            }
            catch (SocketException e)
            {
                Debug.Error("SocketException: " + e);
                listener.Stop();
            }
        }
        private void HandleDeivce(object obj)
        {
            TcpClient client = (TcpClient)obj;
            var stream = client.GetStream();
            handle.socket = stream.Socket;
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
        private void DoWork(NetworkStream stream)
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
                        Packet resp = handle.Message(req, stream);
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