using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SnooperSocket.Cryptography
{
    public class CryptographicProvider
    {
        public string GetSecureString(int length)
        {
            string Indexed = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM1234567890";
            byte[] Buffer = RetriveCrypographicBytes(length);
            byte[] Adjusted = GetAdjustedBytes(Buffer, Indexed.Length);
            return RetriveCryptographicString(Adjusted, Indexed);
        }
        public static string GetCryptographicallySecureString(int length)
        {
            CryptographicProvider P = new CryptographicProvider();
            return P.GetSecureString(length);
        }

        public byte[] RetriveCrypographicBytes(int count)
        {
            RNGCryptoServiceProvider RNG = new RNGCryptoServiceProvider();
            byte[] Buffer = new byte[count];
            RNG.GetBytes(Buffer);
            return Buffer;
        }

        public byte[] GetAdjustedBytes(byte[] Buffer, int max)
        {
            List<byte> ext = new List<byte>();
            foreach (byte _b in Buffer)
            {
                byte b = _b;
                do
                {
                    b = (byte)(b - max);
                } while ((b >= max));
                ext.Add(b);
            }
            return ext.ToArray();
        }
        public string RetriveCryptographicString(byte[] Buffer, string Indexed)
        {
            byte[] Adjusted = GetAdjustedBytes(Buffer, Indexed.Count() - 1);
            string result = "";
            foreach (int i in Adjusted)
            {
                result += Indexed[i];
            }
            return result;
        }
    }
}
