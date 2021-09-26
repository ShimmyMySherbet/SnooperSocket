using SnooperSocket.Cryptography.Protocals;
using SnooperSocket.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnooperSocket.Test
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            new Thread(Server).Start();
            new Thread(Client).Start();
            Thread.Sleep(-1);
        }

        public static SnooperSocketClient ServerClient;
        public static SnooperSocketClient LocalClient;

        private static void Client()
        {
            Console.WriteLine("[Client] Connecting to server...");
            TcpClient Client = new TcpClient();
            Console.WriteLine($"[Client] Network Buffer Size: {Client.ReceiveBufferSize}");
            Client.Connect(IPAddress.Parse("127.0.0.1"), 2081);
            Console.WriteLine("[Client] Connected.");
            LocalClient = new SnooperSocketClient() { Client = Client };
            LocalClient.OnDisconnect += LocalClient_OnDisconnect;
            LocalClient.SetSecurityProtocal(new MutualKeyProtocal() { Key = k, IsServer = false });
            LocalClient.Start();
            Console.WriteLine($"[Client] Requesting Auth...");
            Console.WriteLine($"[Client] Authed: {((MutualKeyProtocal)LocalClient.Security).ValidateConnection()}");

            Console.WriteLine("[Client] Sending on open stream");
            LocalClient.Write(new MSGDat() { Content = "HEYO DERE BOIO!" }, null, "$SnooperSec.MutualKeyProtocal.Validate");

            //while(true)
            //{
            //    Console.ReadLine();
            //    LocalClient.Write(new MSGDat() { Content = "HEY!" }, null, "Request");
            //}
            Console.WriteLine("DC...");
            LocalClient.Disconnect();
        }

        private static void LocalClient_OnDisconnect(SnooperSocketClient Client)
        {
            Console.WriteLine("[Client] DISCONNECTED!");
        }

        public static string k = "!";


        public class TransferObject
        {
            public string Data = "NoData";
        }

        private static void Server()
        {
            Console.WriteLine("[Server] Starting Server.");
            TcpListener tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 2081);
            tcpListener.Start();
            var ent = tcpListener.AcceptTcpClient();

            ServerClient = new SnooperSocketClient() { Client = ent };
            ServerClient.OnDisconnect += ServerClient_OnDisconnect;
            ServerClient.SetSecurityProtocal(new MutualKeyProtocal() { Key = k, IsServer = true });
            ServerClient.Start();

            ServerClient.Channels["Request"].MessageReceived += ServerRequest_Message;
            ServerClient.Channels["$SnooperSec.MutualKeyProtocal.Validate"].MessageReceived += Program_MessageReceived; ;
            ServerClient.Channels["Request"].RequestReceived += ServerRequest_Request;

            Console.WriteLine($"[Server] Authed: {ServerClient.Security.ValidateConnection()}");
        }

        private static void ServerClient_OnDisconnect(SnooperSocketClient Client)
        {
            Console.WriteLine("[Server] DISCONNECTED!");
        }

        private static Task Program_MessageReceived(SnooperMessage message)
        {
            Console.WriteLine($"REC MSG: {message.ReadObject<MSGDat>().Content}");
            return Task.CompletedTask;
        }

        private static Task<object> ServerRequest_Request(SnooperMessage Request)
        {
            Console.WriteLine("[Server] Got a Request in channel Request");
            Thread.Sleep(3000);
            Console.WriteLine("[Server] Returned Request");
            return Task.FromResult((object)new TransferObject() { Data = "Hello World!" });
        }

        private static Task ServerRequest_Message(SnooperMessage message)
        {
            Console.WriteLine("[Server] Got a message in channel Request");
            Console.WriteLine($"{message.ReadObject<MSGDat>().Content}");
            return Task.CompletedTask;
        }
    }

    public class MSGDat
    {
        public string Content;
    }

}