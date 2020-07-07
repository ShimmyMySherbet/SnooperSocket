using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooperSocket.Models
{
    public class SnooperPoolChannel
    {
        public event PoolChannelMessageReceived MessageReceived;

        public SnooperPoolChannel(string Name)
        {
            ChannelName = Name;
        }
        public readonly string ChannelName;
        public delegate void PoolChannelMessageReceived(SnooperMessage message, SnooperSocketClient Client);
        public bool TryRaise(SnooperMessage Message, SnooperSocketClient Client)
        {
            if (MessageReceived == null) return false;
            MessageReceived.Invoke(Message, Client);
            return true;
        }
    }
}
