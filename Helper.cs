using System.Net.Sockets;

namespace Debris
{
    internal class Helper
    {
        public static int? CheckLength(byte[] value, Socket socket)
        {
            if (Encryption.keys.ContainsKey(socket))
            {
                Encryption.aes.Key = Encryption.keys[socket];
                value = Encryption.aes.DecryptCfb(value, Encryption.aes.Key);
            }
            int length = VarintBitConverter.ToInt32(value);
            byte[] lengthx = VarintBitConverter.GetVarintBytes(length);
            return length + lengthx.Length;
        }
    }
}