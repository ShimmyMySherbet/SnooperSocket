using SnooperSocket.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SnooperSocket.Test
{
    public class datas
    {
        public string DS = "dsads";
        public string DFDD = "aa";
    }

    public class Resp
    {
        public string ENTS = "dsadas";
    }

    public class ChannelMessage
    {
        public string DS = "dsads";
        public string DFDD = "aa";
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            new Thread(Server).Start();
            new Thread(Client).Start();
            Console.ReadLine();
        }

        private static void Client()
        {
            Console.WriteLine("[Client] Connecting to server...");
            TcpClient Client = new TcpClient();
            Client.ReceiveBufferSize = (1024 * 1024 * 2);
            Console.WriteLine($"[Client] Network Buffer Size: {Client.ReceiveBufferSize}");
            Client.Connect(IPAddress.Parse("127.0.0.1"), 2081);
            Console.WriteLine("[Client] Connected.");
            SnooperSocketClient ClE = new SnooperSocketClient() { Client = Client };
            Client.ReceiveBufferSize = 1024 * 1024 * 50;
            Thread.Sleep(1000);
            ClE.Start();
            ClE.Channels["Logs"].MessageReceived += Program_MessageReceived;
            ClE.MessageRecieved += ClE_MessageRecieved;
            ClE.Write(new Resp());
            ClE.Write(new byte[] { 21, 12, 11, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21 });
        }

        private static void Program_MessageReceived(SnooperMessage message)
        {
            Console.WriteLine($"[CLIENT] [CHANNEL: Logs] Recieved Message");
        }

        private static void ClE_MessageRecieved(SnooperMessage Message)
        {
            Console.WriteLine($"[Client]Recieved Message" +
                $"\n  >Message Type: {Message.RequestType}" +
                $"\n  >Message Handled: {Message.Requesthandled}" +
                $"\n  >Message Channel Type: {Message.Channel}" +
                $"\n  >Request Length: {Message.Data.Length}" +
                $"\n  >Object Type: {Message?.ObjectType}" +
                $"\n  >Headers:");
            foreach (var H in Message.Headers)
            {
                Console.WriteLine($"    \\>{H.Key}: {H.Value}");
            }
            Console.WriteLine();
        }

        private static void Server()
        {
            Console.WriteLine("[Server] Starting Server.");
            TcpListener tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 2081);
            tcpListener.Start();
            var ent = tcpListener.AcceptTcpClient();

            SnooperSocketClient cl = new SnooperSocketClient() { Client = ent };
            cl.Start();
            cl.MessageRecieved += Cl_MessageRecieved;
            //cl.Write(new byte[] { 21, 23, 43, 100 });
            for (int i = 0; i < 10; i++)
            {
                cl.Write(new ChannelMessage(), null, "Logs");
            }

            //Console.WriteLine("[Server] Client Connected.");
            //Dictionary<string, string> Headers = new Dictionary<string, string>();
            //Headers.Add("H1", "V1");
            //Headers.Add("H2", "V2");
            //Headers.Add("H3", "V3");

            //byte[] Header = new byte[] { 121, 12, 32, 123, 121, 213, 21, 43, 44, (byte)SnooperBytes.HeaderDelim, 0};
            ////byte[] Header = GetHeaderBytes(Headers);
            //byte[] Payload = new byte[30];
            //RNGCryptoServiceProvider RNG = new RNGCryptoServiceProvider();

            //RNG.GetBytes(Payload);
            //byte[] LengthHeader = BitConverter.GetBytes(Payload.Length);
            //Console.WriteLine($"[Server] Header Length: {Header.Length}");
            //Console.WriteLine($"[Server] Payload Length: {Payload.Length}");

            //NetworkStream EStream = ent.GetStream();
            //EStream.Write(LengthHeader, 0, 4);
            //EStream.Write(Header, 0, Header.Length);
            //EStream.WriteByte(246);
            //EStream.Write(Payload, 0, Payload.Length);
            //Console.WriteLine($"[Server] Data Written");
            //Console.WriteLine("[Server] Finished");
            Console.ReadLine();
        }

        private static void Cl_MessageRecieved(SnooperMessage Message)
        {
            Console.WriteLine($"[SERVER]Recieved Message" +
              $"\n  >Message Handled: {Message.Requesthandled}" +
              $"\n  >Message Type: {Message.RequestType}" +
              $"\n  >Message Channel Type: {Message.Channel}" +
              $"\n  >Request Length: {Message.Data.Length}" +
              $"\n  >Object Type: {Message?.ObjectType}" +
              $"\n  >Headers:");
            foreach (var H in Message.Headers)
            {
                Console.WriteLine($"    \\>{H.Key}: {H.Value}");
            }
        }

        private static byte[] GetHeaderBytes(Dictionary<string, string> Headers)
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
    }
}