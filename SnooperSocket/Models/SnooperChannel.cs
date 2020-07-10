﻿using System.Collections.Generic;
using System.IO;

namespace SnooperSocket.Models
{
    public class SnooperChannel
    {
        public event ChannelMessageReceived MessageReceived;

        public event ChannelRequestRecevied RequestReceived;

        public SnooperSocketClient Socket;

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

        #region "Mirrors"

        public void Write(object Data, Dictionary<string, string> Headers = null)
        {
            Socket.Write(Data, Headers, ChannelName);
        }

        public void Write(byte[] Message, Dictionary<string, string> Headers = null)
        {
            Socket.Write(Message, Headers, ChannelName);
        }

        public void Write(Stream Message, Dictionary<string, string> Headers = null)
        {
            Socket.Write(Message, Headers, ChannelName);
        }

        public T Query<T>(object Data, Dictionary<string, string> Headers = null)
        {
            return Socket.Query<T>(Data, Headers, ChannelName);
        }

        public T Query<T>(object Data)
        {
            return Query<T>(Data, null);
        }

        public T Query<T>()
        {
            return Query<T>(null, null);
        }

        #endregion "Mirrors"
    }
}