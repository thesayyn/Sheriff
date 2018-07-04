using Sheriff.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Sheriff.Networking
{

    public abstract class IClient
    {
        public enum Type
        {
            Gateway,
            Agent,
            Download
        }

        public delegate void DisconnectHandler(IClient client);
        public virtual event DisconnectHandler OnDisconnected;
        public abstract Type ClientType { get; }
        public abstract string IPAddress { get; }

        public abstract object GetProperty(string name);
        public abstract bool SetProperty(string name, object value);
        public abstract object FireMethod(string name, params object[] paramaters);

        public abstract void Send(Packet packet);
        public abstract void Disconnect();

    }
}
