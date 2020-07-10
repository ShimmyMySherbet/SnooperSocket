using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooperSocket.Models
{
    public class SnooperSocketClientPool
    {
        private List<SnooperSocketClient> _Clients = new List<SnooperSocketClient>();

        public readonly SnooperPoolChannelStack Stack = new SnooperPoolChannelStack();

        public void SendAllRawData(byte[] Message, string Header = null)
        {
            //foreach (SnooperSocketClient Client in _Clients) Client.WriteRawData(Message, Header);
        }

        public void SendAllRawData(Stream Message, string Header = null)
        {
            //foreach (SnooperSocketClient Client in _Clients) Client.WriteRawData(Message, Header);
        }

        public void SendAll(object Data, Dictionary<string, string> Headers = null, string Channel = null)
        {
            foreach (SnooperSocketClient Client in _Clients) Client.Write(Data, Headers, Channel);
        }

        public void SendAll(byte[] Message, Dictionary<string, string> Headers = null, string Channel = null)
        {
            foreach (SnooperSocketClient Client in _Clients) Client.Write(Message, Headers, Channel);
        }

        public void SendAll(Stream Message, Dictionary<string, string> Headers = null, string Channel = null)
        {
            foreach (SnooperSocketClient Client in _Clients) Client.Write(Message, Headers, Channel);
        }

    }
}
