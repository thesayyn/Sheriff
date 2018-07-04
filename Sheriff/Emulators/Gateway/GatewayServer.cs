using System;
using NLog;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using Sheriff.Common;

using Sheriff.Networking;
using Sheriff.Models;

namespace Sheriff.Emulators.Gateway
{
    class GatewayServer : IServiceBase
    {


        private Socket GatewaySocket = null;
        private Logger logger = LogManager.GetCurrentClassLogger();
        private List<IClient> _clients = new List<IClient>();



        public override byte ID { get; set; }
        public override string Description { get; set; }
        public override string Class { get; set; }
        public override bool Status { get; set; }
        public override List<IClient> Clients { get => this._clients; set => this._clients = value; }
        public override Bind[] Bind { get; set; }




        public DivisionCollection Divisions { get; set; }
        public Server Server { get; set; }




        /// <summary>
        /// Start the server.
        /// </summary>
        public override void Start()
        {
            this.GatewaySocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.GatewaySocket.Bind(this.Bind[1].ToIpEndPoint());
            this.GatewaySocket.Listen(50);
            logger.Info("Service started.");
            base.Start();


            this.GatewaySocket.BeginAccept(new AsyncCallback(AcceptCallBack), null);
        }


        /// <summary>
        /// Stop the server.
        /// </summary>
        public override void Stop()
        {
            this.GatewaySocket.Close();
            logger.Info("Service stopped.");
            base.Stop();
        }



        private void AcceptCallBack(IAsyncResult ar)
        {
            try
            {
               
                GatewayClient client = new GatewayClient(this.GatewaySocket.EndAccept(ar));
                client.Parent = this;
                client.OnDisconnected += client_OnDisconnected;

                this.Clients.Add(client);
                Program.manager.NotifyConnected(client);

                client.Start();
                this.logger.Trace("One user connected.");
            }
            catch { }
            finally
            {
                if (this.Status) this.GatewaySocket.BeginAccept(new AsyncCallback(AcceptCallBack), null);
            }
            
        }
        private void client_OnDisconnected(IClient client)
        {

            this.Clients.Remove(client);

            this.logger.Trace("One user disconnected. ");
         
            Program.manager.NotifyDisconnected(client);
        }


    }
}
