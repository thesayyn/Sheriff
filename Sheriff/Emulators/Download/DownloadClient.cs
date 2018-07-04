using Sheriff.Networking;
using Sheriff.Security;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using Sheriff.Common.Plugins;
using System.Net;

namespace Sheriff.Emulators.Download
{
    class DownloadClient : IClient
    {
        private Socket _clientSocket = null;
        private SecurityManager _security = new SecurityManager();
        private Logger logger = LogManager.GetCurrentClassLogger();
        private Dictionary<string, object> Properties = new Dictionary<string, object>();

        private byte[] buffer = new byte[8000];


        #region "Override Properties"

        public override event DisconnectHandler OnDisconnected;
        public override Type ClientType => Type.Download;
        public override string IPAddress => (this._clientSocket.RemoteEndPoint as IPEndPoint).Address.ToString();

        #endregion;


        public DownloadClient(Socket client)
        {
            this._clientSocket = client;

            this._security.GenerateSecurity(true,true,true);
            this.Send();
            this.ReceiveFromClient();

        }

        private void ReceiveFromClient()
        {
            try
            {
                this._clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallBack), null);
            }
            catch (Exception ex)
            {
                this.logger.Warn(ex);
            }
          
        }

        private void ReceiveCallBack(IAsyncResult ar)
        {
            int lenght = this.EndReceive(ar);

            if (lenght != 0)
            {

                this._security.Recv(this.buffer, 0, lenght);

                List<Packet> packets = this._security.TransferIncoming();

                foreach (Packet packet in packets)
                {
                    this.logger.Info("Packet Received -> Opcode = 0x{0} , Massive = {1} , Encrypted = {2}", packet.Opcode.ToString("x"), packet.Massive, packet.Encrypted);


                    if (packet.Opcode == 0x2001)
                    {

                        Packet pack = new Packet(0x2001);
                        pack.WriteAscii("DownloadServer");
                        pack.WriteUInt8(0);
                        this._security.Send(pack);
                        this.Send();

                        this.logger.Info("Module : " + packet.ReadAscii());
                        this.logger.Info("Version : " + packet.ReadUInt8());
               
                    }

                    if (packet.Opcode == 0x6004)
                    {
                        this.logger.Info("ID : " + packet.ReadUInt32());
                        this.logger.Info("Unk2 : " + packet.ReadUInt32());

                        Packet pack = new Packet(0x1001);
                        pack.WriteUInt8Array(File.ReadAllBytes(@"C:\Users\Sayyn\Desktop\srosahin\yeni\vSRO\9_22\Sheriff One.dll"));
                        this._security.Send(pack);
                        this.Send();

                        Packet padck = new Packet(0xA004);
                        padck.WriteUInt8(1);
                        this._security.Send(padck);
                        this.Send();
                    }

                    if (!Program.manager.Notify(packet, this))
                    {
                        continue;
                    }
                }

                this.ReceiveFromClient();
            }
            else
            {
                this.Disconnect();
            }
        }

        private int EndReceive(IAsyncResult ar)
        {
            try
            {
                return this._clientSocket.EndReceive(ar);
            }
            catch (Exception ex)
            {
                this.logger.Warn(ex);
            }
            return 0;
        }
        private void Send()
        {
            try
            {
                foreach (var packet in this._security.TransferOutgoing())
                {
                    if (Program.manager.Notify(packet.Value, this))
                    {
                        continue;
                    }

                    this._clientSocket.Send(packet.Key.Buffer);
                }
            }
            catch (Exception ex)
            {
                this.logger.Warn(ex);
            }
        }
        public override void Disconnect()
        {
            try
            {
                this._clientSocket.Close();
                if (this.OnDisconnected != null) { this.OnDisconnected(this); }
            }
            catch(Exception ex){
                this.logger.Warn(ex);
            }
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
        public override object FireMethod(string name, object[] paramaters)
        {
            MethodInfo info = this.GetType().GetMethod(name);
            return info.Invoke(this, paramaters);
        }

        public override void Send(Packet packet)
        {
           // clientSecurity.Send(packet);
         //   this.Send(false);
        }
        #endregion
    }



}
