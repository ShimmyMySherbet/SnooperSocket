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

namespace SnooperSocket.Test
{
    internal class Program
    {
        private static void Main(string[] args)
        {

            //MemoryStream E = new MemoryStream();
            ////using (CryptoStream S = new CryptoStream(E, new AesCryptoServiceProvider().CreateDecryptor(new byte[32], new byte[16]), CryptoStreamMode.Write))
            ////{
            ////}
            //E.Position = 0;
            //MemoryStream BaseInput = new MemoryStream(new byte[] { 10, 21, 26, 37, 21, 26, 37, 21, 26, 37, 21, 26, 37, 21, 26, 37, 21, 26, 37 });
            //Console.WriteLine($"In:  {string.Join(", ", BaseInput.ToArray())}");
            //MutualKeyProtocal Protocal = new MutualKeyProtocal();
            //Protocal.Key = "HelloWorld!";



            //Dictionary<string, string> Headers = new Dictionary<string, string>();

            //Protocal.EncryptStream(BaseInput, out MemoryStream ENCStream, ref Headers);
            //Console.WriteLine($"ENC: {string.Join(", ", ENCStream.ToArray())}");


            //Protocal.DecryptStream(ENCStream, out MemoryStream DECStream, ref Headers);
            //Console.WriteLine($"DEC: {string.Join(", ", DECStream.ToArray())}");


            //Console.ReadLine();






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
            LocalClient.SetSecurityProtocal(new MutualKeyProtocal() { Key = k });
            LocalClient.Start();
            Console.WriteLine($"[Client] Authed: {((MutualKeyProtocal)LocalClient.Security).ValidateConnection()}");
            //TransferObject transfer = LocalClient.Query<TransferObject>(new TransferObject(), null, "Request");
            //Console.WriteLine("[Client] Query Returned: " + transfer.Data);


            Console.WriteLine("[Client] Sending on open stream");
            LocalClient.Write(new MSGDat() { Content = "HEYO DERE BOIO!" }, null, "$SnooperSec.MutualKeyProtocal.Validate");

            while(true)
            {
                Console.ReadLine();
                LocalClient.Write(new MSGDat() { Content = "HEY!" }, null, "Request");
            }
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
            ServerClient.SetSecurityProtocal(new MutualKeyProtocal() { Key = k });
            ServerClient.Start();
            //Console.WriteLine($"[Server] Authed: {((MutualKeyProtocal)ServerClient.Security).ValidateConnection()}");


            ServerClient.Channels["Request"].MessageReceived += ServerRequest_Message;
            ServerClient.Channels["$SnooperSec.MutualKeyProtocal.Validate"].MessageReceived += Program_MessageReceived; ;
            ServerClient.Channels["Request"].RequestReceived += ServerRequest_Request;
        }

        private static void Program_MessageReceived(SnooperMessage message)
        {
            Console.WriteLine($"REC MSG: {message.ReadObject<MSGDat>().Content}");
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
            Console.WriteLine($"{message.ReadObject<MSGDat>().Content}");
        }
    }

    public class MSGDat
    {
        public string Content;
    }

}