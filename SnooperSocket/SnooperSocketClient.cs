using Newtonsoft.Json;
using SnooperSocket.Cryptography;
using SnooperSocket.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SnooperSocket
{
    public class SnooperSocketClient
    {
        public TcpClient Client;
        private ConcurrentQueue<SnooperStackMessage> Queue = new ConcurrentQueue<SnooperStackMessage>();

        public delegate void MessageReceivedArgs(SnooperMessage Message);

        public event MessageReceivedArgs MessageReceived;

        public delegate object RequestReceivedArgs(SnooperMessage Request);

        public event RequestReceivedArgs RequestReceived;

        public readonly SnooperChannelStack Channels;

        public bool RaiseHandledRequests = true;

        public bool RaiseRequestResponses = false;

        private SnooperPoolChannelStack PoolChannel;

        public event DisconnectedArgs OnDisconnect;

        public delegate void DisconnectedArgs(SnooperSocketClient Client);

        private bool active = false;

        private SnooperRequestStack Requests = new SnooperRequestStack();

        public void Start()
        {
            if (!active)
            {
                new Thread(ServerListener).Start();
                new Thread(MessageManager).Start();
                active = true;
            }
        }

        public SnooperSocketClient(TcpClient tcpClient = null)
        {
            Channels = new SnooperChannelStack(this);
            if (tcpClient != null) Client = tcpClient;
        }

        public void JoinPool(SnooperSocketClientPool Pool)
        {
            PoolChannel = Pool.Stack;
        }

        public void JoinPool<T>(SnooperSocketClientPool<T> Pool)
        {
            PoolChannel = Pool.Stack;
        }

        private void MessageManager()
        {
            NetworkStream CStream = Client.GetStream();
            while (Client.Connected)
            {
                SnooperStackMessage MSG;
                if (Queue.TryDequeue(out MSG))
                {
                    try
                    {
                        byte[] LenHeader = BitConverter.GetBytes((uint)MSG.Data.Length);
                        CStream.Write(LenHeader, 0, 4);
                        if (MSG.Header != null) CStream.Write(MSG.Header, 0, MSG.Header.Length);
                        CStream.WriteByte((byte)SnooperBytes.DataStart);
                        MSG.Data.Position = 0;
                        MSG.Data.CopyTo(CStream);
                        MSG.Data.Dispose();
                    }
                    catch (IOException)
                    {
                        if (!Client.Connected) OnDisconnect?.Invoke(this);
                    }
                    catch (ObjectDisposedException)
                    {
                        if (!Client.Connected) OnDisconnect?.Invoke(this);
                    }
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
        }

        public void Flush()
        {
            while (!Queue.IsEmpty)
            {
                Thread.Sleep(100);
            }
        }

        protected void RespondToRequest(SnooperMessage Request, object Response)
        {
            string Channel = Request.Channel;
            string RID = Request.Headers["$QueryID"];
            Dictionary<string, string> _Headers = new Dictionary<string, string>();
            _Headers.Add("$BaseMessageType", "Response");
            _Headers.Add("$ObjectType", Response.GetType().Name);
            _Headers.Add("$QueryID", RID);
            if (Channel != null) _Headers.Add($"$Channel", Channel);
            MemoryStream DatStream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Response)));
            Queue.Enqueue(new SnooperStackMessage() { Data = DatStream, Header = GetHeaderBytes(_Headers) });
        }

        private void ServerListener()
        {
            NetworkStream CStream = Client.GetStream();
            while (Client.Connected)
            {
                try
                {
                    using (MemoryStream HeaderStream = new MemoryStream())
                    {
                        byte[] LengthHeader = new byte[4];
                        CStream.Read(LengthHeader, 0, 4);
                        uint MessageSize = BitConverter.ToUInt32(LengthHeader, 0);
                        while (true)
                        {
                            int RByte = CStream.ReadByte();
                            if (RByte == (int)SnooperBytes.DataStart) break;
                            HeaderStream.WriteByte((byte)RByte);
                        }
                        MemoryStream ContentStream = new MemoryStream();

                        while (!(ContentStream.Length >= MessageSize))
                        {
                            int _BUFFERSIZE = 1024;
                            long Remaining = MessageSize - ContentStream.Length;
                            if (_BUFFERSIZE > Remaining) _BUFFERSIZE = (int)Remaining;
                            byte[] Buffer = new byte[_BUFFERSIZE];
                            int Read = CStream.Read(Buffer, 0, _BUFFERSIZE);
                            ContentStream.Write(Buffer, 0, Read);
                        }

                        HeaderStream.Position = 0;
                        Dictionary<string, string> Headers = ParseHeader(HeaderStream);
                        SnooperMessage NMSG = new SnooperMessage();

                        NMSG.Headers = Headers;
                        HeaderStream.Position = 0;
                        NMSG.RawHeaderData = Encoding.UTF8.GetString(HeaderStream.ToArray());
                        NMSG.Data = ContentStream;
                        if (Headers.ContainsKey("$BaseMessageType"))
                        {
                            NMSG.RequestType = (SnooperMessageType)Enum.Parse(typeof(SnooperMessageType), Headers["$BaseMessageType"]);
                            NMSG.IsManagedMessage = true;
                        }
                        else
                        {
                            NMSG.IsManagedMessage = false;
                        }

                        if (Headers.ContainsKey("$Channel"))
                        {
                            NMSG.Channel = Headers["$Channel"];
                        }
                        if (Headers.ContainsKey("$ObjectType"))
                        {
                            NMSG.ObjectType = Headers["$ObjectType"];
                        }

                        new Thread(delegate ()
                        {
                            if (NMSG.RequestType == SnooperMessageType.Response)
                            {
                                string RID = NMSG.Headers["$QueryID"];
                                Requests.ReleaseRequest(RID, NMSG);
                            }
                            else if (NMSG.RequestType == SnooperMessageType.Request)
                            {
                                object ReturnObject = null;
                                ReturnObject = Channels[NMSG.Channel].TryRaiseRequest(NMSG);
                                if (ReturnObject == null)
                                {
                                    ReturnObject = RequestReceived?.Invoke(NMSG);
                                }
                                if (ReturnObject == null) ReturnObject = new object();
                                RespondToRequest(NMSG, ReturnObject);
                            }
                            else
                            {
                                if (PoolChannel != null) NMSG.Requesthandled = PoolChannel[NMSG.Channel].TryRaise(NMSG, this);
                                if ((NMSG.Requesthandled & RaiseHandledRequests) || !NMSG.Requesthandled)
                                {
                                    bool a = Channels[NMSG.Channel].TryRaise(NMSG);
                                    if (a) NMSG.Requesthandled = true;
                                }
                                if ((NMSG.Requesthandled & RaiseHandledRequests) || !NMSG.Requesthandled)
                                {
                                    MessageReceived?.Invoke(NMSG);
                                }
                            }
                        }).Start();
                    }
                }
                catch (ObjectDisposedException)
                {
                    //Disconnected
                    if (!Client.Connected) OnDisconnect?.Invoke(this);
                }
                catch (IOException)
                {
                    //Disconnected Unexpectedly
                    if (!Client.Connected) OnDisconnect?.Invoke(this);
                }
            }
        }

        public void WriteRawData(byte[] Message, string Header = null)
        {
            var msg = new SnooperStackMessage() { Data = new MemoryStream(Message) };
            if (Header != null) msg.Header = Encoding.UTF8.GetBytes(Header);
            Queue.Enqueue(msg);
        }

        public void WriteRawData(Stream Message, string Header = null)
        {
            var msg = new SnooperStackMessage() { Data = Message };
            if (Header != null) msg.Header = Encoding.UTF8.GetBytes(Header);
            Queue.Enqueue(msg);
        }

        public void Write(object Data, Dictionary<string, string> Headers = null, string Channel = null)
        {
            Dictionary<string, string> _Headers = new Dictionary<string, string>();
            _Headers.Add("$BaseMessageType", "Object");
            _Headers.Add("$ObjectType", Data.GetType().Name);
            if (Channel != null) _Headers.Add($"$Channel", Channel);

            if (Headers != null)
            {
                foreach (var ent in Headers)
                {
                    if (_Headers.ContainsKey(ent.Key))
                    {
                        _Headers[ent.Key] = ent.Value;
                    }
                    else
                    {
                        _Headers.Add(ent.Key, ent.Value);
                    }
                }
            }
            MemoryStream DatStream = new MemoryStream(UTF8Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Data)));
            Queue.Enqueue(new SnooperStackMessage() { Data = DatStream, Header = GetHeaderBytes(_Headers) });
        }

        public void Write(byte[] Message, Dictionary<string, string> Headers = null, string Channel = null)
        {
            Dictionary<string, string> _Headers = new Dictionary<string, string>();
            _Headers.Add("$BaseMessageType", "Binary");
            if (Channel != null) _Headers.Add($"$Channel", Channel);
            if (Headers != null)
            {
                foreach (var ent in Headers)
                {
                    if (_Headers.ContainsKey(ent.Key))
                    {
                        _Headers[ent.Key] = ent.Value;
                    }
                    else
                    {
                        _Headers.Add(ent.Key, ent.Value);
                    }
                }
            }
            MemoryStream DatStream = new MemoryStream(Message);
            Queue.Enqueue(new SnooperStackMessage() { Data = DatStream, Header = GetHeaderBytes(_Headers) });
        }

        public void Write(Stream Message, Dictionary<string, string> Headers = null, string Channel = null)
        {
            Dictionary<string, string> _Headers = new Dictionary<string, string>();
            _Headers.Add("$BaseMessageType", "Binary");
            if (Channel != null) _Headers.Add($"$Channel", Channel);

            if (Headers != null)
            {
                foreach (var ent in Headers)
                {
                    if (_Headers.ContainsKey(ent.Key))
                    {
                        _Headers[ent.Key] = ent.Value;
                    }
                    else
                    {
                        _Headers.Add(ent.Key, ent.Value);
                    }
                }
            }
            Queue.Enqueue(new SnooperStackMessage() { Data = Message, Header = GetHeaderBytes(_Headers) });
        }

        public T Query<T>(object Data, Dictionary<string, string> Headers = null, string Channel = null)
        {

            string QueryID = CryptographicProvider.GetCryptographicallySecureString(16);
            
            Dictionary<string, string> _Headers = new Dictionary<string, string>();
            _Headers.Add("$BaseMessageType", "Request");
            _Headers.Add("$ObjectType", Data.GetType().Name);
            _Headers.Add("$QueryID", QueryID);
            if (Channel != null) _Headers.Add($"$Channel", Channel);
            if (Headers != null)
            {
                foreach (var ent in Headers)
                {
                    if (_Headers.ContainsKey(ent.Key))
                    {
                        _Headers[ent.Key] = ent.Value;
                    }
                    else
                    {
                        _Headers.Add(ent.Key, ent.Value);
                    }
                }
            }

            bool BlankData = (Data == null || Data.GetType() == typeof(object));

            MemoryStream DatStream;
            if (BlankData)
            {
                DatStream = new MemoryStream(new byte[] { 0x7b, 0x7c });
            } else
            {
              DatStream =  new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Data)));
            }
            SnooperRequest Request = new SnooperRequest();
            Request.RequestID = QueryID;
            Requests.StackRequest(Request);
            Queue.Enqueue(new SnooperStackMessage() { Data = DatStream, Header = GetHeaderBytes(_Headers) });
            Request.Wait();
            return Request.Response.ReadObject<T>();
        }

        protected byte[] GetHeaderBytes(Dictionary<string, string> Headers)
        {
            MemoryStream DatStream = new MemoryStream();
            foreach (var Header in Headers)
            {
                if (DatStream.Length != 0) DatStream.WriteByte((byte)SnooperBytes.HeaderDelim);
                byte[] Key = Encoding.UTF8.GetBytes(Header.Key);
                byte[] Value = Encoding.UTF8.GetBytes(Header.Value);
                DatStream.Write(Key, 0, Key.Length);
                DatStream.WriteByte((byte)SnooperBytes.HeaderDelim);
                DatStream.Write(Value, 0, Value.Length);
            }
            return DatStream.ToArray();
        }

        protected Dictionary<string, string> ParseHeader(Stream Data)
        {
            Dictionary<string, string> Headers = new Dictionary<string, string>();
            bool EndOfStream = false;
            while (!EndOfStream)
            {
                using (MemoryStream Key = new MemoryStream())
                using (MemoryStream Value = new MemoryStream())
                {
                    bool Val = false;
                    while (true)
                    {
                        int B = Data.ReadByte();
                        if (B == -1) EndOfStream = true;

                        if (EndOfStream || B == (byte)SnooperBytes.HeaderDelim)
                        {
                            if (Val)
                            {
                                if (Key.Length != 0)
                                {
                                    string SKey = Encoding.UTF8.GetString(Key.ToArray());
                                    string SValue = Encoding.UTF8.GetString(Value.ToArray());
                                    if (Headers.ContainsKey(SKey))
                                    {
                                        Headers[SKey] = SValue;
                                    }
                                    else
                                    {
                                        Headers.Add(SKey, SValue);
                                    }
                                }
                                break;
                            }
                            else
                            {
                                Val = true;
                                continue;
                            }
                        }
                        if (Val)
                        {
                            Value.WriteByte((byte)B);
                        }
                        else
                        {
                            Key.WriteByte((byte)B);
                        }
                    }
                }
            }
            return Headers;
        }
    }
}