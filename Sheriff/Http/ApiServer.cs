using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Dynamic;
using System.IO;
using NLog;

using Sheriff.Common;
using Sheriff.Networking;
using Sheriff.Models;

namespace Sheriff.Http
{
    class ApiServer : IServiceBase
    {

        public delegate void OnRequestReceivedEventHandler(ApiServer server, HttpRequest request,HttpResponse response);
        public event OnRequestReceivedEventHandler OnRequestReceived;

     
        private List<IClient> _clients = new List<IClient>();

        

        public override byte ID { get; set; }
        public override string Description { get; set; }
        public override string Class { get; set; }
        public override bool Status { get; set; }
        public override List<IClient> Clients { get => this._clients; set => this._clients = value; }
        public override Bind[] Bind { get; set; }




        private Logger logger = LogManager.GetCurrentClassLogger();

        private Socket apiSocket = null;

        private byte[] buffer = new byte[8192];

        public ApiServer()
        {
            this.apiSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
     
        }

        /// <summary>
        /// Start the server.
        /// </summary>
        public override void Start()
        {
            try
            {
                this.apiSocket.Bind(this.Bind[0].ToIpEndPoint());
                this.apiSocket.Listen(5);
                this.apiSocket.BeginAccept(new AsyncCallback(AcceptCallBack), null);
            }
            catch { }
    
            base.Start();
            logger.Info("Service started.");
        }

        /// <summary>
        /// Stop the server.
        /// </summary>
        public override void Stop()
        {
            logger.Info("Service stopped.");
            base.Stop();
        }
         



        private void AcceptCallBack(IAsyncResult ar)
        {
            Socket client = this.apiSocket.EndAccept(ar);
            this.apiSocket.BeginAccept(new AsyncCallback(AcceptCallBack), null);
            client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallBack), client);
        }

        private void ReceiveCallBack(IAsyncResult ar)
        {
            
            try
            {
                Socket client = (Socket)ar.AsyncState;
                int lenght = client.EndReceive(ar);

                if (lenght != 0)
                {


                    StreamReader reader = new StreamReader(new MemoryStream(this.buffer, 0, lenght));

                    HttpRequest request = new HttpRequest(reader);
                    HttpResponse response = new HttpResponse(client);

                    if (!this.Status)
                    {
                        response.Headers.Add("Access-Control-Allow-Origin", "*");
                        response.StatusCode = StatusCode.Gone;
                        dynamic obj = new ExpandoObject();
                        obj.id = this.ID;
                        obj.name = this.Description;
                        obj.code = StatusCode.Gone;
                        obj.message = "The service has been stopped.";
                        response.setJSONContent(obj);
                        return;
                    }
                    else
                    {
                        if (this.OnRequestReceived != null)
                        {
                            this.OnRequestReceived(this, request, response);
                        }

                    }

                  




                }
                else
                {


                }
            }
            catch(Exception ex) { logger.Warn(ex.ToString); }
        }

        private void SendCallBack(IAsyncResult ar)
        {
            Socket client = (Socket)ar.AsyncState;
            client.EndSend(ar);
            client.Close();
 
        }




    }
}
