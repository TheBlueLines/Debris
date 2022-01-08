using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Debris
{
    public class Packet
    {
        public int length { get; set; }
        public int id { get; set; }
        public byte[] data { get; set; }
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
        [DllImport("KERNEL32.DLL", EntryPoint = "RtlZeroMemory")]
        private static extern bool ZeroMemory(IntPtr Destination, int Length);
        public static void FileEncrypt(string inputFile, string password)
        {
            byte[] salt = new byte[32];
            FileStream fsCrypt = new(inputFile + ".cosy", FileMode.Create);
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            RijndaelManaged AES = new();
            AES.KeySize = 256;
            AES.BlockSize = 128;
            AES.Padding = PaddingMode.PKCS7;
            var key = new Rfc2898DeriveBytes(passwordBytes, salt, 50000);
            AES.Key = key.GetBytes(AES.KeySize / 8);
            AES.IV = key.GetBytes(AES.BlockSize / 8);
            AES.Mode = CipherMode.CFB;
            fsCrypt.Write(salt, 0, salt.Length);
            CryptoStream cs = new(fsCrypt, AES.CreateEncryptor(), CryptoStreamMode.Write);
            FileStream fsIn = new(inputFile, FileMode.Open);
            byte[] buffer = new byte[1048576];
            int read;
            try
            {
                while ((read = fsIn.Read(buffer, 0, buffer.Length)) > 0)
                {
                    cs.Write(buffer, 0, read);
                }
                fsIn.Close();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: {0}", ex.Message);
            }
            finally
            {
                cs.Close();
                fsCrypt.Close();
            }
        }
        public static void FileDecrypt(string inputFile, string outputFile, string password)
        {
            byte[] salt = new byte[32];
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            FileStream fsCrypt = new(inputFile, FileMode.Open);
            fsCrypt.Read(salt, 0, salt.Length);
            RijndaelManaged AES = new();
            AES.KeySize = 256;
            AES.BlockSize = 128;
            var key = new Rfc2898DeriveBytes(passwordBytes, salt, 50000);
            AES.Key = key.GetBytes(AES.KeySize / 8);
            AES.IV = key.GetBytes(AES.BlockSize / 8);
            AES.Padding = PaddingMode.PKCS7;
            AES.Mode = CipherMode.CFB;
            CryptoStream cs = new(fsCrypt, AES.CreateDecryptor(), CryptoStreamMode.Read);
            FileStream fsOut = new(outputFile, FileMode.Create);
            int read;
            byte[] buffer = new byte[1048576];
            try
            {
                while ((read = cs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    fsOut.Write(buffer, 0, read);
                }
            }
            catch (CryptographicException ex_CryptographicException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("CryptographicException error: {0}", ex_CryptographicException.Message);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: {0}", ex.Message);
            }
            try
            {
                cs.Close();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error by closing CryptoStream: {0}", ex.Message);
            }
            finally
            {
                fsOut.Close();
                fsCrypt.Close();
            }
        }
    }
    public class Engine
    {
        public virtual byte[] Reply()
        {
            return new byte[1];
        }
        public static void InsertDate()
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("[" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second + "] ");
        }
        public static string CreateRandomPassword(int length = 100)
        {
            string validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            Random random = new();
            char[] chars = new char[length];
            for (int i = 0; i < length; i++)
            {
                chars[i] = validChars[random.Next(0, validChars.Length)];
            }
            return new string(chars);
        }
        public static void SendPacket(NetworkStream stream, Packet packet)
        {
            stream.Write(Serialize(packet, stream.Socket));
        }
        public static Packet Deserialize(byte[] value, Socket socket)
        {
            int step = 0;
            List<byte> tmp = new();
            Packet packet = new Packet();
            foreach (byte b in value)
            {
                tmp.Add(b);
                if (step == 0)
                {
                    if (b <= 127)
                    {
                        packet.length = VarintBitConverter.ToInt32(tmp.ToArray());
                        tmp.Clear();
                        step++;
                    }
                }
                else if (step == 1)
                {
                    if (b <= 127)
                    {
                        packet.id = VarintBitConverter.ToInt32(tmp.ToArray());
                        tmp.Clear();
                        step++;
                    }
                }
            }
            packet.data = tmp.ToArray();
            if (Encryption.keys.ContainsKey(socket))
            {
                Encryption.aes.Key = Encryption.keys[socket];
                packet.data = Encryption.aes.DecryptCfb(tmp.ToArray(), Encryption.aes.Key);
            }
            return packet;
        }
        public static byte[] Serialize(Packet value, Socket socket)
        {
            byte[] id = VarintBitConverter.GetVarintBytes(value.id);
            byte[] lenght = VarintBitConverter.GetVarintBytes(id.Length + value.data.Length);
            if (Encryption.keys.ContainsKey(socket))
            {
                Encryption.aes.Key = Encryption.keys[socket];
                return Combine(lenght, id, Encryption.aes.EncryptCfb(value.data, Encryption.aes.Key));
            }
            return Combine(lenght, id, value.data);
        }
        public static byte[] SerializeUshortArray(ushort[] array)
        {
            List<byte[]> list = new();
            foreach (ushort value in array)
            {
                list.Add(BitConverter.GetBytes(value));
            }
            return Combine(list.ToArray());
        }
        public static ushort[] DeserializeUshortArray(byte[] array)
        {
            List<ushort> list = new();
            List<byte> tmp = new();
            foreach (byte b in array)
            {
                tmp.Add(b);
                if (tmp.Count >= 2)
                {
                    list.Add(BitConverter.ToUInt16(tmp.ToArray()));
                    tmp.Clear();
                }
            }
            return list.ToArray();
        }
        public static byte[] SerializeStringArray(string[] array)
        {
            List<byte> list = new();
            foreach (string str in array)
            {
                list.AddRange(VarintBitConverter.GetVarintBytes(str.Length));
                list.AddRange(Encoding.UTF8.GetBytes(str));
            }
            return Combine(list.ToArray());
        }
        public static string[] DeserializeStringArray(byte[] array)
        {
            List<string> list = new();
            List<byte> temp = new();
            int length = 0;
            foreach (byte b in array)
            {
                temp.Add(b);
                if (length == 0)
                {
                    if (b <= 127)
                    {
                        length = VarintBitConverter.ToInt32(temp.ToArray());
                        temp.Clear();
                    }
                }
                else
                {
                    if (--length == 0)
                    {
                        list.Add(Encoding.UTF8.GetString(temp.ToArray()));
                        temp.Clear();
                    }
                }
            }
            return list.ToArray();
        }
        public static string GetString(byte[] data)
        {
            List<byte> lengthx = new();
            List<byte> datax = new();
            int length = 0;
            foreach (byte b in data)
            {
                if (length != 0)
                {
                    datax.Add(b);
                    if (--length == 0)
                    {
                        break;
                    }
                }
                else
                {
                    lengthx.Add(b);
                    if (b <= 127)
                    {
                        length = VarintBitConverter.ToInt32(lengthx.ToArray());
                    }
                }
            }
            return Encoding.UTF8.GetString(datax.ToArray());
        }
        public static byte[] Combine(params byte[][] arrays)
        {
            byte[] rv = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }
            return rv;
        }
        public static void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];
            int cnt;
            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }
        public static byte[] Zip(byte[] data)
        {
            using (var msi = new MemoryStream(data))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    CopyTo(msi, gs);
                }
                return mso.ToArray();
            }
        }
        public static byte[] Unzip(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    CopyTo(gs, mso);
                }
                return mso.ToArray();
            }
        }
    }
    public class Debug
    {
        public static void Print(string text, ConsoleColor color = ConsoleColor.Gray)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ResetColor();
        }
        public static void Log(string text)
        {
            Print(text, ConsoleColor.Gray);
        }
        public static void Warn(string text)
        {
            Print(text, ConsoleColor.Yellow);
        }
        public static void Error(string text)
        {
            Print(text, ConsoleColor.Red);
        }
        public static void OK(string text)
        {
            Print(text, ConsoleColor.Green);
        }
        public static void Info(string text)
        {
            Print(text, ConsoleColor.Blue);
        }
    }
}
