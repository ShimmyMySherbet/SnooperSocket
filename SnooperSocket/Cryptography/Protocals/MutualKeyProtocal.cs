using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SnooperSocket.Cryptography.Protocals
{
    public class MutualKeyProtocal : SnooperSecurityProtocal
    {
        public string Key;

        private byte[] KeyBytes
        {
            get
            {
                return Encoding.UTF8.GetBytes(Key);
            }
        }

        public override void Init()
        {
            Channels["$SnooperSec.MutualKeyProtocal.Validate"].RequestReceived += Validate_Message;
        }

        public override bool ValidateConnection()
        {
            byte[] Data = new byte[16];
            using (RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider())
                provider.GetBytes(Data);
            string q = string.Join("", Data);
            MemoryStream Output;
            MemoryStream Input = new MemoryStream(Encoding.UTF8.GetBytes(q));
            string SaltString = CryptographicProvider.GetCryptographicallySecureString(32);
            byte[] SaltBytes = Encoding.UTF8.GetBytes(SaltString);
            byte[] BKey = new byte[32];
            byte[] IV = new byte[16];
            using (RNGCryptoServiceProvider Provider = new RNGCryptoServiceProvider())
            {
                Provider.GetBytes(IV);
            }
            string IVStr = Convert.ToBase64String(IV);
            using (SHA256CryptoServiceProvider SHA = new SHA256CryptoServiceProvider())
            {
                byte[] tmp = SHA.ComputeHash(KeyBytes.Concat(SaltBytes).ToArray());
                Array.Copy(tmp, 0, BKey, 0, 16);
                Array.Copy(tmp, 0, BKey, 15, 16);
            }
            using (AesCryptoServiceProvider AES = new AesCryptoServiceProvider())
            {
                AES.Mode = CipherMode.ECB;
                AES.KeySize = 128;
                using (ICryptoTransform Encryptor = AES.CreateEncryptor(BKey, IV))
                {
                    Output = new MemoryStream();
                    CryptoStream Crypto = new CryptoStream(Output, Encryptor, CryptoStreamMode.Write);
                    Input.Position = 0;
                    Input.CopyTo(Crypto);
                    Crypto.FlushFinalBlock();
                }
            }
            string MyToken = Convert.ToBase64String(Output.ToArray());
            MutualKeyProtocalValidationRequest MSG = new MutualKeyProtocalValidationRequest()
            {
                IV = IVStr,
                Salt = SaltString,
                RawToken = q
            };
            MutualKeyProtocalValidationResponse resp = Channels["$SnooperSec.MutualKeyProtocal.Validate"].Query<MutualKeyProtocalValidationResponse>(MSG);
            return resp.Token == MyToken;
        }


        private object Validate_Message(Models.SnooperMessage Request)
        {
            MutualKeyProtocalValidationRequest RQ = Request.ReadObject<MutualKeyProtocalValidationRequest>();
            MemoryStream Output;
            MemoryStream Input = new MemoryStream(Encoding.UTF8.GetBytes(RQ.RawToken));
            string SaltString = RQ.Salt;
            byte[] SaltBytes = Encoding.UTF8.GetBytes(SaltString);
            byte[] BKey = new byte[32];
            byte[] IV = Convert.FromBase64String(RQ.IV);
            using (SHA256CryptoServiceProvider SHA = new SHA256CryptoServiceProvider())
            {
                byte[] tmp = SHA.ComputeHash(KeyBytes.Concat(SaltBytes).ToArray());
                Array.Copy(tmp, 0, BKey, 0, 16);
                Array.Copy(tmp, 0, BKey, 15, 16);
            }
            using (AesCryptoServiceProvider AES = new AesCryptoServiceProvider())
            {
                AES.Mode = CipherMode.ECB;
                AES.KeySize = 128;
                using (ICryptoTransform Encryptor = AES.CreateEncryptor(BKey, IV))
                {
                    Output = new MemoryStream();
                    CryptoStream Crypto = new CryptoStream(Output, Encryptor, CryptoStreamMode.Write);
                    Input.Position = 0;
                    Input.CopyTo(Crypto);
                    Crypto.FlushFinalBlock();
                }
            }
            string Token = Convert.ToBase64String(Output.ToArray());
            return new MutualKeyProtocalValidationResponse() { Token = Token };
        }

        public override bool EncryptStream(MemoryStream Input, out MemoryStream Output, ref Dictionary<string, string> Headers)
        {
            if (Headers.ContainsKey("$Channel") && Headers["$Channel"] == "$SnooperSec.MutualKeyProtocal.Validate")
            {
                return base.EncryptStream(Input, out Output, ref Headers);
            }
            try
            {
                string SaltString = CryptographicProvider.GetCryptographicallySecureString(32);
                byte[] SaltBytes = Encoding.UTF8.GetBytes(SaltString);
                byte[] BKey = new byte[32];
                byte[] IV = new byte[16];
                using (RNGCryptoServiceProvider Provider = new RNGCryptoServiceProvider())
                {
                    Provider.GetBytes(IV);
                }
                string IVStr = Convert.ToBase64String(IV);
                using (SHA256CryptoServiceProvider SHA = new SHA256CryptoServiceProvider())
                {
                    byte[] tmp = SHA.ComputeHash(KeyBytes.Concat(SaltBytes).ToArray());
                    Array.Copy(tmp, 0, BKey, 0, 16);
                    Array.Copy(tmp, 0, BKey, 15, 16);
                }
                using (AesCryptoServiceProvider AES = new AesCryptoServiceProvider())
                {
                    AES.Mode = CipherMode.ECB;
                    AES.KeySize = 128;
                    using (ICryptoTransform Encryptor = AES.CreateEncryptor(BKey, IV))
                    {
                        Output = new MemoryStream();
                        CryptoStream Crypto = new CryptoStream(Output, Encryptor, CryptoStreamMode.Write);
                        Input.Position = 0;
                        Input.CopyTo(Crypto);
                        Crypto.FlushFinalBlock();
                    }
                }
                Headers.Add("$ENCMODE", GetType().Name);
                Headers.Add("$ENCSALT", SaltString);
                Headers.Add("$ENCIV", IVStr);
                Output.Position = 0;
                return true;
            }
            catch (CryptographicException)
            {
                Console.WriteLine("Encrypt Failiure");
                Output = null;
                return false;
            }
        }

        public override bool DecryptStream(MemoryStream Input, out MemoryStream Output, ref Dictionary<string, string> Headers)
        {
            if (Headers.ContainsKey("$Channel") && Headers["$Channel"] == "$SnooperSec.MutualKeyProtocal.Validate")
            {
                return base.DecryptStream(Input, out Output, ref Headers);
            }
            try
            {
                if (!Headers.ContainsKey("$ENCMODE") || Headers["$ENCMODE"] != GetType().Name) throw new Exception("This message does not use Mutual Key Protocal");
                string SaltString = Headers["$ENCSALT"];
                string IVString = Headers["$ENCIV"];
                byte[] IV = Convert.FromBase64String(IVString);
                byte[] BKey = new byte[32];
                using (SHA256CryptoServiceProvider SHA = new SHA256CryptoServiceProvider())
                {
                    byte[] tmp = SHA.ComputeHash(KeyBytes.Concat(Encoding.UTF8.GetBytes(SaltString)).ToArray());
                    Array.Copy(tmp, 0, BKey, 0, 16);
                    Array.Copy(tmp, 0, BKey, 15, 16);
                }
                Input.Position = 0;
                using (AesCryptoServiceProvider AES = new AesCryptoServiceProvider())
                {
                    AES.Mode = CipherMode.ECB;
                    AES.KeySize = 128;
                    using (ICryptoTransform Decryptor = AES.CreateDecryptor(BKey, IV))
                    {
                        Output = new MemoryStream();
                        CryptoStream Crypto = new CryptoStream(Output, Decryptor, CryptoStreamMode.Write);
                        Input.CopyTo(Crypto);
                        Crypto.FlushFinalBlock();
                    }
                }
                Output.Position = 0;
                return true;
            }
            catch (CryptographicException)
            {
                Console.WriteLine("Decrypt Failiure");
                Output = null;
                return false;
            }
        }
    }

    public class MutualKeyProtocalValidationRequest
    {
        public string RawToken;
        public string Salt;
        public string IV;
    }

    public class MutualKeyProtocalValidationResponse
    {
        public string Token;
    }
}