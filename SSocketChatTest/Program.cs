using SnooperSocket;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SSocketChatTest
{
    internal class Program
    {
        public static int MPort;
        public static string IP;
        public static SnooperSocketClient Client;

        private static void Main(string[] args)
        {
            Console.Write("Client/Server [C/S] ");
            string inp = Console.ReadLine();

            Console.Write("Server Address: ");
            IP = Console.ReadLine();
            Console.Write("Port: ");
            MPort = Convert.ToInt32(Console.ReadLine());
            IPEndPoint EP = new IPEndPoint(IPAddress.Parse(IP), MPort);
            if (inp.ToLower() == "s")
            {
                Console.WriteLine("Starting local server...");
                Server.StartServer(EP);
                IPEndPoint LEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), MPort);
                TcpClient TClient = new TcpClient();
                Console.WriteLine("Connecting to local server...");
                TClient.Connect(LEP);
                Console.WriteLine("Conencted.");
                Client = new SnooperSocketClient() { Client = TClient };
            }
            else
            {
                    TcpClient TClient = new TcpClient();
                    Console.WriteLine("Connecting to remote server...");
                    TClient.Connect(EP);
                    Console.WriteLine("Conencted.");
                    Client = new SnooperSocketClient() { Client = TClient };
            }
            Client.Channels["Messages"].MessageReceived += OnMessage;
            Client.Start();
            Console.Write("Username: ");
            string Username = Console.ReadLine();

            Client.Write(new UserLoginData() { Username = Username }, null, "Login");
            while (true)
            {
                string msg = Console.ReadLine();

                if (msg == "$disconnect") break;
                else if (msg == "$clients")
                {
                    if (Server.Active)
                    {
                        foreach(var cl in Server.Clients)
                        {
                            var a = Server.Clients.GetClientData(cl);
                            Console.WriteLine($"Username: {a.Username}, Logged in: {a.LoggedIn}. IP: {cl.Client.Client.RemoteEndPoint}");
                        }
                        continue;
                    }

                }
                Client.Write(new ClientMessage() { Message = msg }, null, "Messages");
            }
            Client.Write(new object(), null, "Logout");
            Client.Flush();
            Client.Client.Close();
        }

        private static void OnMessage(SnooperSocket.Models.SnooperMessage message)
        {
            ServerMessage MSG = message.ReadObject<ServerMessage>();
            Console.WriteLine($"[{MSG.Username}] {MSG.Message}");
        }
    }
}