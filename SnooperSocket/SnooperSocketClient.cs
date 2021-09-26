using Newtonsoft.Json;
using SnooperSocket.Cryptography;
using SnooperSocket.Cryptography.Protocals;
using SnooperSocket.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnooperSocket
{
    public class SnooperSocketClient
    {
        public TcpClient Client;
        protected ConcurrentQueue<SnooperStackMessage> Queue = new ConcurrentQueue<SnooperStackMessage>();

        public event MessageReceivedArgs UnhandledMessageReceived;

        public delegate Task MessageReceivedArgs(SnooperMessage Message);

        public event RequestReceivedArgs UnhandledRequestReceived;

        public delegate Task<object> RequestReceivedArgs(SnooperMessage Request);

        public event ClientAuthorisedArgs ClientAuthorised;

        public delegate void ClientAuthorisedArgs();

        public SnooperSecurityProtocal Security { get; protected set; } = new BaseSnooperSecurityProtocal();
        public SnooperChannelStack Channels { get; protected set; }
        protected SnooperChannelStack RedirectedChannels;

        private SnooperPoolChannelStack PoolChannel;

        public event DisconnectedArgs OnDisconnect;

        public delegate void DisconnectedArgs(SnooperSocketClient Client);

        private bool active = false;

        private SnooperRequestStack Requests = new SnooperRequestStack();

        private Stream NetStream;

        public bool Connected { get; protected set; } = false;

        public void Start()
        {
            if (!active)
            {
                if (NetStream == null) NetStream = Client.GetStream();
                Task.Run(ServerListener);
                Task.Run(MessageManager);
                active = true;
                Connected = true;
                Security.OnReady();
            }
        }

        public void SetNetStream(Stream NewStream)
        {
            NetStream = NewStream;
        }

        public void InvokeAuthorisation()
        {
            ClientAuthorised?.Invoke();
        }

        public void SetSecurityProtocal(SnooperSecurityProtocal Protocal)
        {
            if (active)
            {
                throw new InvalidOperationException("The socket is already running.");
            }
            Security = Protocal;
            Security.SetChannelOverrides(RedirectedChannels);
            Security.Init();
        }

        public void Disconnect(bool SendMessage = true)
        {
            if (SendMessage)
            {
                Channels["$SnooperSocket.Disconnect"].Write(new byte[] { });
            }
            Flush();
            Connected = false;
            Client.Close();
            active = false;
            OnDisconnect?.Invoke(this);
        }

        public SnooperSocketClient(TcpClient tcpClient = null)
        {
            Channels = new SnooperChannelStack(this);
            RedirectedChannels = new SnooperChannelStack(this);
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

        private async Task MessageManager()
        {
            while (Connected)
            {
                SnooperStackMessage MSG;
                if (Queue.TryDequeue(out MSG))
                {
                    try
                    {
                        using (var data = MSG.Data)
                        {
                            byte[] LenHeader = BitConverter.GetBytes((uint)data.Length);
                            await NetStream.WriteAsync(LenHeader, 0, 4);
                            if (MSG.Header != null) await NetStream.WriteAsync(MSG.Header, 0, MSG.Header.Length);

                            NetStream.WriteByte((byte)SnooperBytes.DataStart);
                            data.Position = 0;
                            await MSG.Data.CopyToAsync(NetStream);
                            MSG.CompletionSource.SetResult(null);
                        }
                    }
                    catch (IOException)
                    {
                        Disconnect(false);
                    }
                    catch (ObjectDisposedException)
                    {
                        Disconnect(false);
                    }
                }
                else
                {
                    await Task.Delay(100);
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
            if (Response == null) Response = new object();
            string Channel = Request.Channel;
            string RID = Request.Headers["$QueryID"];
            Dictionary<string, string> _Headers = new Dictionary<string, string>();
            _Headers.Add("$BaseMessageType", "Response");
            _Headers.Add("$ObjectType", Response.GetType().Name);
            _Headers.Add("$QueryID", RID);
            if (Channel != null) _Headers.Add($"$Channel", Channel);
            string Resp = JsonConvert.SerializeObject(Response);
            MemoryStream DatStream = new MemoryStream(Encoding.UTF8.GetBytes(Resp));

            if (Security.EncryptStream(DatStream, out MemoryStream ResStream, ref _Headers))
            {
                if (DatStream != ResStream) DatStream.Dispose();

                Queue.Enqueue(new SnooperStackMessage() { Data = ResStream, Header = GetHeaderBytes(_Headers) });
            }
            else
            {
                throw new UnauthorizedAccessException();
            }
        }

        private async Task ServerListener()
        {
            //NetworkStream CStream = Client.GetStream();
            while (Connected)
            {
                try
                {
                    using (var HeaderStream = new MemoryStream())
                    {
                        byte[] LengthHeader = new byte[4];
                        await NetStream.ReadAsync(LengthHeader, 0, 4);
                        uint MessageSize = BitConverter.ToUInt32(LengthHeader, 0);
                        while (true)
                        {
                            int RByte = NetStream.ReadByte();
                            if (RByte == (int)SnooperBytes.DataStart) break;
                            HeaderStream.WriteByte((byte)RByte);
                        }
                        var ContentStream = new MemoryStream();

                        while (!(ContentStream.Length >= MessageSize))
                        {
                            int _BUFFERSIZE = 1024;
                            long Remaining = MessageSize - ContentStream.Length;
                            if (_BUFFERSIZE > Remaining) _BUFFERSIZE = (int)Remaining;
                            byte[] Buffer = new byte[_BUFFERSIZE];
                            int Read = await NetStream.ReadAsync(Buffer, 0, _BUFFERSIZE);
                            await ContentStream.WriteAsync(Buffer, 0, Read);
                        }

                        HeaderStream.Position = 0;
                        Dictionary<string, string> Headers = ParseHeader(HeaderStream);
                        if (Security.DecryptStream(ContentStream, out MemoryStream DecStream, ref Headers))
                        {
                            if (ContentStream != DecStream)
                            {
                                ContentStream.Dispose();
                            }
                            ContentStream = DecStream;

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

                            ThreadPool.QueueUserWorkItem(async (_) => await HandleIncommingMessage(NMSG));
                        }
                        else
                        {
                            // Invalid Request
                            ContentStream.Dispose();
                        }
                    }
                }
                catch (ObjectDisposedException)
                {
                    //Disconnected
                    Disconnect(false);
                }
                catch (IOException)
                {
                    //Disconnected Unexpectedly
                    Disconnect(false);
                }
            }
        }



        private async Task HandleIncommingMessage(SnooperMessage message)
        {
            if (message.Channel == "$SnooperSocket.Disconnect")
            {
                Disconnect(false);
                return;
            }
            else if (message.RequestType == SnooperMessageType.Response)
            {
                string RID = message.Headers["$QueryID"];
                Requests.ReleaseRequest(RID, message);
            }
            else if (message.RequestType == SnooperMessageType.Request)
            {
                object ReturnObject = null;
                ReturnObject = await RedirectedChannels[message.Channel].TryRaiseRequest(message);
                if (ReturnObject == null && !Security.RedirectRequests)
                {
                    ReturnObject = await Channels[message.Channel].TryRaiseRequest(message);
                    if (ReturnObject == null) ReturnObject = await UnhandledRequestReceived?.Invoke(message);
                    if (ReturnObject == null) ReturnObject = new object();
                }
                RespondToRequest(message, ReturnObject);
            }
            else
            {
                bool RedirectHandled = await RedirectedChannels[message.Channel].TryRaise(message);
                if (!RedirectHandled && !Security.RedirectRequests)
                {
                    if (PoolChannel != null) message.Requesthandled = PoolChannel[message.Channel].TryRaise(message, this);

                    if (!message.Requesthandled)
                    {
                        bool ChannelsHandled = await Channels[message.Channel].TryRaise(message);
                        if (!ChannelsHandled)
                        {
                            await UnhandledMessageReceived?.Invoke(message);
                        }
                    }
                }
            }


        }

        public void WriteRawData(byte[] Message)
        {
            var msg = new SnooperStackMessage() { Data = new MemoryStream(Message) };
            Queue.Enqueue(msg);
        }

        public void WriteRawData(Stream Message)
        {
            var msg = new SnooperStackMessage() { Data = Message };
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
            MemoryStream DatStream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Data)));

            Security.EncryptStream(DatStream, out MemoryStream ResStream, ref _Headers);

            Queue.Enqueue(new SnooperStackMessage() { Data = ResStream, Header = GetHeaderBytes(_Headers) });
        }

        public async Task WriteAsync(object Data, Dictionary<string, string> Headers = null, string Channel = null)
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
            MemoryStream DatStream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Data)));

            Security.EncryptStream(DatStream, out MemoryStream ResStream, ref _Headers);
            var msg = new SnooperStackMessage() { Data = ResStream, Header = GetHeaderBytes(_Headers) };
            Queue.Enqueue(msg);
            await msg.CompletionSource.Task;
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
            Security.EncryptStream(DatStream, out MemoryStream ResStream, ref _Headers);
            if (DatStream != ResStream) DatStream.Dispose();

            Queue.Enqueue(new SnooperStackMessage() { Data = ResStream, Header = GetHeaderBytes(_Headers) });
        }


        public async Task WriteAsync(byte[] Message, Dictionary<string, string> Headers = null, string Channel = null)
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
            Security.EncryptStream(DatStream, out MemoryStream ResStream, ref _Headers);
            if (DatStream != ResStream) DatStream.Dispose();
            var msg = new SnooperStackMessage() { Data = ResStream, Header = GetHeaderBytes(_Headers) };
            Queue.Enqueue(msg);
            await msg.CompletionSource.Task;
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

            // Fix to allow all streams
            Security.EncryptStream((MemoryStream)Message, out MemoryStream ResStream, ref _Headers);

            Queue.Enqueue(new SnooperStackMessage() { Data = ResStream, Header = GetHeaderBytes(_Headers) });
        }

        public async Task WriteAsync(Stream Message, Dictionary<string, string> Headers = null, string Channel = null)
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

            // Fix to allow all streams
            Security.EncryptStream((MemoryStream)Message, out MemoryStream ResStream, ref _Headers);

            var msg = new SnooperStackMessage() { Data = ResStream, Header = GetHeaderBytes(_Headers) };
            Queue.Enqueue(msg);
            await msg.CompletionSource.Task;
        }


        public T Query<T>(object Data, Dictionary<string, string> Headers = null, string Channel = null)
        {
            string QueryID = CryptographicProvider.GetCryptographicallySecureString(16);
            if (Data == null) Data = new object();
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
            }
            else
            {
                DatStream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Data)));
            }
            SnooperRequest Request = new SnooperRequest();
            Request.RequestID = QueryID;
            Requests.StackRequest(Request);

            Security.EncryptStream(DatStream, out MemoryStream ResStream, ref _Headers);
            if (DatStream != ResStream) DatStream.Dispose();

            Queue.Enqueue(new SnooperStackMessage() { Data = ResStream, Header = GetHeaderBytes(_Headers) });
            Request.Wait();
            return Request.Response.ReadObject<T>();
        }

        public async Task<T> QueryAsync<T>(object Data, Dictionary<string, string> Headers = null, string Channel = null)
        {
            string QueryID = CryptographicProvider.GetCryptographicallySecureString(16);
            if (Data == null) Data = new object();
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
            }
            else
            {
                DatStream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Data)));
            }
            SnooperRequest Request = new SnooperRequest();
            Request.RequestID = QueryID;
            Requests.StackRequest(Request);

            Security.EncryptStream(DatStream, out MemoryStream ResStream, ref _Headers);
            if (DatStream != ResStream) DatStream.Dispose();

            var msg = new SnooperStackMessage() { Data = ResStream, Header = GetHeaderBytes(_Headers) };
            Queue.Enqueue(msg);

            var res = await Request.CompletionSource.Task;
            return res.ReadObject<T>();
        }

        public T Query<T>(object Data, string Channel)
        {
            return Query<T>(Data, null, Channel);
        }
        public async Task<T> QueryAsync<T>(object Data, string Channel)
        {
            return await QueryAsync<T>(Data, null, Channel);
        }

        public T Query<T>(string Channel)
        {
            return Query<T>(null, null, Channel);
        }
        public async Task<T> QueryAsync<T>(string Channel)
        {
            return await QueryAsync<T>(null, null, Channel);
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