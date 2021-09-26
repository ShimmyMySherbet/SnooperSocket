using SnooperSocket.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnooperSocket.Cryptography
{
    public abstract class SnooperSecurityProtocal
    {
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

        public virtual bool TryValidateConnection(int Timeout = 5000)
        {
            Task<bool> ValidateTask = new Task<bool>(ValidateConnection);
            ValidateTask.Start();
            int lp = 0;
            while (!(lp >= Timeout || ValidateTask.IsCompleted))
            {
                Thread.Sleep(100);
                lp += 100;
            }
            if (ValidateTask.IsCompleted) return ValidateTask.Result;
            else return false;
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
