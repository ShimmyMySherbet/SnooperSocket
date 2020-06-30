using SnooperSocket;
using SnooperSocket.Models;
using System;
using System.Net;
using System.Net.Sockets;

namespace SnooperRelayTest
{
    internal class Program
    {
        public static TcpClient TCPClient;
        private static IPEndPoint EP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 3218);
        private static SnooperSocketClient Socket;
        public static string Username;
        public static string ClientName;

        private static void Main(string[] args)
        {
            Console.Write("SERVER/CLIENT [S/C]");
            string inp = Console.ReadLine();

            if (inp.ToLower() == "s")
            {
                Console.WriteLine("Starting Server...");
                TcpListener Listener = new TcpListener(EP);
                Listener.Start();
                Console.WriteLine("Waiting for client...");
                TCPClient = Listener.AcceptTcpClient();
                Console.WriteLine("Client Connected.");
            }
            else
            {
                Console.WriteLine("Conecting...");
                TCPClient = new TcpClient();
                TCPClient.Connect(EP);
                Console.WriteLine("Connected.");
            }
            Console.WriteLine($"Client: {ClientName != null}");
            Socket = new SnooperSocketClient() { Client = TCPClient };
            LoginData LD = new LoginData() { Joining = true };
            Console.Write("Username: ");
            LD.Username = Console.ReadLine();
            Username = LD.Username;
            Socket.Write(LD, null, "Login");
            Socket.Channels["Login"].MessageReceived += UserJoined;
            Socket.Channels["Messages"].MessageReceived += OnMessage;
            Socket.Start();

            while (true)
            {
                string msg = Console.ReadLine();

                if (msg == "$exit")
                {
                    LoginData LDD = new LoginData() { Joining = false };
                    Console.Write("Username: ");
                    LD.Username = Username;
                    Username = LD.Username;
                    Socket.Write(LDD, null, "Login");
                }
                else
                {
                    Message MSG = new Message() { MessageContent = msg };
                    Socket.Write(MSG, null, "Messages");
                }
            }
        }

        private static void OnMessage(SnooperMessage message)
        {
            Message MSG = message.ReadObject<Message>();
            Console.WriteLine($"[{ClientName}] {MSG.MessageContent}");
        }

        private static void UserJoined(SnooperMessage message)
        {
            Console.WriteLine("rec");
            LoginData Data = message.ReadObject<LoginData>();
            ClientName = Data.Username;
            if (Data.Joining)
            {
                Console.WriteLine($"[Server] User '{Data.Username}' joined the chat.");
            }
            else
            {
                Console.WriteLine($"[Server] User '{Data.Username}' left the chat.");
            }
        }
    }

    public class LoginData
    {
        public string Username;
        public bool Joining = true;
    }

    public class Message
    {
        public string MessageContent;
    }
}