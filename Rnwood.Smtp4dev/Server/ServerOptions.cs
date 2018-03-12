using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Server
{
    public class ServerOptions
    {
        public int Port { get; set; }
        public bool AllowRemoteConnections { get; set; }

        public SecureConnection SecureConnection { get; set; }
    }

    public class SecureConnection
    {
        public bool UseSecureConnection { get; set; }

        public string CertificatePath { get; set; }
        public string CertificatePassword { get; set; }
        
        // Windows only
        public string Thumbprint { get; set; }
    }
}
