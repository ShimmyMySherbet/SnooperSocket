using SnooperSocket;
using SnooperSocket.Models;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SSocketChatTest
{
    public static class Server
    {
        public static SnooperSocketClientPool<userProfile> Clients = new SnooperSocketClientPool<userProfile>();

        public static IPEndPoint EndPoint;

        public static TcpListener Listener;

        public static bool Active = false;


        public static void StartServer(IPEndPoint EP)
        {
            EndPoint = EP;
            Listener = new TcpListener(EndPoint);
            Listener.Start();
            new Thread(Listen).Start();
            Clients.Stack["Login"].MessageReceived += LoginRequest;
            Clients.Stack["Messages"].MessageReceived += HandleNewMessage;
            Clients.Stack["Logout"].MessageReceived += OnClientDisconnect;
            Active = true;
        }

        private static void OnClientDisconnect(SnooperMessage message, SnooperSocketClient Client)
        {
            userProfile Profile = Clients.GetClientData(Client);
            if (Profile.LoggedIn)
            {
                Clients.Leave(Client);
                Client.Client.Close();
                Clients.SendAll(new ServerMessage() { Message = $"User '{Profile.Username}' has left the chat.", Username = "Server" }, null, "Messages");

            }
        }

        private static void HandleNewMessage(SnooperMessage message, SnooperSocketClient Client)
        {
            userProfile Profile = Clients.GetClientData(Client);
            if (Profile.LoggedIn)
            {
                Clients.SendAll(new ServerMessage() { Message = message.ReadObject<ClientMessage>().Message, Username = Profile.Username }, null, "Messages");
            } else
            {
            }
        }


        private static void LoginRequest(SnooperMessage message, SnooperSocketClient Client)
        {
            UserLoginData LData = message.ReadObject<UserLoginData>();
            userProfile Profile = Clients.GetClientData(Client);
            if (!Profile.LoggedIn)
            {
                Profile.LoggedIn = true;
                Profile.Username = LData.Username;
                Clients.SendAll(new ServerMessage() { Message = $"User '{Profile.Username}' has joined the chat.", Username = "Server" }, null, "Messages");
            }
        }

        private static void Listen()
        {
            while (true)
            {
                TcpClient NewTCPClient = Listener.AcceptTcpClient();
                SnooperSocketClient NewClient = new SnooperSocketClient() { Client = NewTCPClient };
                NewClient.Start();
                Clients.Join(NewClient, new userProfile());
                NewClient.OnDisconnect += ClientDisconnected;
            }
        }

        private static void ClientDisconnected(SnooperSocketClient Client)
        {
            userProfile Profile = Clients.GetClientData(Client);
            if (Profile != null &&  Profile.LoggedIn)
            {
                Clients.Leave(Client);
                Client.Client.Close();
                Clients.SendAll(new ServerMessage() { Message = $"User '{Profile.Username}' has left the chat (User Disconnected Unexpectedly).", Username = "Server" }, null, "Messages");

            }
        }
    }

    public class UserLoginData
    {
        public string Username;
    }

    public class ServerMessage
    {
        public string Username;
        public string Message;
    }
    public class ClientMessage
    {
        public string Message;
    }
    public class userProfile
    {
        public string Username;
        public DateTime Joined = DateTime.Now;
        public bool LoggedIn = false;
    }
}