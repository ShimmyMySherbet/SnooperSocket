using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SnooperSocket;
using SnooperSocket.Cryptography.Protocals;

namespace ServerClientAuthTest
{
    public static class Server
    {
        public static string Password = "Hello!";
        static TcpListener Listener = new TcpListener(IPAddress.Loopback, 2181);
        public static void StartServer()
        {
            Listener.Start();
            new Thread(() => {
                while(true)
                {
                    TcpClient NewClient = Listener.AcceptTcpClient();
                    new Thread(() => HandleNewClient(NewClient)).Start();
                }

            }).Start();
        }
        private static void HandleNewClient(TcpClient Client)
        {
            SnooperSocketClient SocketClient = new SnooperSocketClient(Client);
            SocketClient.SetSecurityProtocal(new MutualKeyProtocal() { IsServer = true, Key = Password });
            SocketClient.Start();
            Thread.Sleep(400);
            if (SocketClient.Security.TryValidateConnection())
            {
                Console.WriteLine("[Server] Client Authed!");
                SocketClient.Channels["Message"].Write(new StatusMessage() { Message = "Hello!", Status = true });
                SocketClient.Disconnect();
                Client.Dispose();
            } else
            {
                Console.WriteLine("[Server] Client Denied.");
                SocketClient.Disconnect();
                Client.Dispose();
            }
        }


    }
}
