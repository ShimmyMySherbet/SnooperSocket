using System;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace SnooperSocket.Cryptography.Protocals
{
    public class SSLSecurityProtocal : SnooperSecurityProtocal
    {
        public bool IsServer = false;
        public string ServerName;
        public X509Certificate serverCertificate = null;

        public bool SSLAuthenticated { get; protected set; } = false;

        public bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None) return true;
            Console.WriteLine($"Certificate error: {sslPolicyErrors}");
            return false;
        }

        public SSLSecurityProtocal(bool IsServer, string CertFile = null, string ServerName = null)
        {
            this.IsServer = IsServer;
            if (CertFile != null) serverCertificate = X509Certificate.CreateFromCertFile(CertFile);
            this.ServerName = ServerName;
        }

        public override void Init()
        {
            try
            {
                SslStream SSL;
                if (IsServer)
                {
                    SSL = new SslStream(Socket.Client.GetStream(), true);
                    SSL.AuthenticateAsServer(serverCertificate, false, false);
                    SSL.ReadTimeout = 5000;
                    SSL.WriteTimeout = 5000;
                } else
                {
                    SSL = new SslStream(Socket.Client.GetStream(), true, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
                    SSL.AuthenticateAsClient(ServerName);
                }
                // Replace socket's NetworkStream
                Socket.SetNetStream(SSL);
                SSLAuthenticated = true;
            }
            catch (AuthenticationException)
            {
                //Failed
                SSLAuthenticated = false;
                throw;
            }
        }

        public override bool ValidateConnection()
        {
            return SSLAuthenticated;
        }
    }
}