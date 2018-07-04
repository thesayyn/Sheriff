using System;
using System.Collections.Generic;
using System.Net.Sockets;
using Sheriff.Common;
using Sheriff.Models;
using Sheriff.Networking;

namespace Sheriff.Servers
{
    class GatewayServer : IServer
    {
        public override byte ID { get; set; }
        public override string Description { get; set; }
        public override string Class { get; set; }
        public override bool Status { get; set; }
        public override List<IClient> Clients { get; set; }
        public override Socket Socket { get; set; }
        public override Bind[] Bind { get; set; }

        public GatewayServer() : base()
        {
            
        }




        public override void Start()
        {
            this.Socket.Bind(this.Bind[0].ToIpEndPoint());
            this.Socket.Listen(100);
            this.Socket.BeginAccept(AcceptCallBack, null);
            base.Start();
        }

        private void AcceptCallBack(IAsyncResult ar)
        {
            Socket clientSocket = this.Socket.EndAccept(ar);




            this.Socket.BeginAccept(AcceptCallBack, null);

        }

        public override void Stop()
        {
            this.Socket.Close();
            this.Clients.ForEach(x => x.Disconnect());
            this.Clients.Clear();
            base.Stop();
        }
    }
}
