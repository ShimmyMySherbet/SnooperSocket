using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using SnooperSocket;
using SnooperSocket.Cryptography.Protocals;

namespace ServerClientAuthTest
{
    public static class Client
    {
        public static void Run()
        {

            while (true)
            {
                Console.Write("Password: ");
                string Password = Console.ReadLine();

                TcpClient Client = new TcpClient();
                Client.Connect(IPAddress.Loopback, 2181);

                SnooperSocketClient SocketClient = new SnooperSocketClient(Client);
                SocketClient.SetSecurityProtocal(new MutualKeyProtocal() { IsServer = false, Key = Password });
                SocketClient.OnDisconnect += SocketClient_OnDisconnect;
                SocketClient.Channels["Message"].MessageReceived += Client_MessageReceived;
                SocketClient.Start();
                //if (SocketClient.Security.TryValidateConnection())
                //{
                //    Console.WriteLine($"[Client] Authorised");

                //    Console.WriteLine("Press any to continue...");
                //    Console.ReadKey();

                //} else
                //{
                //    Console.WriteLine($"[Client] Auth Denied.");
                //}


                Console.WriteLine("Press any to continue...");
                Console.ReadKey();

                Console.WriteLine("[Client] Disconnecting...");
                SocketClient.Disconnect();

                Client.Dispose();
                Console.WriteLine("\n");
            }



        }

        private static Task Client_MessageReceived(SnooperSocket.Models.SnooperMessage message)
        {
            Console.WriteLine($"[Client] Rec Message: {message.ReadObject<StatusMessage>().Message}");
            return Task.CompletedTask;
        }

        private static void SocketClient_OnDisconnect(SnooperSocketClient Client)
        {
            Console.WriteLine("[Client] Disconnected");
        }
    }
}
