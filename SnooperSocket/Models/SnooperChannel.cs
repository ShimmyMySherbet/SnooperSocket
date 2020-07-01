using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooperSocket.Models
{
    public class SnooperChannel
    {
        public event ChannelMessageReceived MessageReceived;
        public event ChannelRequestRecevied RequestReceived;
        public SnooperChannel(string Name)
        {
            ChannelName = Name;
        }
        public readonly string ChannelName;
        public delegate void ChannelMessageReceived(SnooperMessage message);
        public delegate object ChannelRequestRecevied(SnooperMessage Request);
        public bool TryRaise(SnooperMessage Message)
        {
            if (MessageReceived == null) return false;
            MessageReceived.Invoke(Message);
            return true;
        }

        public object TryRaiseRequest(SnooperMessage Message)
        {
            if (RequestReceived == null) return null;
            return RequestReceived.Invoke(Message);
        }
    }
}
