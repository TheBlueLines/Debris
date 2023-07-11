using System.Collections.Generic;
using System.Net.Sockets;
using System.Security.Cryptography;
using TTMC.Tools;

namespace TTMC.Debris
{
	public class Handle
	{
		public virtual Packet Message(Packet packet, NetworkStream stream) { return null; }
	}
	public class Encryption
	{
		public static Dictionary<Socket, byte[]> keys = new();
		public static Aes aes = Aes.Create();
		public static RSA rsa = RSA.Create();
		public static byte[] Encrypt(byte[] data, byte[] key)
		{
			aes.Key = key;
			return aes.EncryptCfb(data, key);
		}
		public static byte[] Decrypt(byte[] data, byte[] key)
		{
			aes.Key = key;
			return aes.DecryptCfb(data, key);
		}
	}
	public class Packet
	{
		public int length { get; set; }
		public int id { get; set; }
		public byte[] data { get; set; }
		public static void SendPacket(NetworkStream stream, Packet packet)
		{
			stream.Write(Serialize(packet, stream.Socket));
		}
		public static Packet Deserialize(byte[] value, Socket socket)
		{
			if (Encryption.keys.ContainsKey(socket))
			{
				Encryption.aes.Key = Encryption.keys[socket];
				value = Encryption.aes.DecryptCfb(value, Encryption.aes.Key);
			}
			int length = VarintBitConverter.ToInt32(value);
			byte[] lengthx = VarintBitConverter.GetVarintBytes(length);
			int id = VarintBitConverter.ToInt32(value[lengthx.Length..]);
			byte[] idx = VarintBitConverter.GetVarintBytes(id);
			byte[] data = value[(lengthx.Length + idx.Length)..];
			return new Packet()
			{
				length = length,
				id = id,
				data = data,
			};
		}
		public static byte[] Serialize(Packet value, Socket socket)
		{
			byte[] id = VarintBitConverter.GetVarintBytes(value.id);
			byte[] lenght = VarintBitConverter.GetVarintBytes(id.Length + value.data.Length);
			if (Encryption.keys.ContainsKey(socket))
			{
				Encryption.aes.Key = Encryption.keys[socket];
				return Encryption.aes.EncryptCfb(Engine.Combine(lenght, id, value.data), Encryption.aes.Key);
			}
			return Engine.Combine(lenght, id, value.data);
		}
	}
}