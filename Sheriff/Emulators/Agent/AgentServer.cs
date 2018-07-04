using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using NLog;

using Sheriff.Common;
using Sheriff.Networking;
using Sheriff.Models;

namespace Sheriff.Emulators.Agent
{
    class AgentServer : IServiceBase
    {

        private Socket AgentSocket = null;
        private Logger logger = LogManager.GetCurrentClassLogger();
        private List<IClient> _clients = new List<IClient>();



        public override byte ID { get; set; }
        public override string Description { get; set; }
        public override string Class { get; set; }
        public override bool Status { get; set; }
        public override List<IClient> Clients { get => this._clients; set => this._clients = value; }
        public override Bind[] Bind { get; set; }


     



        /// <summary>
        /// Start the server.
        /// </summary>
        public override void Start()
        {
            this.AgentSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.AgentSocket.Bind(this.Bind[1].ToIpEndPoint());
            this.AgentSocket.Listen(50);
            logger.Info("Service started.");
            base.Start();
            this.AgentSocket.BeginAccept(new AsyncCallback(AcceptCallBack), null);
        }


        /// <summary>
        /// Stop the server.
        /// </summary>
        public override void Stop()
        {
            this.AgentSocket.Close();
            logger.Info("Service stopped.");
            base.Stop();
        }





        private void AcceptCallBack(IAsyncResult ar)
        {
            try
            {
                AgentClient client = new AgentClient(this.AgentSocket.EndAccept(ar));
                client.Parent = this;
                client.OnDisconnected += client_OnDisconnected;
                this.Clients.Add(client);
                this.logger.Trace("One user connected.");
                client.Start();
                Program.manager.NotifyConnected(client);
            }
            catch{}
            finally
            {
                if (this.Status) this.AgentSocket.BeginAccept(new AsyncCallback(AcceptCallBack), null);
            }
          

            
           
        }

        private void client_OnDisconnected(IClient client)
        {
            this.logger.Info("One user disconnected.");
            this.Clients.Remove(client);
            Program.manager.NotifyDisconnected(client);

        }

        
    }
}
