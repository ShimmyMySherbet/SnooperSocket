using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooperSocket.Cryptography
{
    public abstract class SnooperSecurityProtocal
    {
        public SnooperSocketClient Socket;
        public bool RedirectRequests = false;

        public virtual void OnConnect()
        {
        }
        public virtual void DecryptStream(MemoryStream Input, out MemoryStream Output, ref Dictionary<string, string> Headers)
        {
            Output = Input;
        }
        public virtual void EncryptStream(MemoryStream Input, out MemoryStream Output, ref Dictionary<string, string> Headers)
        {
            Output = Input;
        }
        public virtual void Init()
        {
        }
    }
}
