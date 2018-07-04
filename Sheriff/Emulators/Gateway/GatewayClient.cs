using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Collections;
using System.Reflection;
using System.Net;
using NLog;
using Sheriff.Common;
using Sheriff.Networking;
using Sheriff.Security;


namespace Sheriff.Emulators.Gateway
{
    class GatewayClient : IClient
    {
        public GatewayServer Parent { private get; set; }



        private Socket clientSocket = null;
        private Socket gatewaySocket = null;


        #region "Override Properties"

        public override event   DisconnectHandler OnDisconnected;
        public override Type    ClientType => Type.Gateway;
        public override string IPAddress => (this.clientSocket.RemoteEndPoint as IPEndPoint).Address.ToString();

        #endregion;



        #region "Buffers"  

        byte[] clientBuffer = new byte[8192];
        byte[] gatewayBuffer = new byte[8192];

        #endregion;


        #region "Security"

        private SecurityManager clientSecurity = new SecurityManager();
        private SecurityManager gatewaySecurity = new SecurityManager();

        #endregion;





        #region "Private Properties"

        private Logger logger = LogManager.GetCurrentClassLogger();
        private Dictionary<string, object> Properties = new Dictionary<string, object>();
        private uint shardId = 0x0;

        #endregion;


        public GatewayClient(Socket clientSocket)
        {
            this.clientSocket = clientSocket;
        }


        /// <summary>
        /// Start the client and connect to gateway server.
        /// </summary>
        public void Start()
        {

            try
            {
                this.gatewaySocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                this.gatewaySocket.Connect(this.Parent.Bind[0].ToIpEndPoint());

            }
            catch (SocketException)
            {
                this.Disconnect();
                logger.Warn("Cant connect to gateway server. [{0}]({1})", this.Parent.ID, this.Parent.Bind[0].ToIpEndPoint().ToString());
                return;
            }





            this.clientSecurity.GenerateSecurity(true, true, true);
            this.ReceiveFromClient();
            this.Send(false);

        }




        #region "Socket Callbacks"

        private void GatewayReceiveCallBack(IAsyncResult ar)
        {

            int lenght = 0;
            try
            {
                lenght = this.gatewaySocket.EndReceive(ar);
            }
            catch (ObjectDisposedException)
            {
                this.Disconnect();
                return;
            }
            catch (SocketException)
            {
                this.Disconnect();
                return;
            }
            catch (Exception ex)
            {
                logger.Warn(ex);
                this.Disconnect();
                return;
            }


                if (lenght != 0)
                {
                    gatewaySecurity.Recv(gatewayBuffer, 0, lenght);

                    List<Packet> packets = gatewaySecurity.TransferIncoming();


              

                    if (packets != null)
                    {
                        foreach (Packet packet in packets)
                        {

                        #region "HADSHAKE_AND_OTHERS"
                        if (packet.Opcode == 0x5000 || packet.Opcode == 0x9000)
                        {
                            this.Send(true);
                            continue;
                        }
                        #endregion

                        #region "SERVER_GATEWAY_AGENT_REDITECT"
                        if (packet.Opcode == 0xA102)
                        {
                            byte res = packet.ReadUInt8();

                            if (res == 1)
                            {
                                uint id = packet.ReadUInt32();

                                Division agent = this.Parent.Divisions.GetByShardID(this.shardId);

                                Packet loginPacket = new Packet(0xA102, true);
                                loginPacket.WriteUInt8(1);
                                loginPacket.WriteUInt32(id);

                              
                                loginPacket.WriteAscii(agent.Agent.Bind[1].IP);
                                loginPacket.WriteUInt16(agent.Agent.Bind[1].Port);


                                packet.Replace(loginPacket);
                                    
                            }
                        }
                        #endregion

                        #region "SERVER_GATEWAY_SHARD_LIST_RESPONSE"
                        if (packet.Opcode == 0xA101)
                        {

                                Packet response = new Packet(packet.Opcode, packet.Encrypted);
         
                                response.WriteUInt8(1);
                                response.WriteUInt8(this.Parent.Divisions.FarmID);
                                response.WriteAscii(this.Parent.Divisions.FarmName);
                                response.WriteUInt8(0);


                                IEnumerator enumerator =  this.Parent.Divisions.GetEnumerator();

                                int hasEntry = (enumerator.MoveNext() ? 1 : 0);
                                response.WriteUInt8(hasEntry); 

                                while(hasEntry == 1){


                                    Division div = (Division) enumerator.Current;
                                    string shard = packet.ReadUInt16().ToString();
                                    response.WriteUInt16(div.ShardID);
                                    response.WriteAscii(div.Name);
                                    response.WriteUInt16(div.Capacity - 3);
                                    response.WriteUInt16(div.Capacity);
                                    response.WriteUInt8(1);
                                    response.WriteUInt8(this.Parent.Divisions.FarmID);

                                    hasEntry = (enumerator.MoveNext() ? 1 : 0);
                                    response.WriteUInt8(hasEntry);
                                           
                                }

                            response.Lock();
                        packet.Replace(response);

                        }
                        #endregion

                        if (!Program.manager.Notify(packet, this))
                        {
                            continue;
                        }

                        clientSecurity.Send(packet);
                        this.Send(false);

                        }


                    }


         
                    
                }
                else
                {
                    this.Disconnect();
                    return;
                }

                this.ReceiveFromGateway();
        }

        private void ClientReceiveCallBack(IAsyncResult ar)
        {
            int lenght = 0;

            try
            {
                lenght = this.clientSocket.EndReceive(ar);
            }
            catch (ObjectDisposedException)
            {
                this.Disconnect();
                return;
            }
            catch (SocketException)
            {
                this.Disconnect();
                return;
            }
            catch (Exception ex)
            {
                logger.Warn(ex);
                this.Disconnect();
                return;
            }

            if (lenght != 0)
            {
                clientSecurity.Recv(clientBuffer, 0, lenght);

                List<Packet> packets = clientSecurity.TransferIncoming();



                if (packets != null)
                {
                    foreach (Packet packet in packets)
                    {

                      
                        #region "HANDSHAKE_AND_OTHERS"
                        if (packet.Opcode == 0x5000 || packet.Opcode == 0x9000)
                        {
                            this.Send(false);
                            continue;
                        }

                        if (packet.Opcode == 0x2001)
                        {
                            this.ReceiveFromGateway();
                            continue;
                        }
                        #endregion;

                        #region "CLIENT_GATEWAY_LOGIN_REQUEST"
                        if (packet.Opcode == 0x6102)
                        {
                            uint ContentID = packet.ReadUInt8();
                            string Username = packet.ReadAscii();
                            string Password = packet.ReadAscii();
                            this.shardId = packet.ReadUInt16();
                        }
                        #endregion


                        if (!Program.manager.Notify(packet, this))
                        {
                            continue;
                        }

                        gatewaySecurity.Send(packet);
                        this.Send(true);

                    }


                }

            }
            else
            {
                this.Disconnect();
                return;
            }


            this.ReceiveFromClient();


        }

        #endregion;




        public void Send(bool toGateway)
        {
            try
            {
                foreach (var packet in (toGateway ? gatewaySecurity : clientSecurity).TransferOutgoing())
                {

                    Socket socket = (toGateway ? gatewaySocket : clientSocket);
                    socket.Send(packet.Key.Buffer);
                }
            }
            catch (ObjectDisposedException)
            {
                this.Disconnect();
                return;
            }
            catch (SocketException)
            {
                this.Disconnect();
                return;
            }
            catch (Exception ex) { 
                logger.Warn(ex);
                this.Disconnect();
                return;
            }
        }
        private void ReceiveFromClient()
        {
            try
            {
                this.clientSocket.BeginReceive(clientBuffer, 0, clientBuffer.Length, SocketFlags.None, new AsyncCallback(ClientReceiveCallBack), null);
            }
            catch (ObjectDisposedException)
            {
                this.Disconnect();
                return;
            }
            catch (SocketException)
            {
                this.Disconnect();
                return;
            }
            catch (Exception ex)
            {
                logger.Warn(ex);
                this.Disconnect();
                return;

            }
        }
        private void ReceiveFromGateway()
        {
     
            try
            {
                this.gatewaySocket.BeginReceive(gatewayBuffer, 0, gatewayBuffer.Length, SocketFlags.None, new AsyncCallback(GatewayReceiveCallBack), null);
            }
            catch (ObjectDisposedException)
            {
                this.Disconnect();
                return;
            }
            catch (SocketException)
            {
                this.Disconnect();
                return;
            }
            catch (Exception ex)
            {
                logger.Warn(ex);
                this.Disconnect();
                return;
            }
        }





        #region "IClient Override Methods"

        public override object GetProperty(string name)
        {
            if (this.Properties.ContainsKey(name))
            {
                return this.Properties[name];
            }
            return null;
        }
        public override bool SetProperty(string name, object value)
        {
            this.Properties[name] = value;
            return true;
        }
        public override object FireMethod(string name, object[] paramaters)
        {
            MethodInfo info = this.GetType().GetMethod(name);
            return info.Invoke(this, paramaters);
        }
        public override void Disconnect()
        {

            if (this.OnDisconnected != null)
            {
                this.OnDisconnected(this);
                this.OnDisconnected = null;
            }
            try
            {
                this.clientSocket.Close();
                this.gatewaySocket.Close();
            }
            catch { }
        }
        public override void Send(Packet packet)
        {
            clientSecurity.Send(packet);
            this.Send(false);
        }


        #endregion


    }
}
