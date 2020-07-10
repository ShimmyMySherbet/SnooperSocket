using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SnooperSocket.Models
{
    public class SnooperChannelStack : IEnumerable<SnooperChannel>
    {
        private List<SnooperChannel> Channels = new List<SnooperChannel>();
        private SnooperSocketClient SnooperSocket;
        public SnooperChannelStack(SnooperSocketClient Client)
        {
            SnooperSocket = Client;
        }
        public SnooperChannel this[string ChannelName] {
        get
            {
                foreach(SnooperChannel Channel in Channels)
                {
                    if (Channel.ChannelName.ToLower() == ChannelName.ToLower()) return Channel;
                }
                SnooperChannel NewChannel = new SnooperChannel(ChannelName);
                NewChannel.Socket = SnooperSocket;
                Channels.Add(NewChannel);
                return NewChannel;
            }
        }

        public void CreateInstanceChannel(string Name)
        {
            Channels.Add(new SnooperChannel(Name) { Socket = SnooperSocket });
        }

        public IEnumerator<SnooperChannel> GetEnumerator()
        {
            return Channels.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Channels.GetEnumerator();
        }
    }
}
