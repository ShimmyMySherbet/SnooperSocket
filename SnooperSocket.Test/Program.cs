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
            LocalClient.Start();
            TransferObject transfer = LocalClient.Query<TransferObject>(new TransferObject(), null, "Request");
            Console.WriteLine("[Client] Query Returned: " + transfer.Data);


            while(true)
            {
                Console.ReadLine();
                LocalClient.Write(new object(), null, "Request");
            }
        }

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
            ServerClient.Start();


            ServerClient.Channels["Request"].MessageReceived += ServerRequest_Message;
            ServerClient.Channels["Request"].RequestReceived += ServerRequest_Request;
        }

        private static object ServerRequest_Request(SnooperMessage Request)
        {
            Console.WriteLine("[Server] Got a Request in channel Request");
            Thread.Sleep(3000);
            Console.WriteLine("[Server] Returned Request");
            return new TransferObject() { Data = "Hello World!" };
        }

        private static void ServerRequest_Message(SnooperMessage message)
        {
            Console.WriteLine("[Server] Got a message in channel Request");
        }
    }

}