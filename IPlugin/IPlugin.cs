using NLog;
using Sheriff.Security;
using Sheriff.Networking;
using System.Collections.Generic;
using System.IO;
using System;

namespace Sheriff.Common.Plugins
{

    public abstract class IPlugin
    {
        public abstract string Name { get; }
        public abstract string Author { get; }
        public abstract string Description { get;}
        public abstract Version Version { get; }

        private Logger Logger { get; set; }
        private PluginSettings Settings { get; set; }
        private List<ushort> Opcodes { get; set; }
        private List<string> Data { get; set; }

        public virtual Logger GetLogger()
        {
            if(this.Logger == null) this.Logger = LogManager.GetCurrentClassLogger(); 
            return this.Logger;
        }
        public virtual void SetLogger(Logger logger)
        {
            this.Logger = logger;
        }
        public virtual PluginSettings GetSettings()
        {
            if (this.Settings == null) this.Settings = new PluginSettings(this);
            return this.Settings;
        }


        public virtual void Register(ushort opcode)
        {
            if (this.Opcodes == null) this.Opcodes = new List<ushort>();
            if(!this.Opcodes.Contains(opcode)) this.Opcodes.Add(opcode);

        }

        public virtual void UnRegister(ushort opcode)
        {
            if (this.Opcodes == null) this.Opcodes = new List<ushort>();
            this.Opcodes.Remove(opcode);
        }

        /// <summary>If you want to send data to api , register your api datasource.</summary>
        public virtual void RegisterApi(string datasource)
        {
            if (this.Data == null) this.Data = new List<string>();
            this.Data.Remove(datasource);
            this.Data.Add(datasource);
        }



        public virtual List<ushort> GetOpcodes()
        {
            if (this.Opcodes == null) this.Opcodes = new List<ushort>();
            return this.Opcodes;
        }
        public virtual List<string> GetRegisteredApis()
        {
            if (this.Data == null) this.Data = new List<string>();
            return this.Data;
        }



        /// <summary>Notify when any client connected.</summary>
        public virtual bool OnClientConnected(IClient client)
        {
            return true;
        }

        /// <summary>Notify when packet received. That is will be same your registered packets. </summary>
        public virtual bool OnPacketReceived(Packet packet,IClient client)
        {
            return true;
        }

        /// <summary>Notify when client disconnected.</summary>
        public virtual void OnClientDisconnected(IClient client) { }



        /// <summary>Notify when api request received.</summary>
        public virtual object OnApiRequested(Request request)
        {
            return new object();
        }



        public abstract void OnLoad();
        public abstract void OnUnLoad();

        /// <summary>Notify when settings changed by user or api.</summary>
        public abstract void OnSettingsChanged();



    }
}
