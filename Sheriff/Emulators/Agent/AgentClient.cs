using System;
using System.Collections.Generic;
using System.Reflection;
using System.Net.Sockets;
using NLog;

using Sheriff.Security;
using Sheriff.Networking;
using Sheriff.Common.Plugins;
using System.Net;

namespace Sheriff.Emulators.Agent
{
    class AgentClient : IClient
    {
        public AgentServer Parent { private get; set; }
        private Socket clientSocket = null;
        private Socket agentSocket = null;

        byte[] clientBuffer = new byte[8192];
        byte[] agentBuffer = new byte[8192];


        #region "Override Properties"

        public override event DisconnectHandler OnDisconnected;
        public override Type ClientType => Type.Gateway;
        public override string IPAddress => (this.clientSocket.RemoteEndPoint as IPEndPoint).Address.ToString();

        #endregion;


        private SecurityManager clientSecurity = new SecurityManager();
        private SecurityManager agentSecurity = new SecurityManager();

        private Logger logger = LogManager.GetCurrentClassLogger();
        private Logger packetlogger = LogManager.GetLogger("Packet");
        private Dictionary<string, object> Properties = new Dictionary<string, object>();
        

        public AgentClient(Socket clientSocket)
        {
            this.clientSocket = clientSocket;
        }
        public void Start()
        {

            try
            {
                this.agentSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                this.agentSocket.Connect(this.Parent.Bind[0].ToIpEndPoint());
            }
            catch (SocketException)
            {
                this.Disconnect();
                logger.Warn("Cant connect to agent server. [{0}]({1})", this.Parent.ID ,this.Parent.Bind[1].ToIpEndPoint().ToString());
                return;
            }

         


            this.clientSecurity.GenerateSecurity(true, true, true);
            this.ReceiveFromClient();
            this.Send(false);

        }
        private void AgentReceiveCallBack(IAsyncResult ar)
        {

            int lenght = 0;
            try
            {
                lenght = this.agentSocket.EndReceive(ar);
            }
            catch (ObjectDisposedException)
            {
                this.Disconnect();
                return;
            }
            catch(SocketException)
            {
                this.Disconnect();
                return;
            }
            catch (Exception ex)
            {
                this.logger.Warn(ex);
                this.Disconnect();
                return;
            }


            if (lenght != 0)
            {
                agentSecurity.Recv(agentBuffer, 0, lenght);

                List<Packet> packets = agentSecurity.TransferIncoming();


                if (packets != null)
                {
                    foreach (Packet packet in packets)
                    {
                      
                        if (packet.Opcode == 0x5000 || packet.Opcode == 0x9000)
                        {
                            this.Send(true);
                            continue;
                        }



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

            this.ReceiveFromAgent();
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
                this.logger.Warn(ex);
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

                        if (packet.Opcode == 0x2113)
                        {
                            this.Send(false);
                            continue;
                        }

                        if (packet.Opcode == 0x2001)
                        {
                            this.ReceiveFromAgent();
                            continue;
                        }
                        #endregion

                      
                        if (!Program.manager.Notify(packet, this))
                        {
                            continue;
                        }

                        agentSecurity.Send(packet);
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


        public void Send(bool toAgent)
        {
            try
            {
                foreach (var packet in (toAgent ? agentSecurity : clientSecurity).TransferOutgoing())
                {
                    Socket socket = (toAgent ? agentSocket : clientSocket);
                    socket.Send(packet.Key.Buffer);
                 //   logger.Info(packet.Value.Opcode.ToString("X") + " -> " + ByteArrayExtension.HexDump(packet.Value.GetBytes()));
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
            catch (Exception ex)
            {
                this.logger.Warn(ex);
                this.Disconnect();
                return;
            }
    
        }
        public void ReceiveFromClient()
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
                this.logger.Warn(ex);
                return;
            }
        }
        public void ReceiveFromAgent()
        {
            try
            {
                this.agentSocket.BeginReceive(agentBuffer, 0, agentBuffer.Length, SocketFlags.None, new AsyncCallback(AgentReceiveCallBack), null);
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
                this.logger.Warn(ex);
                return;
            }
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
                this.agentSocket.Close();
            }
            catch{}
        }



        #region "MODULE"
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
        public override object FireMethod(string name,object[] paramaters)
        {
            MethodInfo info = this.GetType().GetMethod(name);
            return info.Invoke(this, paramaters);
        }

        public override void Send(Packet packet)
        {
            clientSecurity.Send(packet);
            this.Send(false);
        }

        #endregion
    }
}
