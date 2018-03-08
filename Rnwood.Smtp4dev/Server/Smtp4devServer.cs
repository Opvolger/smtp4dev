using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using MimeKit;
using Rnwood.Smtp4dev.DbModel;
using Rnwood.Smtp4dev.Hubs;
using Rnwood.SmtpServer;
using System;
using System.IO;

namespace Rnwood.Smtp4dev.Server
{
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;

    public class Smtp4devServer
    {
        public Smtp4devServer(Func<Smtp4devDbContext> dbContextFactory, IOptions<ServerOptions> serverOptions, MessagesHub messagesHub, SessionsHub sessionsHub)
        {
            this.dbContextFactory = dbContextFactory;

            this.smtpServer = new DefaultServer(serverOptions.Value.AllowRemoteConnections, serverOptions.Value.Port,
                GetX509Certificate(serverOptions.Value.SecureConnection));
            this.smtpServer.MessageReceived += OnMessageReceived;
            this.smtpServer.SessionCompleted += OnSessionCompleted;

            this.messagesHub = messagesHub;
            this.sessionsHub = sessionsHub;
        }

        private X509Certificate GetX509Certificate(SecureConnection secureConnection)
        {
            if (secureConnection != null &&
                secureConnection.UseSecureConnection)
            {
                if (!string.IsNullOrEmpty(secureConnection.CertificatePath))
                {
                    return new X509Certificate(File.ReadAllBytes(secureConnection.CertificatePath));
                }
                if (!string.IsNullOrEmpty(secureConnection.Thumbprint))
                {
                    var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
                    store.Open(OpenFlags.ReadOnly);
                    var cert = store.Certificates.OfType<X509Certificate2>()
                        .FirstOrDefault(b => b.Thumbprint == secureConnection.Thumbprint);
                    store.Close();
                    return cert;
                }
            }
            return null;
        }

        private void OnSessionCompleted(object sender, SessionEventArgs e)
        {
            Smtp4devDbContext dbContent = dbContextFactory();

            Session session = new Session();
            session.EndDate = e.Session.EndDate.GetValueOrDefault(DateTime.Now);
            session.ClientAddress = e.Session.ClientAddress.ToString();
            session.ClientName = e.Session.ClientName;
            session.NumberOfMessages = e.Session.GetMessages().Length;
            session.Log = e.Session.GetLog().ReadToEnd();
            dbContent.Sessions.Add(session);

            dbContent.SaveChanges();

            sessionsHub.OnSessionsChanged().Wait();
        }

        private void OnMessageReceived(object sender, MessageEventArgs e)
        {
            Smtp4devDbContext dbContent = dbContextFactory();

            using (Stream stream = e.Message.GetData())
            {
                Message message = new MessageConverter().Convert(stream);
                dbContent.Messages.Add(message);
            }

            dbContent.SaveChanges();
            messagesHub.OnMessagesChanged().Wait();
        }

        private Func<Smtp4devDbContext> dbContextFactory;

        private DefaultServer smtpServer;

        private MessagesHub messagesHub;
        private SessionsHub sessionsHub;

        public void Start()
        {
            smtpServer.Start();
        }
    }
}
