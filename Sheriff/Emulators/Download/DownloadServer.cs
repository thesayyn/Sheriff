using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using NLog;

using Sheriff.Networking;
using Sheriff.Common;
using Sheriff.Models;

namespace Sheriff.Emulators.Download
{
    class DownloadServer : IServiceBase
    {

        private List<IClient> _clients = new List<IClient>();

        public override byte ID { get; set; }
        public override string Description { get; set; }
        public override string Class { get; set; }
        public override bool Status { get; set; }
        public override List<IClient> Clients { get => this._clients; set => this._clients = value; }

        public override Bind[] Bind { get; set; }

        private Socket DownloadSocket = null;
        private Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Start the server.
        /// </summary>
        public override void Start()
        {
            this.DownloadSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.DownloadSocket.Bind(this.Bind[0].ToIpEndPoint());
            this.DownloadSocket.Listen(50);
            base.Start();
            logger.Info("Service started.");
            this.DownloadSocket.BeginAccept(new AsyncCallback(AcceptCallBack), null);
        }


        /// <summary>
        /// Stop the server.
        /// </summary>
        public override void Stop()
        {
            this.DownloadSocket.Close();
            logger.Info("Service stopped.");
            base.Stop();
        }


        private void AcceptCallBack(IAsyncResult ar)
        {

            DownloadClient client = new DownloadClient(this.DownloadSocket.EndAccept(ar));
            client.OnDisconnected += client_OnDisconnected;
            this.Clients.Add(client);
            this.logger.Trace("One user connected.");
            Program.manager.NotifyConnected(client);
            this.DownloadSocket.BeginAccept(new AsyncCallback(AcceptCallBack), null);

        }

        private void client_OnDisconnected(IClient client)
        {
            this.Clients.Remove(client);
            this.logger.Info("One user disconnected.");
            Program.manager.NotifyDisconnected(client);
        }



    }
}
