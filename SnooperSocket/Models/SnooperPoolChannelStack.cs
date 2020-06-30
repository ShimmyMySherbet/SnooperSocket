using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooperSocket.Models
{
    public class SnooperPoolChannelStack : IEnumerable<SnooperPoolChannel>
    {
        private List<SnooperPoolChannel> Channels = new List<SnooperPoolChannel>();
        public SnooperPoolChannel this[string ChannelName]
        {
            get
            {
                foreach (SnooperPoolChannel Channel in Channels)
                {
                    if (Channel.ChannelName.ToLower() == ChannelName.ToLower()) return Channel;
                }
                SnooperPoolChannel NewChannel = new SnooperPoolChannel(ChannelName);
                Channels.Add(NewChannel);
                return NewChannel;
            }
        }

        public void CreateInstanceChannel(string Name)
        {
            Channels.Add(new SnooperPoolChannel(Name));
        }

        public IEnumerator<SnooperPoolChannel> GetEnumerator()
        {
            return Channels.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Channels.GetEnumerator();
        }
    }
}
