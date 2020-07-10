using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooperSocket.Models
{
    public class SnooperSocketClientPool<T> : IEnumerable<SnooperSocketClient>
    {
        private Dictionary<SnooperSocketClient, T> _Clients = new Dictionary<SnooperSocketClient, T>();

        public readonly SnooperPoolChannelStack Stack = new SnooperPoolChannelStack();

        public void Join(SnooperSocketClient Client, T Data)
        {
            _Clients.Add(Client, Data);
            Client.JoinPool(this);
        }

        public void Leave(SnooperSocketClient Client)
        {
            _Clients.Remove(Client);
        }

        public void SendAllRawData(byte[] Message, string Header = null)
        {
            //foreach (var Client in _Clients) Client.Key.WriteRawData(Message, Header);
        }

        public void SendAllRawData(Stream Message, string Header = null)
        {
            //foreach (var Client in _Clients) Client.Key.WriteRawData(Message, Header);
        }

        public void SendAll(object Data, Dictionary<string, string> Headers = null, string Channel = null)
        {
            foreach (var Client in _Clients) Client.Key.Write(Data, Headers, Channel);
        }

        public void SendAll(byte[] Message, Dictionary<string, string> Headers = null, string Channel = null)
        {
            foreach (var Client in _Clients) Client.Key.Write(Message, Headers, Channel);
        }

        public void SendAll(Stream Message, Dictionary<string, string> Headers = null, string Channel = null)
        {
            foreach (var Client in _Clients) Client.Key.Write(Message, Headers, Channel);
        }

        public T GetClientData(SnooperSocketClient Client)
        {
            if (_Clients.ContainsKey(Client)) return _Clients[Client];
            return default(T);
        }

        public IEnumerator<SnooperSocketClient> GetEnumerator()
        {
            return _Clients.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _Clients.Keys.GetEnumerator();
        }
    }
}
