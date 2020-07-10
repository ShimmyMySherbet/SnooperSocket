using SnooperSocket.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SnooperSocket.Cryptography
{
    public abstract class SnooperSecurityProtocal
    {
        //public delegate void RedirectRequestedArgs(SnooperChannelStack Redirect);
        //public event RedirectRequestedArgs RedirectRequested;
        public SnooperSocketClient Socket;
        protected SnooperChannelStack Channels;
        private bool _Redirect = false;
        public bool RedirectRequests
        {
            get
            {
                return _Redirect;
            }
            protected set
            {
                _Redirect = value;
                //if (value != _Redirect)
                //{
                //    if (value)
                //    {
                //        Channels = new SnooperChannelStack(Socket);
                //        //RedirectRequested?.Invoke(Channels);
                //    }
                //}
            }
        }

        public void SetChannelOverrides(SnooperChannelStack Channels)
        {
            this.Channels = Channels;
        }

        public virtual bool DecryptStream(MemoryStream Input, out MemoryStream Output, ref Dictionary<string, string> Headers)
        {
            Output = Input;
            return true;
        }

        public virtual bool ValidateConnection()
        {
            return true;
        }
        public virtual bool EncryptStream(MemoryStream Input, out MemoryStream Output, ref Dictionary<string, string> Headers)
        {
            Output = Input;
            return true;
        }

        public virtual void OnReady()
        {

        }

        public virtual void Init()
        {
            Socket?.InvokeAuthorisation();
        }
    }
}
