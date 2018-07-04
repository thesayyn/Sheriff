using System;
using System.Collections.Generic;
using NLog;
using Sheriff.Networking;
using Sheriff.Security;
using Sheriff.Common.Plugins;
using System.Xml;

namespace Sheriff.Common.Plugins
{
    public class PluginBase
    {
        public PluginBase(IPlugin plugin,Type type) {
            this.Plugin = plugin;
            this.Class = type.FullName;
            this.ID = Utility.MD5(this.Class);
            this.Status = true;
        }
        
        private IPlugin Plugin { get; set; }
        public string ID { get; internal set; }
        public string Class { get; internal set; }
        public bool Status { get; internal set; }

        public string Name => Plugin.Name;
        public string Author => Plugin.Author;
        public string Description => Plugin.Description;
        public Version Version => Plugin.Version;



        public void Stop()
        {
            this.Status = false;
            this.GetPlugin().OnLoad();
        }
        public void Start()
        {
            this.Status = true;
            this.GetPlugin().OnUnLoad();
        }

        public IPlugin GetPlugin(){  return this.Plugin;  }
        public List<ushort> GetOpcodes()
        {
            return this.GetPlugin().GetOpcodes();
        }
        public bool IsRegistered(string ds)
        {
            return this.GetPlugin().GetRegisteredApis().Contains(ds);
        }
    }
}
