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
        public SnooperChannel(string Name)
        {
            ChannelName = Name;
        }
        public readonly string ChannelName;
        public delegate void ChannelMessageReceived(SnooperMessage message);
        public bool TryRaise(SnooperMessage Message)
        {
            if (MessageReceived == null) return false;
            MessageReceived.Invoke(Message);
            return true;
        }
    }
}
