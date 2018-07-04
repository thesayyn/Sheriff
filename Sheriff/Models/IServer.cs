using Sheriff.Common;
using Sheriff.Networking;

using System.Collections.Generic;
using System.Net.Sockets;
using NLog;

namespace Sheriff.Models
{
    abstract class IServer
    {

        public abstract byte ID { get; set; }
        public abstract string Description { get; set; }
        public abstract string Class { get; set; }
        public abstract bool Status { get; set; }
        public abstract List<IClient> Clients { get; set; }
        public abstract Socket Socket { get; set; }
        public abstract Bind[] Bind { get; set; }
        public abstract Logger  Logger { get; set; }


        public IServer()
        {
            this.ID = 0;
            this.Description = null;
            this.Class = null;
            this.Status = false;
            this.Clients = new List<IClient>();
            this.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public virtual void Start()
        {
            this.Logger = LogManager.GetLogger(this.Class);
            this.Status = true;
        }

        public virtual void Stop()
        {
            this.Status = false;
        }
    }
}
